namespace PC_Medic;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel _sidebar;
    private Panel _header;
    private Panel _content;
    private BusyBar _busyBar;
    private Label _pageTitle;
    private Label _pageSub;
    private Label _clock;
    private Badge _adminBadge;
    private Badge _planBadge;
    private ChromeButton _minimize;
    private NeonButton _help;
    private NeonButton _elevate;
    private ToolTip _tips;
    private System.Windows.Forms.Timer _clockTimer;
    private NotifyIcon _tray;
    private BrandMark _mark;
    private Label _brand;
    private Label _tagline;
    private Label _build;
    private Panel _sidebarDivider;
    private Panel _sidebarBottom;
    private Label _elevationLabel;
    private Label _elevationState;
    private NeonButton _sidebarElevate;
    private NeonButton _checkUpdatesButton;
    private Label _sidebarNote;
    private NavItem _navOverview;
    private NavItem _navStartup;
    private NavItem _navSleep;
    private NavItem _navMaintenance;
    private NavItem _navBackup;
    private NavItem _navTweaks;
    private NavItem _navSoftware;
    private NavItem _navCatalogue;
    private NavItem _navUpdates;
    private NavItem _navAudit;
    private NavItem _navBenchmarks;
    private NavItem _navConsole;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _sidebar = new Panel();
        _header = new Panel();
        _content = new Panel();
        _busyBar = new BusyBar();
        _pageTitle = new Label();
        _pageSub = new Label();
        _clock = new Label();
        _adminBadge = new Badge();
        _planBadge = new Badge();
        _minimize = new ChromeButton();
        _help = new NeonButton();
        _elevate = new NeonButton();
        _tips = new ToolTip(components);
        _clockTimer = new System.Windows.Forms.Timer(components);
        _tray = new NotifyIcon(components);
        _mark = new BrandMark();
        _brand = new Label();
        _tagline = new Label();
        _build = new Label();
        _sidebarDivider = new Panel();
        _sidebarBottom = new Panel();
        _elevationLabel = new Label();
        _elevationState = new Label();
        _sidebarElevate = new NeonButton();
        _checkUpdatesButton = new NeonButton();
        _sidebarNote = new Label();
        _navOverview = new NavItem();
        _navStartup = new NavItem();
        _navSleep = new NavItem();
        _navMaintenance = new NavItem();
        _navBackup = new NavItem();
        _navTweaks = new NavItem();
        _navSoftware = new NavItem();
        _navCatalogue = new NavItem();
        _navUpdates = new NavItem();
        _navAudit = new NavItem();
        _navBenchmarks = new NavItem();
        _navConsole = new NavItem();

        _sidebar.SuspendLayout();
        _header.SuspendLayout();
        _content.SuspendLayout();
        _sidebarBottom.SuspendLayout();
        SuspendLayout();

        Text = "PC MEDIC — System Optimizer";
        ClientSize = new Size(1250, 815);
        MinimumSize = new Size(1120, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        DoubleBuffered = true;

        _sidebar.BackColor = Theme.Surface;
        _sidebar.Controls.Add(_mark);
        _sidebar.Controls.Add(_brand);
        _sidebar.Controls.Add(_tagline);
        _sidebar.Controls.Add(_build);
        _sidebar.Controls.Add(_sidebarDivider);
        _sidebar.Controls.Add(_sidebarBottom);
        _sidebar.Controls.Add(_navOverview);
        _sidebar.Controls.Add(_navStartup);
        _sidebar.Controls.Add(_navSleep);
        _sidebar.Controls.Add(_navMaintenance);
        _sidebar.Controls.Add(_navBackup);
        _sidebar.Controls.Add(_navTweaks);
        _sidebar.Controls.Add(_navSoftware);
        _sidebar.Controls.Add(_navCatalogue);
        _sidebar.Controls.Add(_navUpdates);
        _sidebar.Controls.Add(_navAudit);
        _sidebar.Controls.Add(_navBenchmarks);
        _sidebar.Controls.Add(_navConsole);
        _sidebar.Dock = DockStyle.Left;
        _sidebar.Width = 246;

        _header.BackColor = Theme.Bg;
        _header.Controls.Add(_pageTitle);
        _header.Controls.Add(_pageSub);
        _header.Controls.Add(_clock);
        _header.Controls.Add(_adminBadge);
        _header.Controls.Add(_planBadge);
        _header.Controls.Add(_elevate);
        _header.Controls.Add(_minimize);
        _header.Controls.Add(_help);
        _header.Dock = DockStyle.Top;
        _header.Height = 78;

        _content.BackColor = Theme.Bg;
        _content.Controls.Add(_busyBar);
        _content.Dock = DockStyle.Fill;
        _content.Padding = new Padding(28, 8, 26, 22);

        _busyBar.Location = new Point(28, 771);
        _busyBar.Size = new Size(160, 28);

        _pageTitle.AutoSize = true;
        _pageTitle.BackColor = Color.Transparent;
        _pageTitle.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _pageTitle.ForeColor = Theme.Text;
        _pageTitle.Location = new Point(28, 14);

        _pageSub.AutoSize = true;
        _pageSub.BackColor = Color.Transparent;
        _pageSub.Font = Theme.UiSmall;
        _pageSub.ForeColor = Theme.TextDim;
        _pageSub.Location = new Point(30, 45);

        _clock.AutoSize = true;
        _clock.BackColor = Color.Transparent;
        _clock.Font = Theme.Mono;
        _clock.ForeColor = Theme.TextDim;

        _adminBadge.AutoSize = true;
        _planBadge.AutoSize = true;

        _minimize.Glyph = "";
        _minimize.Size = new Size(32, 30);

        _help.Size = new Size(32, 28);
        _help.Text = "?";

        _elevate.Size = new Size(96, 28);
        _elevate.Text = "ELEVATE";
        _elevate.Tone = Theme.Warn;

        _clockTimer.Interval = 1000;

        _tray.Text = "PC MEDIC — System Optimizer";

        _mark.Location = new Point(20, 16);
        _mark.Size = new Size(42, 42);

        _brand.AutoSize = true;
        _brand.BackColor = Color.Transparent;
        _brand.Font = Theme.H1;
        _brand.ForeColor = Theme.Text;
        _brand.Location = new Point(70, 18);
        _brand.Text = "PC MEDIC";

        _tagline.AutoSize = true;
        _tagline.BackColor = Color.Transparent;
        _tagline.Font = Theme.Kicker;
        _tagline.ForeColor = Theme.Alpha(Theme.Accent, 215);
        _tagline.Location = new Point(72, 48);
        _tagline.Text = "SYSTEM OPTIMIZER";

        // Big enough to read from across a desk, and the accent rather than the dim grey the rest of
        // the fine print uses: this is the line people quote when they report a problem.
        _build.AutoSize = true;
        _build.BackColor = Color.Transparent;
        _build.Font = new Font(Theme.Mono.FontFamily, 10f, FontStyle.Bold);
        _build.ForeColor = Theme.Accent;
        _build.Location = new Point(22, 68);
        _build.Text = "PORTABLE " + AppInfo.CurrentVersionText;

        _sidebarDivider.BackColor = Theme.Border;
        _sidebarDivider.Location = new Point(22, 90);
        _sidebarDivider.Size = new Size(202, 1);

        _sidebarBottom.BackColor = Theme.Surface;
        _sidebarBottom.Controls.Add(_elevationLabel);
        _sidebarBottom.Controls.Add(_elevationState);
        _sidebarBottom.Controls.Add(_sidebarElevate);
        _sidebarBottom.Controls.Add(_checkUpdatesButton);
        _sidebarBottom.Controls.Add(_sidebarNote);
        _sidebarBottom.Dock = DockStyle.Bottom;
        _sidebarBottom.Height = 168;

        _elevationLabel.AutoSize = true;
        _elevationLabel.BackColor = Color.Transparent;
        _elevationLabel.Font = Theme.Kicker;
        _elevationLabel.ForeColor = Theme.TextDim;
        _elevationLabel.Location = new Point(24, 16);
        _elevationLabel.Text = "ELEVATION";

        _elevationState.AutoSize = true;
        _elevationState.BackColor = Color.Transparent;
        _elevationState.Font = Theme.MonoSmall;
        _elevationState.Location = new Point(24, 32);

        _sidebarElevate.Location = new Point(22, 58);
        _sidebarElevate.Size = new Size(198, 32);

        _checkUpdatesButton.Location = new Point(22, 96);
        _checkUpdatesButton.Size = new Size(198, 32);
        _checkUpdatesButton.Text = "CHECK UPDATES";

        _sidebarNote.AutoSize = true;
        _sidebarNote.BackColor = Color.Transparent;
        _sidebarNote.Font = Theme.Kicker;
        _sidebarNote.ForeColor = Theme.TextDim;
        _sidebarNote.Location = new Point(24, 138);
        _sidebarNote.Text = "USB-friendly • nothing installed";

        _navOverview.Location = new Point(14, 104);
        _navOverview.Size = new Size(218, 44);
        _navOverview.Key = "overview";
        _navOverview.Caption = "Overview";
        _navOverview.Glyph = "\uE80F";

        _navStartup.Location = new Point(14, 152);
        _navStartup.Size = new Size(218, 44);
        _navStartup.Key = "startup";
        _navStartup.Caption = "Startup & Boot";
        _navStartup.Glyph = "\uE71D";

        _navSleep.Location = new Point(14, 200);
        _navSleep.Size = new Size(218, 44);
        _navSleep.Key = "sleep";
        _navSleep.Caption = "Optimize Sleep";
        _navSleep.Glyph = "\uE708";

        _navMaintenance.Location = new Point(14, 248);
        _navMaintenance.Size = new Size(218, 44);
        _navMaintenance.Key = "maintenance";
        _navMaintenance.Caption = "Maintenance";
        _navMaintenance.Glyph = "\uE90F";

        _navBackup.Location = new Point(14, 296);
        _navBackup.Size = new Size(218, 44);
        _navBackup.Key = "backup";
        _navBackup.Caption = "Backup & Restore";
        _navBackup.Glyph = "\uE8B7";

        _navTweaks.Location = new Point(14, 344);
        _navTweaks.Size = new Size(218, 44);
        _navTweaks.Key = "tweaks";
        _navTweaks.Caption = "Tweaks";
        _navTweaks.Glyph = "\uE713";

        _navSoftware.Location = new Point(14, 392);
        _navSoftware.Size = new Size(218, 44);
        _navSoftware.Key = "software";
        _navSoftware.Caption = "Software";
        _navSoftware.Glyph = "\uE8FD";

        _navCatalogue.Location = new Point(14, 440);
        _navCatalogue.Size = new Size(218, 44);
        _navCatalogue.Key = "catalogue";
        _navCatalogue.Caption = "Catalogue";
        _navCatalogue.Glyph = "\uE7B8";

        _navUpdates.Location = new Point(14, 488);
        _navUpdates.Size = new Size(218, 44);
        _navUpdates.Key = "updates";
        _navUpdates.Caption = "Updates";
        _navUpdates.Glyph = "\uE896";

        _navAudit.Location = new Point(14, 536);
        _navAudit.Size = new Size(218, 44);
        _navAudit.Key = "audit";
        _navAudit.Caption = "Registry Audit";
        _navAudit.Glyph = "\uE721";

        _navBenchmarks.Location = new Point(14, 584);
        _navBenchmarks.Size = new Size(218, 44);
        _navBenchmarks.Key = "benchmarks";
        _navBenchmarks.Caption = "Benchmarks";
        _navBenchmarks.Glyph = "\uE9D9";

        _navConsole.Location = new Point(14, 632);
        _navConsole.Size = new Size(218, 44);
        _navConsole.Key = "console";
        _navConsole.Caption = "Command Shell";
        _navConsole.Glyph = "\uE756";

        Controls.Add(_content);
        Controls.Add(_header);
        Controls.Add(_sidebar);

        _sidebar.ResumeLayout(false);
        _sidebar.PerformLayout();
        _header.ResumeLayout(false);
        _header.PerformLayout();
        _content.ResumeLayout(false);
        _sidebarBottom.ResumeLayout(false);
        _sidebarBottom.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}
