namespace PC_Medic;

internal sealed partial class SleepPage : PageBase
{
    private bool _loading;

    public SleepPage()
        : base("OPTIMIZE SLEEP", "One coherent idle ladder instead of half-sleep and surprise wake-ups.")
    {
        InitializeComponent();

        void LayoutHero()
        {
            int width = Math.Max(240, _hero.ClientSize.Width - 36);
            _heroBody.SetBounds(18, 56, width, 44);
            _heroStatus.Location = new Point(19, 105);
            _apply.SetBounds(18, 130, _apply.Width, _apply.Height);
            _restore.SetBounds(_apply.Right + 10, 130, _restore.Width, _restore.Height);
        }

        _hero.Resize += (_, _) => LayoutHero();
        LayoutHero();

        StepperRow[] steppers = { _screen, _sleep, _hibernate, _disk };

        void LayoutLadder()
        {
            int width = Math.Max(200, _ladder.ClientSize.Width - 36);
            int y = 58;
            foreach (StepperRow row in steppers)
            {
                row.SetBounds(18, y, width, 58);
                y += 62;
            }
        }

        _ladder.Resize += (_, _) => LayoutLadder();
        LayoutLadder();

        ToggleRow[] toggles = { _wake, _usb, _hybrid, _keepAwake };

        void LayoutBehavior()
        {
            int width = Math.Max(200, _behavior.ClientSize.Width - 36);
            int y = 58;
            foreach (ToggleRow row in toggles)
            {
                row.SetBounds(18, y, width, 62);
                y += 68;
            }

            _settingsNote.SetBounds(18, y + 4, width, 20);
        }

        _behavior.Resize += (_, _) => LayoutBehavior();
        LayoutBehavior();

        _apply.Click += async (_, _) =>
        {
            _apply.Enabled = false;
            using IDisposable busy = Working();
            SetHero("Applying optimized sleep ladder\u2026", Theme.TextDim);
            List<PowerStep> steps = await PowerService.OptimizeSleepAsync();
            int ok = steps.Count(step => step.Ok);
            bool all = ok == steps.Count;
            SetHero($"{ok}/{steps.Count} steps applied. " + (all ? "Sleep profile active." : "Remaining steps need elevation."),
                all ? Theme.Good : Theme.Warn);
            _apply.Enabled = true;
            LoadValues();
        };

        _restore.Click += async (_, _) =>
        {
            _restore.Enabled = false;
            using IDisposable busy = Working();
            (bool ok, string message) = await Task.Run(PowerService.RestoreBackup);
            ActivityLog.Write(ok ? LogLevel.Success : LogLevel.Warn, message);
            SetHero(message, ok ? Theme.Good : Theme.Warn);
            _restore.Enabled = true;
            LoadValues();
        };

        _screen.ValueChanged += (_, minutes) => WriteSetting(PowerService.ScreenOff, minutes);
        _sleep.ValueChanged += (_, minutes) => WriteSetting(PowerService.SleepAfter, minutes);
        _hibernate.ValueChanged += (_, minutes) => WriteSetting(PowerService.HibernateAfter, minutes);
        _disk.ValueChanged += (_, minutes) => WriteSetting(PowerService.DiskIdle, minutes);

        _wake.Toggled += (row, on) => WriteFlag(PowerService.WakeTimers, row, on, "Wake timers");
        _usb.Toggled += (row, on) => WriteFlag(PowerService.UsbSuspend, row, on, "USB selective suspend");
        _hybrid.Toggled += (row, on) => WriteFlag(PowerService.HybridSleep, row, on, "Hybrid sleep");
        _keepAwake.Toggled += (_, on) =>
        {
            KeepAwake.SetManual(on, keepDisplayAwake: true);
            ActivityLog.Info(on
                ? "Keep-awake on — the machine will not sleep while PC Medic runs."
                : "Keep-awake off — normal sleep behaviour restored.");
        };
    }

    public override void OnNavigated() => LoadValues();

    public override void RefreshData() => LoadValues();

    private void SetHero(string text, Color tone)
    {
        _heroStatus.Text = text;
        _heroStatus.ForeColor = tone;
    }

    private async void LoadValues()
    {
        if (_loading) return;
        _loading = true;
        using IDisposable busy = Working();

        try
        {
            var snapshot = await Task.Run(() => new
            {
                Screen = PowerService.Read(PowerService.ScreenOff),
                Sleep = PowerService.Read(PowerService.SleepAfter),
                Hibernate = PowerService.Read(PowerService.HibernateAfter),
                Disk = PowerService.Read(PowerService.DiskIdle),
                Wake = PowerService.Read(PowerService.WakeTimers),
                Usb = PowerService.Read(PowerService.UsbSuspend),
                Hybrid = PowerService.Read(PowerService.HybridSleep),
            });

            if (IsDisposed) return;

            _screen.Value = (snapshot.Screen.Ac ?? 0) / 60;
            _sleep.Value = (snapshot.Sleep.Ac ?? 0) / 60;
            _hibernate.Value = (snapshot.Hibernate.Ac ?? 0) / 60;
            _disk.Value = (snapshot.Disk.Ac ?? 0) / 60;
            _wake.Checked = snapshot.Wake.Ac is > 0;
            _usb.Checked = snapshot.Usb.Ac is > 0;
            _hybrid.Checked = snapshot.Hybrid.Ac is > 0;

            foreach (ToggleRow row in new[] { _wake, _usb, _hybrid, _keepAwake }) row.Invalidate();
            _keepAwake.Checked = KeepAwake.ManualActive;

            _hibernate.Enabled = MachineInfo.HibernateAvailable;
            _hybrid.Enabled = MachineInfo.HibernateAvailable;

            SetHero(
                $"Plan: {PowerService.ActivePlanName}  \u2022  hibernate {(MachineInfo.HibernateAvailable ? "available" : "unavailable")}" +
                (Native.IsElevated ? "  \u2022  elevated" : "  \u2022  standard user"),
                Theme.TextDim);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Power query failed: {ex.Message}");
            SetHero("Power policy could not be read.", Theme.Bad);
        }
        finally
        {
            _loading = false;
        }
    }

    private async void WriteSetting(PowerSetting setting, int minutes)
    {
        if (_loading) return;

        int seconds = Math.Max(0, minutes * 60);
        (bool ok, string message) = await Task.Run(() => PowerService.Apply(setting, seconds));

        if (!ok)
        {
            SetHero($"{setting.Label}: {message}", Theme.Bad);
            ActivityLog.Error($"{setting.Label}: {message}");
            LoadValues();
            return;
        }

        ActivityLog.Success($"{setting.Label} set to {(minutes == 0 ? "never" : minutes + " min")}.");
        SetHero($"{setting.Label} \u2192 {(minutes == 0 ? "never" : minutes + " min")}", Theme.Good);
    }

    private async void WriteFlag(PowerSetting setting, ToggleRow row, bool on, string label)
    {
        if (_loading) return;

        int value = on ? 1 : 0;
        (bool ok, string message) = await Task.Run(() => PowerService.Apply(setting, value));

        if (!ok)
        {
            row.Checked = !on;
            row.Invalidate();
            SetHero($"{label}: {message}", Theme.Bad);
            ActivityLog.Error($"{label}: {message}");
            return;
        }

        ActivityLog.Success($"{label} {(on ? "enabled" : "disabled")}.");
        SetHero($"{label} {(on ? "enabled" : "disabled")}.", Theme.Good);
    }
}
