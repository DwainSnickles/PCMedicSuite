namespace PC_Medic;

partial class SoftwarePage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private Card _updatesCard;
    private TableLayoutPanel _updatesInner;
    private Panel _updatesToolbar;
    private BufferedPanel _updatesList;
    private ScanOverlay _updatesOverlay;
    private Label _updateStatus;
    private NeonButton _securityOnly;
    private NeonButton _selectAll;
    private NeonButton _scan;
    private NeonButton _updateSelected;
    private NeonButton _cancelRun;
    private NeonButton _bulkExes;
    private Card _installedCard;
    private TableLayoutPanel _installedInner;
    private Panel _installedToolbar;
    private BufferedPanel _installedList;
    private ScanOverlay _installedOverlay;
    private Label _installedStatus;
    private Panel _searchHost;
    private Label _searchIcon;
    private TextBox _search;
    private NeonButton _findNext;
    private NeonButton _sort;
    private NeonButton _cleanDead;
    private NeonButton _exportCsv;
    private NeonButton _exportHtml;
    private NeonButton _rescan;
    private Card _settingsCard;
    private TableLayoutPanel _settingsInner;
    private Panel _settingsToolbar;
    private Label _settingsStatus;
    private NeonButton _exportDiag;
    private NeonButton _refreshCatalogue;
    private TableLayoutPanel _settingsColumns;
    private BufferedPanel _settingsList;
    private TableLayoutPanel _settingsRight;
    private Label _exclusionHeader;
    private BufferedPanel _exclusionList;
    private ContextMenuStrip _sortMenu;
    private System.Windows.Forms.Timer _sweep;
    private System.Windows.Forms.Timer _confirmTimer;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _updatesCard = new Card();
        _updatesInner = new TableLayoutPanel();
        _updatesToolbar = new Panel();
        _updatesList = new BufferedPanel();
        _updatesOverlay = new ScanOverlay();
        _updateStatus = new Label();
        _securityOnly = new NeonButton();
        _selectAll = new NeonButton();
        _scan = new NeonButton();
        _updateSelected = new NeonButton();
        _cancelRun = new NeonButton();
        _bulkExes = new NeonButton();
        _installedCard = new Card();
        _installedInner = new TableLayoutPanel();
        _installedToolbar = new Panel();
        _installedList = new BufferedPanel();
        _installedOverlay = new ScanOverlay();
        _installedStatus = new Label();
        _searchHost = new Panel();
        _searchIcon = new Label();
        _search = new TextBox();
        _findNext = new NeonButton();
        _sort = new NeonButton();
        _cleanDead = new NeonButton();
        _exportCsv = new NeonButton();
        _exportHtml = new NeonButton();
        _rescan = new NeonButton();
        _settingsCard = new Card();
        _settingsInner = new TableLayoutPanel();
        _settingsToolbar = new Panel();
        _settingsStatus = new Label();
        _exportDiag = new NeonButton();
        _refreshCatalogue = new NeonButton();
        _settingsColumns = new TableLayoutPanel();
        _settingsList = new BufferedPanel();
        _settingsRight = new TableLayoutPanel();
        _exclusionHeader = new Label();
        _exclusionList = new BufferedPanel();
        _sortMenu = new ContextMenuStrip(components);
        _sweep = new System.Windows.Forms.Timer(components);
        _confirmTimer = new System.Windows.Forms.Timer(components);
        _tips = new ToolTip(components);

        _root.SuspendLayout();
        _updatesCard.SuspendLayout();
        _updatesInner.SuspendLayout();
        _updatesToolbar.SuspendLayout();
        _installedCard.SuspendLayout();
        _installedInner.SuspendLayout();
        _installedToolbar.SuspendLayout();
        _searchHost.SuspendLayout();
        _settingsCard.SuspendLayout();
        _settingsInner.SuspendLayout();
        _settingsToolbar.SuspendLayout();
        _settingsColumns.SuspendLayout();
        _settingsRight.SuspendLayout();
        _sortMenu.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_updatesCard, 0, 0);
        _root.Controls.Add(_installedCard, 0, 1);
        _root.Controls.Add(_settingsCard, 0, 2);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 4;
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 22));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

        _updatesCard.Collapsible = true;
        _updatesCard.Controls.Add(_updatesInner);
        _updatesCard.Controls.Add(_updatesOverlay);
        _updatesCard.Dock = DockStyle.Fill;
        _updatesCard.Header = "UPDATES";
        _updatesCard.Kicker = "WINGET — SECURITY • PINS • IGNORE RULES";
        _updatesCard.Margin = new Padding(0, 0, 0, 8);

        _updatesInner.BackColor = Theme.Surface;
        _updatesInner.ColumnCount = 1;
        _updatesInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _updatesInner.Controls.Add(_updatesToolbar, 0, 0);
        _updatesInner.Controls.Add(_updatesList, 0, 1);
        _updatesInner.Dock = DockStyle.Fill;
        _updatesInner.Margin = new Padding(0);
        _updatesInner.RowCount = 2;
        _updatesInner.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _updatesInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _updatesToolbar.BackColor = Theme.Surface;
        _updatesToolbar.Controls.Add(_updateStatus);
        _updatesToolbar.Controls.Add(_securityOnly);
        _updatesToolbar.Controls.Add(_selectAll);
        _updatesToolbar.Controls.Add(_scan);
        _updatesToolbar.Controls.Add(_updateSelected);
        _updatesToolbar.Controls.Add(_cancelRun);
        _updatesToolbar.Controls.Add(_bulkExes);
        _updatesToolbar.Dock = DockStyle.Fill;
        _updatesToolbar.Margin = new Padding(0);

        _updateStatus.AutoEllipsis = true;
        _updateStatus.AutoSize = false;
        _updateStatus.BackColor = Color.Transparent;
        _updateStatus.Font = Theme.MonoSmall;
        _updateStatus.ForeColor = Theme.TextDim;
        _updateStatus.Location = new Point(0, 8);
        _updateStatus.Size = new Size(210, 18);
        _updateStatus.Text = "NOT SCANNED";
        _updateStatus.TextAlign = ContentAlignment.MiddleLeft;

        _securityOnly.Height = 28;
        _securityOnly.Location = new Point(600, 3);
        _securityOnly.Size = new Size(118, 28);
        _securityOnly.Text = "SECURITY ONLY";
        _securityOnly.Tone = Theme.TextDim;

        _selectAll.Height = 28;
        _selectAll.Location = new Point(494, 3);
        _selectAll.Size = new Size(100, 28);
        _selectAll.Text = "SELECT ALL";

        _scan.Height = 28;
        _scan.Location = new Point(412, 3);
        _scan.Size = new Size(76, 28);
        _scan.Text = "SCAN";

        _updateSelected.Height = 28;
        _updateSelected.Location = new Point(256, 3);
        _updateSelected.Primary = true;
        _updateSelected.Size = new Size(150, 28);
        _updateSelected.Text = "UPDATE SELECTED";

        _cancelRun.Height = 28;
        _cancelRun.Location = new Point(158, 3);
        _cancelRun.Size = new Size(92, 28);
        _cancelRun.Text = "CANCEL";
        _cancelRun.Tone = Theme.Bad;
        _cancelRun.Visible = false;

        _bulkExes.Height = 28;
        _bulkExes.Location = new Point(42, 3);
        _bulkExes.Size = new Size(110, 28);
        _bulkExes.Text = "BULK EXES";

        _updatesList.AutoScroll = true;
        _updatesList.BackColor = Theme.Surface;
        _updatesList.Dock = DockStyle.Fill;
        _updatesList.Margin = new Padding(0);

        _updatesOverlay.Visible = false;

        _installedCard.Collapsible = true;
        _installedCard.Controls.Add(_installedInner);
        _installedCard.Controls.Add(_installedOverlay);
        _installedCard.Dock = DockStyle.Fill;
        _installedCard.Header = "INSTALLED";
        _installedCard.Kicker = "REGISTRY + STORE — WINGET-TRACKED ROWS CAN BE REMOVED";
        _installedCard.Margin = new Padding(0, 8, 0, 8);

        _installedInner.BackColor = Theme.Surface;
        _installedInner.ColumnCount = 1;
        _installedInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _installedInner.Controls.Add(_installedToolbar, 0, 0);
        _installedInner.Controls.Add(_installedList, 0, 1);
        _installedInner.Dock = DockStyle.Fill;
        _installedInner.Margin = new Padding(0);
        _installedInner.RowCount = 2;
        _installedInner.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _installedInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _installedToolbar.BackColor = Theme.Surface;
        _installedToolbar.Controls.Add(_installedStatus);
        _installedToolbar.Controls.Add(_searchHost);
        _installedToolbar.Controls.Add(_findNext);
        _installedToolbar.Controls.Add(_sort);
        _installedToolbar.Controls.Add(_cleanDead);
        _installedToolbar.Controls.Add(_exportCsv);
        _installedToolbar.Controls.Add(_exportHtml);
        _installedToolbar.Controls.Add(_rescan);
        _installedToolbar.Dock = DockStyle.Fill;
        _installedToolbar.Margin = new Padding(0);

        _installedStatus.AutoEllipsis = true;
        _installedStatus.AutoSize = false;
        _installedStatus.BackColor = Color.Transparent;
        _installedStatus.Font = Theme.MonoSmall;
        _installedStatus.ForeColor = Theme.TextDim;
        _installedStatus.Location = new Point(0, 8);
        _installedStatus.Size = new Size(210, 18);
        _installedStatus.Text = "NOT SCANNED";
        _installedStatus.TextAlign = ContentAlignment.MiddleLeft;

        _searchHost.BackColor = Theme.Surface2;
        _searchHost.Controls.Add(_searchIcon);
        _searchHost.Controls.Add(_search);
        _searchHost.Height = 26;
        _searchHost.Location = new Point(300, 4);
        _searchHost.Size = new Size(250, 26);

        _searchIcon.BackColor = Color.Transparent;
        _searchIcon.Dock = DockStyle.Left;
        _searchIcon.Font = Theme.Icon;
        _searchIcon.ForeColor = Theme.Accent;
        _searchIcon.Text = "";
        _searchIcon.TextAlign = ContentAlignment.MiddleCenter;
        _searchIcon.Width = 26;

        _search.BackColor = Theme.Surface2;
        _search.BorderStyle = BorderStyle.None;
        _search.Font = Theme.Ui;
        _search.Location = new Point(26, 0);
        _search.Size = new Size(224, 26);
        _search.Text = "SEARCH INSTALLED…";

        _findNext.Height = 28;
        _findNext.Location = new Point(220, 3);
        _findNext.Size = new Size(62, 28);
        _findNext.Text = "NEXT";
        _findNext.Tone = Theme.TextDim;

        _sort.Height = 28;
        _sort.Location = new Point(102, 3);
        _sort.Primary = true;
        _sort.Size = new Size(112, 28);
        _sort.Text = "SORT: NAME";

        _cleanDead.Height = 28;
        _cleanDead.Location = new Point(0, 3);
        _cleanDead.Size = new Size(104, 28);
        _cleanDead.Text = "CLEAN DEAD";
        _cleanDead.Tone = Theme.Bad;

        _exportCsv.Height = 28;
        _exportCsv.Location = new Point(-100, 3);
        _exportCsv.Size = new Size(96, 28);
        _exportCsv.Text = "EXPORT CSV";

        _exportHtml.Height = 28;
        _exportHtml.Location = new Point(-204, 3);
        _exportHtml.Size = new Size(100, 28);
        _exportHtml.Text = "EXPORT HTML";

        _rescan.Height = 28;
        _rescan.Location = new Point(-286, 3);
        _rescan.Size = new Size(76, 28);
        _rescan.Text = "RESCAN";

        _installedList.AutoScroll = true;
        _installedList.BackColor = Theme.Surface;
        _installedList.Dock = DockStyle.Fill;
        _installedList.Margin = new Padding(0);

        _installedOverlay.Visible = false;

        _settingsCard.Collapsible = true;
        _settingsCard.Controls.Add(_settingsInner);
        _settingsCard.Dock = DockStyle.Fill;
        _settingsCard.Header = "SETTINGS";
        _settingsCard.Kicker = "SOURCES • SCANNING • INSTALLS • EXCLUSIONS";
        _settingsCard.Margin = new Padding(0, 8, 0, 0);

        _settingsInner.BackColor = Theme.Surface;
        _settingsInner.ColumnCount = 1;
        _settingsInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _settingsInner.Controls.Add(_settingsToolbar, 0, 0);
        _settingsInner.Controls.Add(_settingsColumns, 0, 1);
        _settingsInner.Dock = DockStyle.Fill;
        _settingsInner.Margin = new Padding(0);
        _settingsInner.RowCount = 2;
        _settingsInner.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _settingsInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _settingsToolbar.BackColor = Theme.Surface;
        _settingsToolbar.Controls.Add(_settingsStatus);
        _settingsToolbar.Controls.Add(_exportDiag);
        _settingsToolbar.Controls.Add(_refreshCatalogue);
        _settingsToolbar.Dock = DockStyle.Fill;
        _settingsToolbar.Margin = new Padding(0);

        _settingsStatus.AutoEllipsis = true;
        _settingsStatus.AutoSize = false;
        _settingsStatus.BackColor = Color.Transparent;
        _settingsStatus.Font = Theme.MonoSmall;
        _settingsStatus.ForeColor = Theme.TextDim;
        _settingsStatus.Location = new Point(0, 8);
        _settingsStatus.Size = new Size(210, 18);
        _settingsStatus.Text = "PREFERENCES • EXCLUSIONS";
        _settingsStatus.TextAlign = ContentAlignment.MiddleLeft;

        _exportDiag.Height = 28;
        _exportDiag.Location = new Point(140, 3);
        _exportDiag.Size = new Size(160, 28);
        _exportDiag.Text = "EXPORT DIAGNOSTICS…";

        _refreshCatalogue.Height = 28;
        _refreshCatalogue.Location = new Point(0, 3);
        _refreshCatalogue.Size = new Size(140, 28);
        _refreshCatalogue.Text = "REFRESH CATALOGUE";

        _settingsColumns.BackColor = Theme.Surface;
        _settingsColumns.ColumnCount = 2;
        _settingsColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        _settingsColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        _settingsColumns.Controls.Add(_settingsList, 0, 0);
        _settingsColumns.Controls.Add(_settingsRight, 1, 0);
        _settingsColumns.Dock = DockStyle.Fill;
        _settingsColumns.Margin = new Padding(0);
        _settingsColumns.RowCount = 1;
        _settingsColumns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _settingsList.AutoScroll = true;
        _settingsList.BackColor = Theme.Surface;
        _settingsList.Dock = DockStyle.Fill;
        _settingsList.Margin = new Padding(0, 0, 10, 0);

        _settingsRight.BackColor = Theme.Surface;
        _settingsRight.ColumnCount = 1;
        _settingsRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _settingsRight.Controls.Add(_exclusionHeader, 0, 0);
        _settingsRight.Controls.Add(_exclusionList, 0, 1);
        _settingsRight.Dock = DockStyle.Fill;
        _settingsRight.Margin = new Padding(0);
        _settingsRight.RowCount = 2;
        _settingsRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        _settingsRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _exclusionHeader.AutoSize = false;
        _exclusionHeader.BackColor = Color.Transparent;
        _exclusionHeader.Dock = DockStyle.Fill;
        _exclusionHeader.Font = Theme.Kicker;
        _exclusionHeader.ForeColor = Theme.TextDim;
        _exclusionHeader.Text = "EXCLUSION RULES";
        _exclusionHeader.TextAlign = ContentAlignment.MiddleLeft;

        _exclusionList.AutoScroll = true;
        _exclusionList.BackColor = Theme.Surface;
        _exclusionList.Dock = DockStyle.Fill;
        _exclusionList.Margin = new Padding(0);

        _sortMenu.Font = Theme.MonoSmall;
        _sortMenu.ShowImageMargin = false;
        _sortMenu.Size = new Size(61, 4);

        _sweep.Interval = 40;

        _confirmTimer.Interval = 4000;

        Controls.Add(_root);

        _updatesToolbar.ResumeLayout(false);
        _updatesToolbar.PerformLayout();
        _updatesInner.ResumeLayout(false);
        _updatesCard.ResumeLayout(false);
        _installedToolbar.ResumeLayout(false);
        _installedToolbar.PerformLayout();
        _installedInner.ResumeLayout(false);
        _installedCard.ResumeLayout(false);
        _searchHost.ResumeLayout(false);
        _searchHost.PerformLayout();
        _settingsToolbar.ResumeLayout(false);
        _settingsToolbar.PerformLayout();
        _settingsInner.ResumeLayout(false);
        _settingsColumns.ResumeLayout(false);
        _settingsRight.ResumeLayout(false);
        _settingsRight.PerformLayout();
        _settingsCard.ResumeLayout(false);
        _sortMenu.ResumeLayout(false);
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
