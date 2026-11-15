namespace PC_Medic;

/// <summary>
/// The shell: fixed sidebar, fixed header, docked page host. No flow layouts anywhere.
/// </summary>
internal sealed partial class MainForm : Form
{
    private string _currentPageKey = "overview";
    private readonly List<NavItem> _nav = new();
    private NavItem? _updatesNav;
    private readonly Dictionary<string, PageBase> _pages = new();
    private readonly SystemMonitor _trayMonitor = new();
    private bool _trayHintShown;

    public MainForm()
    {
        InitializeComponent();

        try
        {
            Icon = Brand.CreateIcon(64);
        }
        catch
        {
            // Icon rendering is cosmetic; never let it block startup.
        }

        BuildSidebar();
        BuildHeader();
        BuildContent();
        BuildNav();

        ShowPage("overview");

        _clockTimer.Tick += (_, _) =>
        {
            _clock.Text = DateTime.Now.ToString("HH:mm:ss");
            _planBadge.SetText(PowerService.ActivePlanName.ToUpperInvariant());
        };
        _clockTimer.Start();
        _clock.Text = DateTime.Now.ToString("HH:mm:ss");

        BuildTray();

        UpdatesState.Changed += OnUpdatesChanged;
        ShowUpdatesBadge();
        UpdatesState.BeginScan();
    }

    /// <summary>Tray presence, so the console can be parked and reopened without relaunching.</summary>
    private void BuildTray()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = Theme.Surface2,
            ForeColor = Theme.Text,
            Font = Theme.Ui,
            ShowImageMargin = false,
            Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors()),
        };

        menu.Items.Add("Open PC MEDIC", null, (_, _) => ShowFromTray());
        menu.Items.Add("Check for updates", null, (_, _) => ShowSelfUpdate());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit PC MEDIC", null, (_, _) => Close());

        _tray.Text = "PC MEDIC \u2014 System Optimizer";
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowFromTray();

        try
        {
            _tray.Icon = Brand.CreateIcon(32);
        }
        catch
        {
            // Without a rendered icon the tray entry stays hidden; the shell still works.
        }

        _tray.Visible = _tray.Icon is not null;

        // The tooltip is the only surface visible while the console is parked, so it carries live telemetry.
        _trayMonitor.Sampled += OnTraySample;
        _trayMonitor.Start();
    }

    /// <summary>Mirrors the dashboard sampler into the tray tooltip: CPU load, memory load and used / total.</summary>
    private void OnTraySample(SystemSample sample)
    {
        if (IsDisposed || !_tray.Visible) return;
        _tray.Text = TrayText(sample);
    }

    /// <summary>One-line telemetry for the tray tooltip. Kept well under the NotifyIcon 63-character cap.</summary>
    internal static string TrayText(SystemSample sample) =>
        $"PC MEDIC \u2014 CPU {sample.CpuPercent:0}% \u00b7 RAM {sample.MemoryPercent:0}%" +
        $" \u00b7 {Theme.Bytes(sample.MemoryUsed)} / {Theme.Bytes(sample.MemoryTotal)}";

    /// <summary>Confirms a second launch was redirected to this console instead of opening another.</summary>
    internal void NotifySecondLaunch()
    {
        if (IsDisposed || !_tray.Visible) return;

        _tray.ShowBalloonTip(
            2200,
            "PC MEDIC is already running",
            "The console was brought forward instead of starting a second copy.",
            ToolTipIcon.None);
    }

    /// <summary>Brings the console back from the tray or the taskbar.</summary>
    internal void ShowFromTray()
    {
        if (InvokeRequired)
        {
            BeginInvoke(ShowFromTray);
            return;
        }

        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState != FormWindowState.Minimized || !_tray.Visible) return;

        Hide();
        ShowInTaskbar = false;

        if (_trayHintShown) return;
        _trayHintShown = true;
        _tray.ShowBalloonTip(
            2200,
            "PC MEDIC is still running",
            "Double-click the tray icon to reopen the console.",
            ToolTipIcon.None);
    }

    private static Label MakeLabel(string text, Font font, Color color) => new()
    {
        Text = text,
        Font = font,
        ForeColor = color,
        BackColor = Color.Transparent,
        AutoSize = true,
    };

    private void BuildSidebar()
    {
        _sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
        };

        _sidebarBottom.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 22, 0, _sidebarBottom.Width - 22, 0);
        };

        _elevationState.Text = Native.IsElevated ? "ADMINISTRATOR" : "STANDARD USER";
        _elevationState.ForeColor = Native.IsElevated ? Theme.Good : Theme.Warn;

        _sidebarElevate.Text = Native.IsElevated ? "RUNNING ELEVATED" : "RESTART AS ADMIN";
        _sidebarElevate.Enabled = !Native.IsElevated;
        _sidebarElevate.Click += (_, _) => ElevateNow();

        _checkUpdatesButton.Click += (_, _) => ShowSelfUpdate();

        _adminBadge.SetText(Native.IsElevated ? "ELEVATED" : "STANDARD");
        _adminBadge.Tone = Native.IsElevated ? Theme.Good : Theme.Warn;
    }

    /// <summary>Relaunches through UAC. The console name is released first, or the new copy exits as a duplicate.</summary>
    private void ElevateNow()
    {
        if (Native.IsElevated) return;

        Program.ReleaseInstance();

        if (Native.RestartElevated())
        {
            Close();
            return;
        }

        Program.ReclaimInstance();
        ActivityLog.Warn("Elevation was cancelled — still running limited.");
    }
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        LayoutHeader();
    }

    private void BuildHeader()
    {
        _header.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.BorderSoft);
            e.Graphics.DrawLine(pen, 28, _header.Height - 1, _header.Width - 26, _header.Height - 1);
        };

        _adminBadge.Tone = Native.IsElevated ? Theme.Good : Theme.Warn;
        _planBadge.Tone = Theme.Accent;
        _planBadge.SetText(PowerService.ActivePlanName.ToUpperInvariant());

        _elevate.Visible = !Native.IsElevated;
        _tips.SetToolTip(_elevate, "Restarts PC MEDIC through UAC so repairs, machine-wide tweaks and Disk Defrag can run.");
        _elevate.Click += (_, _) => ElevateNow();
        _tips.SetToolTip(_minimize, "Minimize to Notify Icon");
        _minimize.Click += (_, _) => WindowState = FormWindowState.Minimized;

        _tips.SetToolTip(_help, "Help for this page (F1) — what it reads, what it writes, and what it will not do.");
        _help.Click += (_, _) => OpenHelp();

        _header.Resize += (_, _) => LayoutHeader();
    }

    private void LayoutHeader()
    {
        int right = _header.ClientSize.Width - 26;
        _minimize.SetBounds(right - _minimize.Width, 24, _minimize.Width, 30);
        right -= _minimize.Width + 8;
        _help.SetBounds(right - _help.Width, 25, _help.Width, 28);
        right -= _help.Width + 14;
        _clock.Location = new Point(right - _clock.Width, 27);
        right -= _clock.Width + 16;
        if (_elevate.Visible)
        {
            _elevate.SetBounds(right - _elevate.Width, 26, _elevate.Width, 28);
            right -= _elevate.Width + 12;
        }
        _adminBadge.Location = new Point(right - _adminBadge.Width, 31);
        right -= _adminBadge.Width + 10;
        _planBadge.Location = new Point(right - _planBadge.Width, 31);
    }

    private void BuildContent()
    {
        _content.SizeChanged += (_, _) => LayoutBusyBar();
        _content.Layout += (_, _) => LayoutBusyBar();
        LayoutBusyBar();

        Busy.Indicator = _busyBar;
    }

    private void LayoutBusyBar()
    {
        int width = Math.Max(160, _content.ClientSize.Width - 54);
        int top = Math.Max(0, _content.ClientSize.Height - _busyBar.Height - 16);
        _busyBar.SetBounds(28, top, width, _busyBar.Height);
    }

    private void BuildNav()
    {
        // The caption alone says where a page goes, so the tip says what the page is for.
        (NavItem Item, string Hint)[] entries =
        {
            (_navOverview, "Live CPU, memory, disk, update and uptime telemetry with one-click cleanup actions."),
            (_navStartup, "Everything that runs at sign-in — Run keys, Startup folders and auto-start services — with boot metrics and Fast Startup."),
            (_navSleep, "Sleep, hibernate and power-plan settings through powercfg, with a backup to restore from."),
            (_navMaintenance, "Fifteen recovery and cleanup tools in one grid: SFC, DISM, restore points, Storage Sense, Disk Defrag."),
            (_navBackup, "WIM disk images with wimlib, bare-metal restore with boot repair, and component-store health checks."),
            (_navTweaks, "Explorer and visual-effects tweaks, each written with a backup so it can be reverted."),
            (_navSoftware, "Full app inventory with security-flagged updates, pins, ignore rules and scheduled scans."),
            (_navCatalogue, "A curated list of common Windows software — tick what you want and install it silently."),
            (_navUpdates, "winget application upgrades and Windows Update driver scans, sharing one pending count."),
            (_navAudit, "Registry keys and shortcuts whose targets are gone, with review rows for what could not be resolved."),
            (_navBenchmarks, "Quick CPU and disk sanity runs; a missing benchmark rig installs from the page itself."),
            (_navConsole, "An embedded console for the tools the pages do not wrap."),
        };

        foreach ((NavItem item, string hint) in entries)
        {
            _tips.SetToolTip(item, hint);
            item.Click += (_, _) => ShowPage(item.Key);
            _nav.Add(item);

            if (item.Key == "updates") _updatesNav = item;
        }
    }

    private void ShowSelfUpdate()
    {
        if (!Visible) ShowFromTray();

        using var dialog = new SelfUpdateForm();
        dialog.ShowDialog(this);
    }

    private void OnUpdatesChanged()
    {
        if (IsDisposed) return;

        if (InvokeRequired)
        {
            BeginInvoke(OnUpdatesChanged);
            return;
        }

        ShowUpdatesBadge();
    }

    private void ShowUpdatesBadge()
    {
        if (_updatesNav is null) return;

        _updatesNav.BadgeText = UpdatesState.Total > 0 ? UpdatesState.Total.ToString() : string.Empty;
        _updatesNav.BadgeTone = UpdatesState.Drivers > 0 ? Theme.Violet : Theme.Accent;
    }

    private void OpenHelp()
    {
        try
        {
            HelpForm.Open(this, _currentPageKey);
        }
        catch (Exception ex)
        {
            ActivityLog.Error("Help window failed: " + ex.Message);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F1)
        {
            OpenHelp();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ShowPage(string key)
    {
        _currentPageKey = key;

        if (!_pages.TryGetValue(key, out PageBase? page))
        {
            page = CreatePage(key);
            page.NavigateTo = ShowPage;
            _content.Controls.Add(page);
            _pages[key] = page;
        }

        foreach ((string pageKey, PageBase candidate) in _pages)
        {
            candidate.Visible = pageKey == key;
        }

        // The defrag page lives under Maintenance, so keep that nav item lit while it is open.
        string navKey = key == "defrag" ? "maintenance" : key;

        foreach (NavItem item in _nav)
        {
            item.Active = item.Key == navKey;
        }

        _pageTitle.Text = page.TitleText;
        _pageSub.Text = page.SubtitleText;
        _planBadge.SetText(PowerService.ActivePlanName.ToUpperInvariant());

        page.BringToFront();
        page.OnNavigated();
    }

    private static PageBase CreatePage(string key) => key switch
    {
        "startup" => new StartupPage(),
        "sleep" => new SleepPage(),
        "maintenance" => new MaintenancePage(),
        "backup" => new BackupPage(),
        "defrag" => new DefragPage(),
        "tweaks" => new TweaksPage(),
        "software" => new SoftwarePage(),
        "catalogue" => new CataloguePage(),
        "updates" => new UpdatesPage(),
        "audit" => new AuditPage(),
        "benchmarks" => new BenchPage(),
        "console" => new ConsolePage(),
        _ => new OverviewPage(),
    };

    /// <summary>A defrag that PC Medic started is stopped on exit, so ask first while it is working.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        if (e.Cancel) return;

        if (!_pages.TryGetValue("defrag", out PageBase? page) || page is not DefragPage { IsWorking: true }) return;

        DialogResult answer = MessageBox.Show(
            this,
            "Disk Defrag is still working." + Environment.NewLine + Environment.NewLine +
            "Closing PC MEDIC stops that operation." + Environment.NewLine + Environment.NewLine +
            "Close anyway?",
            "Defrag in progress",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer == DialogResult.Cancel) e.Cancel = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Busy.Indicator = null;
            _clockTimer.Stop();
            _clockTimer.Dispose();
            UpdatesState.Changed -= OnUpdatesChanged;

            _trayMonitor.Sampled -= OnTraySample;
            _trayMonitor.Dispose();

            _tray.Visible = false;
            _tray.ContextMenuStrip?.Dispose();
            _tray.Dispose();
        }

        base.Dispose(disposing);
    }
}
