namespace PC_Medic;

internal sealed partial class OverviewPage : PageBase
{
    private string _address = string.Empty;
    private int _ticks;
    private readonly SystemMonitor _monitor = new();

    public OverviewPage()
        : base("SYSTEM OVERVIEW", "Live telemetry, machine identity and one-click remediation.")
    {
        InitializeComponent();

        void LayoutLoading() => _loading.Bounds = ClientRectangle;
        Resize += (_, _) => LayoutLoading();
        LayoutLoading();

        foreach ((StatTile tile, NeonButton action) in new[]
                 {
                     (_cpu, _coolCpu), (_memory, _freeMemory), (_disk, _cleanDisk), (_updates, _openUpdates),
                 })
        {
            action.Font = Theme.Kicker;
            tile.Action = action;
        }

        _tips.SetToolTip(_coolCpu, "Caps the processor ceiling at 70% to cut heat and power draw. Press again to restore it.");
        _tips.SetToolTip(_freeMemory, "Trims every reachable process working set and purges the standby list. Nothing is closed.");
        _tips.SetToolTip(_cleanDisk, $"Removes temp files on {MachineInfo.SystemDrive} that no process is using.");
        _tips.SetToolTip(_openUpdates, "Opens the Updates page: installed apps and drivers with pending updates.");

        void LayoutActions()
        {
            const int Gap = 8;
            int width = _actions.ClientSize.Width - 36;
            int y = 54;
            int height = Math.Max(26, (_actions.ClientSize.Height - 54 - 16 - (Gap * 2)) / 3);
            foreach (NeonButton button in new[] { _optimize, _startup, _taskManager })
            {
                button.SetBounds(18, y, width, height);
                y += height + Gap;
            }
        }

        _actions.Resize += (_, _) => LayoutActions();
        LayoutActions();

        _console.ContextMenuStrip = BuildConsoleMenu();

        _optimize.Click += async (_, _) =>
        {
            _optimize.Enabled = false;
            using IDisposable busy = Working();
            using IDisposable plate = _loading.Busy();
            ActivityLog.Info("Optimize Sleep requested from the dashboard.");
            await PowerService.OptimizeSleepAsync();
            _optimize.Enabled = true;
            RefreshData();
        };

        _updates.Cursor = Cursors.Hand;
        _updates.Click += (_, _) => NavigateTo?.Invoke("updates");
        _openUpdates.Click += (_, _) => NavigateTo?.Invoke("updates");

        _startup.Click += (_, _) => NavigateTo?.Invoke("startup");
        _taskManager.Click += (_, _) => Tools.LaunchTool("Task Manager", Tools.SystemExe("Taskmgr.exe"));

        _coolCpu.Click += async (_, _) => await ToggleCpuCeilingAsync();

        _freeMemory.Click += async (_, _) =>
        {
            _freeMemory.Enabled = false;
            using IDisposable busy = Working();
            using IDisposable plate = _loading.Busy();
            ActivityLog.Info("Trimming process working sets\u2026");

            MemoryReport report = await Task.Run(MemoryService.Trim);
            string message = report.Freed > 0
                ? $"{Theme.Bytes(report.Freed)} reclaimed \u2022 {report.Trimmed} processes trimmed" +
                  (report.Purged ? " \u2022 standby list purged" : string.Empty)
                : $"{report.Trimmed} processes trimmed; nothing was reclaimable";

            ActivityLog.Write(report.Trimmed > 0 ? LogLevel.Success : LogLevel.Warn, message);
            if (!report.Purged) ActivityLog.Info("The standby purge needs an elevated session.");
            Flash(_freeMemory, report.Freed > 0 ? $"FREED {Theme.Bytes(report.Freed)}" : "NOTHING TO FREE", "FREE MEMORY");
            _freeMemory.Enabled = true;
        };

        _cleanDisk.Click += (_, _) =>
        {
            using var dialog = new JunkCleanupForm();
            dialog.ShowDialog(this);

            // Freed space changes the drive tile, so it is re-read once the dialog closes.
            MachineInfo.RefreshDrive();
            UpdateDiskTile();
        };
    }

    /// <summary>Right-click on the activity feed offers the whole log on the clipboard.</summary>
    private static ContextMenuStrip BuildConsoleMenu()
    {
        var menu = new ContextMenuStrip { BackColor = Theme.Surface2, ForeColor = Theme.Text, Font = Theme.Ui };
        menu.ShowImageMargin = false;
        menu.Items.Add("Copy activity log", null, (_, _) => CopyActivityLog());
        return menu;
    }

    private static void CopyActivityLog()
    {
        string text = string.Join(
            Environment.NewLine,
            ActivityLog.Snapshot().Select(e => $"{e.Stamp}  {e.Level,-7}  {e.Message}"));

        try
        {
            Clipboard.SetText(text);
            ActivityLog.Info("Activity log copied to the clipboard.");
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Clipboard copy failed: {ex.Message}");
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Shift | Keys.C))
        {
            CopyActivityLog();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _monitor.Sampled += OnSample;
        _monitor.Start();
        UpdatesState.Changed += OnUpdatesChanged;
        UpdateUpdatesTile();
        RefreshData();
        _ = SyncCpuButtonAsync();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _monitor.Sampled -= OnSample;
        _monitor.Stop();
        UpdatesState.Changed -= OnUpdatesChanged;
        base.OnHandleDestroyed(e);
    }

    public override void RefreshData()
    {
        _profile.Clear();
        _profile.Add("PROCESSOR", MachineInfo.CpuName);
        _profile.Add("GRAPHICS", MachineInfo.GraphicsText);
        _profile.Add("HOSTNAME", MachineInfo.MachineName);
        _profile.Add("OPERATING SYSTEM", MachineInfo.OsName);
        _profile.Add("SYSTEM DRIVE", $"{MachineInfo.SystemDrive}  {Theme.Bytes(MachineInfo.DriveFree)} free");
        _address = MachineInfo.LocalAddressText;
        _profile.Add("IP ADDRESS", _address, Theme.Accent);
        _profile.Add("POWER PLAN", PowerService.ActivePlanName);

        UpdateDiskTile();
    }

    /// <summary>Caps the processor ceiling at 70%, or lifts the cap again, tracking the real plan state.</summary>
    private async Task ToggleCpuCeilingAsync()
    {
        _coolCpu.Enabled = false;
        using IDisposable busy = Working();

        bool capped = await Task.Run(() => PowerService.CpuCapped);
        (bool ok, string message) = await Task.Run(() =>
            capped ? PowerService.RestoreCpuCeiling() : PowerService.CapCpu(70));

        ActivityLog.Write(ok ? LogLevel.Success : LogLevel.Warn, message);
        if (!ok && !Native.IsElevated) ActivityLog.Info("Processor ceilings live in the power plan and need an elevated session.");

        if (!IsDisposed) await SyncCpuButtonAsync();
    }

    private async Task SyncCpuButtonAsync()
    {
        int ceiling = await Task.Run(() => PowerService.CpuCeiling);
        if (IsDisposed) return;

        _coolCpu.Text = ceiling < 100 ? "RESTORE CPU" : "REDUCE CPU";
        _coolCpu.Tone = ceiling < 100 ? Theme.Good : Theme.Warn;
        _coolCpu.Enabled = true;
        _coolCpu.Invalidate();
    }

    /// <summary>Shows the result on the button itself for a few seconds, then restores its label.</summary>
    private static void Flash(NeonButton button, string text, string restore)
    {
        button.Text = text;

        var timer = new System.Windows.Forms.Timer { Interval = 6000 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            if (!button.IsDisposed) button.Text = restore;
        };
        timer.Start();
    }

    private void OnUpdatesChanged()
    {
        if (IsDisposed) return;

        if (InvokeRequired)
        {
            BeginInvoke(OnUpdatesChanged);
            return;
        }

        UpdateUpdatesTile();
    }

    private void UpdateUpdatesTile()
    {
        if (!UpdatesState.Scanned)
        {
            _updates.Value = "\u2014";
            _updates.Sub = "scanning updates";
            _updates.Progress = 0;
            _updates.Tone = Theme.Accent;
        }
        else
        {
            _updates.Value = UpdatesState.Total.ToString();
            _updates.Sub = UpdatesState.Scanning
                ? $"{UpdatesState.Apps} apps \u2022 checking drivers"
                : UpdatesState.Total == 0
                    ? "everything is current"
                    : $"{UpdatesState.Apps} apps \u2022 {UpdatesState.Drivers} drivers";
            _updates.Progress = 0;
            _updates.Tone = UpdatesState.Scanning
                ? Theme.Accent
                : UpdatesState.Total == 0 ? Theme.Good : Theme.Warn;
        }

        _updates.Invalidate();
    }

    private void UpdateDiskTile()
    {
        if (MachineInfo.DriveTotal == 0)
        {
            _disk.Value = "n/a";
            _disk.Sub = "drive not readable";
            return;
        }

        double used = 1 - ((double)MachineInfo.DriveFree / MachineInfo.DriveTotal);
        _disk.Value = Theme.Bytes(MachineInfo.DriveFree);
        _disk.Sub = $"{used * 100:0}% used of {Theme.Bytes(MachineInfo.DriveTotal)}";
        _disk.Progress = used;
        _disk.Tone = used > 0.92 ? Theme.Bad : used > 0.8 ? Theme.Warn : Theme.Good;
        _disk.Invalidate();
    }

    private void OnSample(SystemSample sample)
    {
        _cpu.Value = $"{sample.CpuPercent:0}%";
        _cpu.Progress = sample.CpuPercent / 100;
        _cpu.Sub = $"{MachineInfo.LogicalCpus} logical processors";

        _memory.Value = $"{sample.MemoryPercent:0}%";
        _memory.Progress = sample.MemoryPercent / 100;
        _memory.Sub = $"{Theme.Bytes(sample.MemoryUsed)} / {Theme.Bytes(sample.MemoryTotal)}";

        _uptime.Value = Theme.Duration(sample.Uptime);
        _uptime.Sub = $"since {MachineInfo.LastBoot:ddd HH:mm}";

        // DHCP renewals move the address; rebuild only when it actually changes.
        if (++_ticks % 5 == 0 && MachineInfo.LocalAddressText != _address) RefreshData();
    }
}
