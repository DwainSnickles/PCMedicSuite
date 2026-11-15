namespace PC_Medic;

/// <summary>
/// Startup &amp; boot: one surface for everything that runs at sign-in — Run keys, Startup-folder
/// shortcuts and automatic services — with the boot metrics and the Fast Startup switch above the
/// list. The separate Boot page used to show the same inventory a second time.
/// </summary>
internal sealed partial class StartupPage : PageBase
{
    /// <summary>The service registry can hold hundreds of rows; the first slice keeps the list usable.</summary>
    private const int ServiceLimit = 150;

    private List<StartupEntry> _entries = new();
    private List<ServiceEntry> _services = new();
    private int _filter;

    public StartupPage()
        : base("STARTUP & BOOT", "Everything that runs at sign-in \u2014 startup apps and auto-start services \u2014 with boot metrics and Fast Startup.")
    {
        InitializeComponent();

        void LayoutEngine()
        {
            int width = Math.Max(120, _engineCard.ClientSize.Width - 36);
            _engine.SetBounds(18, 58, width, Math.Max(28, _engine.Height));
        }

        _engineCard.Resize += (_, _) => LayoutEngine();
        LayoutEngine();

        void LayoutFast()
        {
            int width = Math.Max(120, _fastCard.ClientSize.Width - 36);
            _fast.SetBounds(18, 58, width, 62);
            _fastNote.SetBounds(18, 126, width, 32);
        }

        _fastCard.Resize += (_, _) => LayoutFast();
        LayoutFast();

        void LayoutBoot()
        {
            int width = Math.Max(120, _bootCard.ClientSize.Width - 36);
            int half = Math.Max(48, (width - 8) / 2);
            _safeMode.SetBounds(18, 58, half, 32);
            _repairMode.SetBounds(18 + half + 8, 58, Math.Max(48, width - half - 8), 32);
            _bootState.SetBounds(18, 96, width, 18);
            _bootNote.SetBounds(18, 116, width, Math.Max(26, _bootCard.ClientSize.Height - 122));
        }

        _bootCard.Resize += (_, _) => LayoutBoot();
        LayoutBoot();

        void LayoutToolbar()
        {
            int right = _toolbar.ClientSize.Width;
            foreach (NeonButton button in new[] { _rescan, _delayThirdParty, _disabled, _servicesOnly, _apps, _all })
            {
                right -= button.Width;
                button.SetBounds(right, 4, button.Width, 30);
                right -= 8;
            }

            _status.SetBounds(2, 13, Math.Max(80, right - 4), 18);
        }

        _toolbar.Resize += (_, _) => LayoutToolbar();
        LayoutToolbar();

        _all.Click += (_, _) => SetFilter(0);
        _apps.Click += (_, _) => SetFilter(1);
        _servicesOnly.Click += (_, _) => SetFilter(2);
        _disabled.Click += (_, _) => SetFilter(3);
        _rescan.Click += (_, _) => Reload();
        _delayThirdParty.Click += (_, _) => DelayThirdParty();
        _fast.Toggled += OnFastStartupToggled;
        _safeMode.Click += (_, _) => OnSafeMode();
        _repairMode.Click += (_, _) => OnRepairMode();

        _tips.SetToolTip(
            _safeMode,
            "Set safe mode for the next start and restart now. Windows loads minimal drivers and services.\n" +
            "Safe mode stays set until it is cleared - press the same button again to clear it.");
        _tips.SetToolTip(
            _repairMode,
            "Restart into Windows Recovery (Advanced startup), where Startup Repair, System Restore,\n" +
            "Reset this PC and Command Prompt are.");

        _list.Resize += (_, _) =>
        {
            foreach (Control control in _list.Controls)
            {
                control.Width = RowWidth();
            }
        };

        SyncFilterButtons();
    }

    public override void OnNavigated() => Reload(announce: false);

    public override void RefreshData() => Reload(announce: false);

    private int RowWidth() => Math.Max(220, _list.ClientSize.Width - 24);

    private void SetFilter(int filter)
    {
        _filter = filter;
        SyncFilterButtons();
        Rebuild();
    }

    private void SyncFilterButtons()
    {
        _all.Primary = _filter == 0;
        _apps.Primary = _filter == 1;
        _servicesOnly.Primary = _filter == 2;
        _disabled.Primary = _filter == 3;
        _all.Invalidate();
        _apps.Invalidate();
        _servicesOnly.Invalidate();
        _disabled.Invalidate();
    }

    private void Reload(bool announce = true)
    {
        _entries = StartupService.Load();
        _services = WindowsServices.AutoStartServices();

        _fast.Checked = PowerService.FastStartupEnabled;
        _fast.Invalidate();

        UpdateEngine();
        UpdateBootModes();
        Rebuild();
        SyncFilterButtons();
        UpdateStatus();

        if (announce)
        {
            ActivityLog.Info(
                $"Startup scan: {_entries.Count} apps ({_entries.Count(entry => entry.Enabled)} enabled), " +
                $"{_services.Count} auto services, fast startup {(_fast.Checked ? "on" : "off")}.");
        }
    }

    private void UpdateEngine()
    {
        _engine.Clear();
        _engine.Add("LAST BOOT", MachineInfo.LastBoot.ToString("ddd dd MMM  HH:mm"));
        _engine.Add("UPTIME", Theme.Duration(Native.Uptime));
        _engine.Add("HIBERNATION", MachineInfo.HibernateAvailable ? "available" : "unavailable",
            MachineInfo.HibernateAvailable ? Theme.Good : Theme.Warn);
    }

    private void Rebuild()
    {
        _list.SuspendLayout();
        foreach (Control control in _list.Controls.Cast<Control>().ToArray()) control.Dispose();
        _list.Controls.Clear();

        List<StartupEntry> apps;
        if (_filter == 1) apps = _entries;                                        // APPS
        else if (_filter == 2) apps = new List<StartupEntry>();                   // SERVICES only
        else if (_filter == 3) apps = _entries.Where(entry => !entry.Enabled).ToList(); // DISABLED
        else apps = _entries;

        List<ServiceEntry> services = _filter is 1 or 3
            ? new List<ServiceEntry>()
            : _services.Take(ServiceLimit).ToList();

        int y = 6;
        int shown = 0;

        if (apps.Count > 0)
        {
            string appHeader = _filter == 3
                ? $"STARTUP APPS  \u2022  {apps.Count} DISABLED"
                : $"STARTUP APPS  \u2022  {apps.Count(entry => entry.Enabled)} OF {apps.Count} ENABLED";
            y = AddHeader(appHeader, y);
            foreach (StartupEntry entry in apps)
            {
                ToggleRow row = BuildAppRow(entry);
                row.SetBounds(4, y, RowWidth(), 62);
                _list.Controls.Add(row);
                y += 68;
                shown++;
            }
        }

        if (services.Count > 0)
        {
            if (shown > 0) y += 8;

            string header = $"AUTO-START SERVICES  \u2022  {services.Count(service => service.Delayed)} OF {services.Count} DELAYED";
            if (_services.Count > ServiceLimit) header += $"  \u2022  SHOWING {ServiceLimit} OF {_services.Count}";
            y = AddHeader(header, y);

            foreach (ServiceEntry service in services)
            {
                ToggleRow row = BuildServiceRow(service);
                row.SetBounds(4, y, RowWidth(), 62);
                _list.Controls.Add(row);
                y += 68;
                shown++;
            }
        }

        if (shown == 0)
        {
            _list.Controls.Add(new Label
            {
                Text = "Nothing matches this filter.",
                Font = Theme.MonoSmall,
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(8, 12),
            });
            y += 40;
        }

        _list.AutoScrollMinSize = new Size(0, y + 6);
        _list.ResumeLayout();
    }

    private int AddHeader(string text, int y)
    {
        _list.Controls.Add(new Label
        {
            Text = text,
            Font = Theme.Kicker,
            ForeColor = Theme.TextFaint,
            BackColor = Color.Transparent,
            AutoSize = false,
            Height = 18,
            Width = RowWidth(),
            Location = new Point(6, y),
            TextAlign = ContentAlignment.MiddleLeft,
        });

        return y + 24;
    }

    private ToggleRow BuildAppRow(StartupEntry entry)
    {
        bool locked = entry.MachineWide && !Native.IsElevated;
        string subtitle = $"{entry.Source}  \u2022  {entry.Command}";
        if (locked) subtitle += "  \u2022  needs admin";

        var row = new ToggleRow
        {
            Title = entry.Name,
            Subtitle = subtitle,
            BadgeText = entry.ImpactLabel,
            BadgeTone = entry.ImpactTone,
            Tone = entry.Enabled ? Theme.Accent : Theme.TextDim,
            Checked = entry.Enabled,
            Enabled = !locked,
            Tag = entry,
        };

        row.Toggled += OnRowToggled;

        _tips.SetToolTip(
            row,
            locked
                ? $"{entry.Name}\r\n{entry.Source}\r\n{entry.Command}\r\nNeeds an elevated run to change."
                : $"{entry.Name}\r\n{entry.Source}\r\n{entry.Command}");

        return row;
    }

    private ToggleRow BuildServiceRow(ServiceEntry service)
    {
        bool locked = !Native.IsElevated || !service.ThirdParty;

        var row = new ToggleRow
        {
            Title = service.DisplayName,
            Subtitle = ServiceSubtitle(service),
            BadgeText = service.Delayed ? "DELAYED" : "BOOT",
            BadgeTone = service.Delayed ? Theme.Good : Theme.Warn,
            Tone = service.Delayed ? Theme.Good : Theme.Warn,
            Checked = service.Delayed,
            Enabled = !locked,
            Tag = service,
        };

        row.Toggled += OnServiceToggled;

        _tips.SetToolTip(
            row,
            $"{service.DisplayName}\r\n{service.Name}  \u2022  " +
            $"{(service.ThirdParty ? "third-party" : "windows")}  \u2022  " +
            $"{(service.Delayed ? "delayed start" : "immediate start")}\r\n{service.ImagePath}" +
            (locked
                ? service.ThirdParty
                    ? "\r\nNeeds an elevated run to change."
                    : "\r\nWindows services are left alone."
                : string.Empty));

        return row;
    }

    private static string ServiceSubtitle(ServiceEntry service) =>
        $"{service.Name}  \u2022  {(service.ThirdParty ? "third-party" : "windows")}  \u2022  " +
        $"{(service.Delayed ? "delayed start" : "immediate start")}";

    private void OnRowToggled(ToggleRow row, bool enabled)
    {
        if (row.Tag is not StartupEntry entry) return;

        (bool ok, string message) = StartupService.SetEnabled(entry, enabled);
        if (!ok)
        {
            row.Checked = entry.Enabled;
            ActivityLog.Error($"{entry.Name}: {message}");
            UpdateStatus(message, Theme.Bad);
            return;
        }

        entry.Enabled = enabled;
        row.Tone = enabled ? Theme.Accent : Theme.TextDim;
        row.Invalidate();
        ActivityLog.Success($"{entry.Name} {message.ToLowerInvariant()}.");
        UpdateStatus($"{entry.Name} {message.ToLowerInvariant()}", Theme.Good);
    }

    private void OnServiceToggled(ToggleRow row, bool delayed)
    {
        if (row.Tag is not ServiceEntry service) return;

        (bool ok, string message) = WindowsServices.SetDelayed(service, delayed);
        if (!ok)
        {
            row.Checked = service.Delayed;
            ActivityLog.Error($"{service.DisplayName}: {message}");
            UpdateStatus(message, Theme.Bad);
            return;
        }

        service.Delayed = delayed;
        row.BadgeText = delayed ? "DELAYED" : "BOOT";
        row.BadgeTone = delayed ? Theme.Good : Theme.Warn;
        row.Tone = delayed ? Theme.Good : Theme.Warn;
        row.Subtitle = ServiceSubtitle(service);
        row.Invalidate();
        ActivityLog.Success($"{service.DisplayName}: {message.ToLowerInvariant()} start.");
        UpdateStatus($"{service.DisplayName}: {message.ToLowerInvariant()} start", Theme.Good);
    }

    private void OnFastStartupToggled(ToggleRow row, bool enabled)
    {
        if (!Native.IsElevated)
        {
            row.Checked = PowerService.FastStartupEnabled;
            UpdateStatus("fast startup needs an elevated run \u2014 use the sidebar", Theme.Warn);
            return;
        }

        if (enabled && !MachineInfo.HibernateAvailable)
        {
            row.Checked = false;
            UpdateStatus("enable hibernation first (powercfg /hibernate on)", Theme.Warn);
            ActivityLog.Warn("Fast Startup needs hibernation, which is currently disabled.");
            return;
        }

        (bool ok, string message) = PowerService.SetFastStartup(enabled);
        if (!ok)
        {
            row.Checked = PowerService.FastStartupEnabled;
            UpdateStatus(message, Theme.Bad);
            return;
        }

        ActivityLog.Success(message + ".");
        UpdateStatus(message, Theme.Good);
        UpdateEngine();
    }

    private void DelayThirdParty()
    {
        if (!Native.IsElevated)
        {
            UpdateStatus("delaying services needs an elevated run", Theme.Warn);
            return;
        }

        List<ServiceEntry> targets = _services.Where(service => service.ThirdParty && !service.Delayed).ToList();
        if (targets.Count == 0)
        {
            UpdateStatus("no third-party services left to delay", Theme.TextDim);
            return;
        }

        int succeeded = 0;
        foreach (ServiceEntry service in targets)
        {
            (bool ok, string _) = WindowsServices.SetDelayed(service, true);
            if (ok)
            {
                service.Delayed = true;
                succeeded++;
            }
        }

        ActivityLog.Success($"Delayed {succeeded} of {targets.Count} third-party auto services.");
        Rebuild();
        UpdateStatus($"delayed {succeeded} / {targets.Count} services",
            succeeded == targets.Count ? Theme.Good : Theme.Warn);
    }

    private void OnSafeMode()
    {
        if (!Native.IsElevated)
        {
            RequireElevation("Safe mode");
            return;
        }

        // Armed already: the useful action is the way back, not arming it twice.
        if (BootModes.SafeModeArmed() == true)
        {
            (bool cleared, string clearMessage) = BootModes.ClearSafeMode();
            ActivityLog.Write(cleared ? LogLevel.Success : LogLevel.Error, clearMessage + ".");
            UpdateStatus(clearMessage, cleared ? Theme.Good : Theme.Bad);
            UpdateBootModes();
            return;
        }

        DialogResult confirm = MessageBox.Show(
            this,
            "Restart into Safe Mode?\n\n" +
            "Windows will start with minimal drivers and services. Safe mode stays switched on until it is " +
            "cleared on this page, so a second visit is needed to get back to a normal start.\n\n" +
            "Anything unsaved will be closed. The restart happens 10 seconds after confirming.",
            "Safe mode",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes) return;

        (bool armed, string message) = BootModes.ArmSafeMode();
        if (!armed)
        {
            ActivityLog.Error(message + ".");
            UpdateStatus(message, Theme.Bad);
            MessageBox.Show(this, message, "Safe mode", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        (bool _, string restartMessage) = BootModes.RestartNow();
        ActivityLog.Success($"{message}; {restartMessage}.");
        UpdateStatus($"safe mode set \u2014 {restartMessage}", Theme.Good);
        UpdateBootModes();
    }

    private void OnRepairMode()
    {
        if (!Native.IsElevated)
        {
            RequireElevation("Repair mode");
            return;
        }

        DialogResult confirm = MessageBox.Show(
            this,
            "Restart into Windows Recovery?\n\n" +
            "The PC restarts into Advanced startup, where Startup Repair, System Restore, Reset this PC " +
            "and Command Prompt are. The restart happens 10 seconds after confirming.",
            "Repair mode",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes) return;

        (bool ok, string message) = BootModes.RestartIntoRepair();
        ActivityLog.Write(ok ? LogLevel.Success : LogLevel.Error, message + ".");
        UpdateStatus(message, ok ? Theme.Good : Theme.Bad);
        if (!ok) MessageBox.Show(this, message, "Repair mode", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>Both restart options write the boot configuration, so both stop here without rights.</summary>
    private void RequireElevation(string action)
    {
        const string message =
            "Changing the boot configuration needs administrator rights.\n\n" +
            "Close PC Medic, start it again with Right-click > Run as administrator, then press the button again.";

        ActivityLog.Warn($"{action} not attempted: PC Medic is not elevated.");
        UpdateStatus($"{action} needs an elevated run", Theme.Warn);
        MessageBox.Show(this, message, action, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Shows what the next start will be, and labels the button with the way out of safe mode.</summary>
    private void UpdateBootModes()
    {
        bool? armed = BootModes.SafeModeArmed();

        _safeMode.Text = armed == true ? "CLEAR SAFE" : "SAFE MODE";
        _bootState.Text = armed switch
        {
            true => "SAFE MODE ARMED \u2014 NEXT START IS SAFE MODE",
            false => "NORMAL BOOT",
            _ => "BOOT STORE UNREADABLE \u2014 RUN AS ADMIN",
        };
        _bootState.ForeColor = armed switch
        {
            true => Theme.Warn,
            false => Theme.Good,
            _ => Theme.TextDim,
        };
    }

    private void UpdateStatus(string? message = null, Color? tone = null)
    {
        int enabled = _entries.Count(entry => entry.Enabled);
        string scope = Native.IsElevated ? "elevated" : "standard user";

        _status.Text =
            $"{_entries.Count} APPS  \u2022  {enabled} ENABLED  \u2022  {_services.Count} AUTO SERVICES  \u2022  " +
            $"{_services.Count(service => service.Delayed)} DELAYED  \u2022  {scope.ToUpperInvariant()}" +
            (message is null ? string.Empty : $"  \u2022  {message.ToUpperInvariant()}");
        _status.ForeColor = tone ?? Theme.TextDim;
    }
}
