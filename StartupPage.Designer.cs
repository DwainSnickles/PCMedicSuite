namespace PC_Medic;

partial class StartupPage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private TableLayoutPanel _strip;
    private Card _engineCard;
    private InfoList _engine;
    private Card _fastCard;
    private ToggleRow _fast;
    private Label _fastNote;
    private Card _bootCard;
    private NeonButton _safeMode;
    private NeonButton _repairMode;
    private Label _bootState;
    private Label _bootNote;
    private Panel _toolbar;
    private Label _status;
    private NeonButton _all;
    private NeonButton _apps;
    private NeonButton _servicesOnly;
    private NeonButton _disabled;
    private NeonButton _delayThirdParty;
    private NeonButton _rescan;
    private Card _listCard;
    private BufferedPanel _list;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _strip = new TableLayoutPanel();
        _engineCard = new Card();
        _engine = new InfoList();
        _fastCard = new Card();
        _fast = new ToggleRow();
        _fastNote = new Label();
        _bootCard = new Card();
        _safeMode = new NeonButton();
        _repairMode = new NeonButton();
        _bootState = new Label();
        _bootNote = new Label();
        _toolbar = new Panel();
        _status = new Label();
        _all = new NeonButton();
        _apps = new NeonButton();
        _servicesOnly = new NeonButton();
        _disabled = new NeonButton();
        _delayThirdParty = new NeonButton();
        _rescan = new NeonButton();
        _listCard = new Card();
        _list = new BufferedPanel();
        _tips = new ToolTip(components);

        _root.SuspendLayout();
        _strip.SuspendLayout();
        _engineCard.SuspendLayout();
        _fastCard.SuspendLayout();
        _bootCard.SuspendLayout();
        _toolbar.SuspendLayout();
        _listCard.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_strip, 0, 0);
        _root.Controls.Add(_toolbar, 0, 1);
        _root.Controls.Add(_listCard, 0, 2);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 3;
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _strip.BackColor = Theme.Bg;
        _strip.ColumnCount = 3;
        _strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        _strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        _strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        _strip.Controls.Add(_engineCard, 0, 0);
        _strip.Controls.Add(_fastCard, 1, 0);
        _strip.Controls.Add(_bootCard, 2, 0);
        _strip.Dock = DockStyle.Fill;
        _strip.Margin = new Padding(0);
        _strip.RowCount = 1;
        _strip.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _engineCard.Controls.Add(_engine);
        _engineCard.Dock = DockStyle.Fill;
        _engineCard.Header = "BOOT ENGINE";
        _engineCard.Kicker = "STARTUP PATH";
        _engineCard.Margin = new Padding(0, 0, 12, 12);

        _engine.RowHeight = 28;

        _fastCard.Controls.Add(_fast);
        _fastCard.Controls.Add(_fastNote);
        _fastCard.Dock = DockStyle.Fill;
        _fastCard.Header = "FAST STARTUP";
        _fastCard.Kicker = "HIBERNBOOT";
        _fastCard.Margin = new Padding(0, 0, 0, 12);

        _fast.BadgeText = "BOOT";
        _fast.BadgeTone = Theme.Accent;
        _fast.Subtitle = "HiberbootEnabled — resumes the kernel session from disk";
        _fast.Title = "FAST STARTUP";

        _fastNote.AutoEllipsis = true;
        _fastNote.AutoSize = false;
        _fastNote.BackColor = Color.Transparent;
        _fastNote.Font = Theme.UiSmall;
        _fastNote.ForeColor = Theme.TextDim;
        _fastNote.Location = new Point(18, 126);
        _fastNote.Size = new Size(400, 32);
        _fastNote.Text = "Skips the cold boot path on shutdown. Needs hibernation enabled; a change takes effect on the next shutdown.";

        _bootCard.Controls.Add(_safeMode);
        _bootCard.Controls.Add(_repairMode);
        _bootCard.Controls.Add(_bootState);
        _bootCard.Controls.Add(_bootNote);
        _bootCard.Dock = DockStyle.Fill;
        _bootCard.Header = "RESTART OPTIONS";
        _bootCard.Kicker = "SAFE MODE + REPAIR";
        _bootCard.Margin = new Padding(0, 0, 0, 12);

        _safeMode.Height = 32;
        _safeMode.Location = new Point(18, 58);
        _safeMode.Size = new Size(118, 32);
        _safeMode.Text = "SAFE MODE";

        _repairMode.Height = 32;
        _repairMode.Location = new Point(144, 58);
        _repairMode.Size = new Size(118, 32);
        _repairMode.Text = "REPAIR MODE";

        _bootState.AutoEllipsis = true;
        _bootState.AutoSize = false;
        _bootState.BackColor = Color.Transparent;
        _bootState.Font = Theme.MonoSmall;
        _bootState.ForeColor = Theme.TextDim;
        _bootState.Location = new Point(18, 96);
        _bootState.Size = new Size(400, 18);

        _bootNote.AutoEllipsis = true;
        _bootNote.AutoSize = false;
        _bootNote.BackColor = Color.Transparent;
        _bootNote.Font = Theme.UiSmall;
        _bootNote.ForeColor = Theme.TextDim;
        _bootNote.Location = new Point(18, 116);
        _bootNote.Size = new Size(400, 40);
        _bootNote.Text = "Safe mode stays set until cleared here. Repair mode restarts into Windows Recovery.";

        _toolbar.BackColor = Theme.Bg;
        _toolbar.Controls.Add(_status);
        _toolbar.Controls.Add(_all);
        _toolbar.Controls.Add(_apps);
        _toolbar.Controls.Add(_servicesOnly);
        _toolbar.Controls.Add(_disabled);
        _toolbar.Controls.Add(_delayThirdParty);
        _toolbar.Controls.Add(_rescan);
        _toolbar.Dock = DockStyle.Fill;
        _toolbar.Margin = new Padding(0);

        _status.AutoEllipsis = true;
        _status.AutoSize = false;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(2, 13);
        _status.Size = new Size(400, 18);
        _status.TextAlign = ContentAlignment.MiddleLeft;

        _all.Height = 30;
        _all.Location = new Point(700, 4);
        _all.Size = new Size(62, 30);
        _all.Text = "ALL";

        _apps.Height = 30;
        _apps.Location = new Point(640, 4);
        _apps.Size = new Size(70, 30);
        _apps.Text = "APPS";

        _servicesOnly.Height = 30;
        _servicesOnly.Location = new Point(536, 4);
        _servicesOnly.Size = new Size(96, 30);
        _servicesOnly.Text = "SERVICES";

        _disabled.Height = 30;
        _disabled.Location = new Point(432, 4);
        _disabled.Size = new Size(96, 30);
        _disabled.Text = "DISABLED";

        _delayThirdParty.Height = 30;
        _delayThirdParty.Location = new Point(248, 4);
        _delayThirdParty.Primary = true;
        _delayThirdParty.Size = new Size(176, 30);
        _delayThirdParty.Text = "DELAY THIRD-PARTY";

        _rescan.Height = 30;
        _rescan.Location = new Point(150, 4);
        _rescan.Size = new Size(90, 30);
        _rescan.Text = "RESCAN";

        _listCard.Controls.Add(_list);
        _listCard.Dock = DockStyle.Fill;
        _listCard.Header = "WHAT RUNS AT SIGN-IN";
        _listCard.Kicker = "AUTORUN + SERVICES";

        _list.AutoScroll = true;
        _list.BackColor = Theme.Surface;
        _list.Dock = DockStyle.Fill;

        Controls.Add(_root);

        _engineCard.ResumeLayout(false);
        _fastCard.ResumeLayout(false);
        _fastCard.PerformLayout();
        _bootCard.ResumeLayout(false);
        _bootCard.PerformLayout();
        _toolbar.ResumeLayout(false);
        _toolbar.PerformLayout();
        _listCard.ResumeLayout(false);
        _strip.ResumeLayout(false);
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
