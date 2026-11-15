namespace PC_Medic;

partial class UpdatesPage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private Card _appsCard;
    private TableLayoutPanel _appsInner;
    private Panel _appsToolbar;
    private BufferedPanel _apps;
    private ScanOverlay _appsLoading;
    private Label _appStatus;
    private NeonButton _scanApps;
    private NeonButton _downloadApps;
    private Card _driversCard;
    private TableLayoutPanel _driversInner;
    private Panel _driversToolbar;
    private BufferedPanel _drivers;
    private ScanOverlay _driversLoading;
    private Label _driverStatus;
    private NeonButton _scanDrivers;
    private NeonButton _downloadDrivers;
    private NeonButton _offlineDrivers;
    private Label _note;
    private ContextMenuStrip _offlineMenu;
    private System.Windows.Forms.Timer _confirmTimer;
    private System.Windows.Forms.Timer _rowSweep;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _appsCard = new Card();
        _appsInner = new TableLayoutPanel();
        _appsToolbar = new Panel();
        _apps = new BufferedPanel();
        _appsLoading = new ScanOverlay();
        _appStatus = new Label();
        _scanApps = new NeonButton();
        _downloadApps = new NeonButton();
        _driversCard = new Card();
        _driversInner = new TableLayoutPanel();
        _driversToolbar = new Panel();
        _drivers = new BufferedPanel();
        _driversLoading = new ScanOverlay();
        _driverStatus = new Label();
        _scanDrivers = new NeonButton();
        _downloadDrivers = new NeonButton();
        _offlineDrivers = new NeonButton();
        _note = new Label();
        _offlineMenu = new ContextMenuStrip(components);
        _confirmTimer = new System.Windows.Forms.Timer(components);
        _rowSweep = new System.Windows.Forms.Timer(components);
        _tips = new ToolTip(components);

        _root.SuspendLayout();
        _appsCard.SuspendLayout();
        _appsInner.SuspendLayout();
        _appsToolbar.SuspendLayout();
        _driversCard.SuspendLayout();
        _driversInner.SuspendLayout();
        _driversToolbar.SuspendLayout();
        _offlineMenu.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 3;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
        _root.Controls.Add(_appsCard, 0, 0);
        _root.Controls.Add(_driversCard, 1, 0);
        _root.Controls.Add(_note, 0, 1);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 2;
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        _appsCard.Collapsible = true;
        _appsCard.Controls.Add(_appsInner);
        _appsCard.Controls.Add(_appsLoading);
        _appsCard.Dock = DockStyle.Fill;
        _appsCard.Header = "APPLICATIONS";
        _appsCard.Kicker = "WINGET";
        _appsCard.Margin = new Padding(0, 0, 6, 8);

        _appsInner.BackColor = Theme.Surface;
        _appsInner.ColumnCount = 1;
        _appsInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _appsInner.Controls.Add(_appsToolbar, 0, 0);
        _appsInner.Controls.Add(_apps, 0, 1);
        _appsInner.Dock = DockStyle.Fill;
        _appsInner.Margin = new Padding(0);
        _appsInner.RowCount = 2;
        _appsInner.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _appsInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _appsToolbar.BackColor = Theme.Surface;
        _appsToolbar.Controls.Add(_appStatus);
        _appsToolbar.Controls.Add(_downloadApps);
        _appsToolbar.Controls.Add(_scanApps);
        _appsToolbar.Dock = DockStyle.Fill;
        _appsToolbar.Margin = new Padding(0);

        _appStatus.AutoEllipsis = true;
        _appStatus.AutoSize = false;
        _appStatus.BackColor = Color.Transparent;
        _appStatus.Font = Theme.MonoSmall;
        _appStatus.ForeColor = Theme.TextDim;
        _appStatus.Location = new Point(0, 8);
        _appStatus.Size = new Size(210, 18);
        _appStatus.TextAlign = ContentAlignment.MiddleLeft;

        _scanApps.Height = 28;
        _scanApps.Location = new Point(300, 3);
        _scanApps.Size = new Size(76, 28);
        _scanApps.Text = "SCAN";

        _downloadApps.Glyph = "";
        _downloadApps.Height = 28;
        _downloadApps.Location = new Point(140, 3);
        _downloadApps.Primary = true;
        _downloadApps.Size = new Size(158, 28);
        _downloadApps.Text = "DOWNLOAD ALL";

        _apps.AutoScroll = true;
        _apps.BackColor = Theme.Surface;
        _apps.Dock = DockStyle.Fill;
        _apps.Margin = new Padding(0);

        _appsLoading.Visible = false;

        _driversCard.Collapsible = true;
        _driversCard.CollapsedText = "OPEN TO CHECK DRIVERS";
        _driversCard.Controls.Add(_driversInner);
        _driversCard.Controls.Add(_driversLoading);
        _driversCard.Dock = DockStyle.Fill;
        _driversCard.Header = "DRIVERS";
        _driversCard.Kicker = "WINDOWS UPDATE";
        _driversCard.Margin = new Padding(6, 0, 0, 8);

        _driversInner.BackColor = Theme.Surface;
        _driversInner.ColumnCount = 1;
        _driversInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _driversInner.Controls.Add(_driversToolbar, 0, 0);
        _driversInner.Controls.Add(_drivers, 0, 1);
        _driversInner.Dock = DockStyle.Fill;
        _driversInner.Margin = new Padding(0);
        _driversInner.RowCount = 2;
        _driversInner.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _driversInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _driversToolbar.BackColor = Theme.Surface;
        _driversToolbar.Controls.Add(_driverStatus);
        _driversToolbar.Controls.Add(_downloadDrivers);
        _driversToolbar.Controls.Add(_offlineDrivers);
        _driversToolbar.Controls.Add(_scanDrivers);
        _driversToolbar.Dock = DockStyle.Fill;
        _driversToolbar.Margin = new Padding(0);

        _driverStatus.AutoEllipsis = true;
        _driverStatus.AutoSize = false;
        _driverStatus.BackColor = Color.Transparent;
        _driverStatus.Font = Theme.MonoSmall;
        _driverStatus.ForeColor = Theme.TextDim;
        _driverStatus.Location = new Point(0, 8);
        _driverStatus.Size = new Size(210, 18);
        _driverStatus.TextAlign = ContentAlignment.MiddleLeft;

        _scanDrivers.Height = 28;
        _scanDrivers.Location = new Point(430, 3);
        _scanDrivers.Size = new Size(76, 28);
        _scanDrivers.Text = "SCAN";

        _downloadDrivers.Glyph = "";
        _downloadDrivers.Height = 28;
        _downloadDrivers.Location = new Point(270, 3);
        _downloadDrivers.Primary = true;
        _downloadDrivers.Size = new Size(158, 28);
        _downloadDrivers.Text = "DOWNLOAD ALL";

        _offlineDrivers.Height = 28;
        _offlineDrivers.Location = new Point(172, 3);
        _offlineDrivers.Size = new Size(92, 28);
        _offlineDrivers.Text = "OFFLINE";

        _drivers.AutoScroll = true;
        _drivers.BackColor = Theme.Surface;
        _drivers.Dock = DockStyle.Fill;
        _drivers.Margin = new Padding(0);

        _driversLoading.Visible = false;

        _note.AutoEllipsis = true;
        _note.AutoSize = false;
        _note.BackColor = Color.Transparent;
        _note.Dock = DockStyle.Fill;
        _note.Font = Theme.MonoSmall;
        _note.ForeColor = Theme.TextDim;
        _note.Location = new Point(2, 0);
        _note.Margin = new Padding(2, 0, 0, 0);
        _note.Size = new Size(800, 22);
        _note.TextAlign = ContentAlignment.MiddleLeft;

        _offlineMenu.Font = Theme.MonoSmall;
        _offlineMenu.ShowImageMargin = false;
        _offlineMenu.Size = new Size(61, 4);

        _confirmTimer.Interval = 4000;

        _rowSweep.Interval = 40;

        Controls.Add(_root);

        _appsToolbar.ResumeLayout(false);
        _appsToolbar.PerformLayout();
        _appsInner.ResumeLayout(false);
        _appsCard.ResumeLayout(false);
        _driversToolbar.ResumeLayout(false);
        _driversToolbar.PerformLayout();
        _driversInner.ResumeLayout(false);
        _driversCard.ResumeLayout(false);
        _offlineMenu.ResumeLayout(false);
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
