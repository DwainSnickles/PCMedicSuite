namespace PC_Medic;

partial class SleepPage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private TableLayoutPanel _split;
    private Card _hero;
    private Label _heroBody;
    private Label _heroStatus;
    private NeonButton _apply;
    private NeonButton _restore;
    private Card _ladder;
    private StepperRow _screen;
    private StepperRow _sleep;
    private StepperRow _hibernate;
    private StepperRow _disk;
    private Card _behavior;
    private ToggleRow _wake;
    private ToggleRow _usb;
    private ToggleRow _hybrid;
    private ToggleRow _keepAwake;
    private Label _settingsNote;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _hero = new Card();
        _heroBody = new Label();
        _heroStatus = new Label();
        _apply = new NeonButton();
        _restore = new NeonButton();
        _split = new TableLayoutPanel();
        _ladder = new Card();
        _screen = new StepperRow();
        _sleep = new StepperRow();
        _hibernate = new StepperRow();
        _disk = new StepperRow();
        _behavior = new Card();
        _wake = new ToggleRow();
        _usb = new ToggleRow();
        _hybrid = new ToggleRow();
        _keepAwake = new ToggleRow();
        _settingsNote = new Label();

        _root.SuspendLayout();
        _hero.SuspendLayout();
        _split.SuspendLayout();
        _ladder.SuspendLayout();
        _behavior.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_hero, 0, 0);
        _root.Controls.Add(_split, 0, 1);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 2;
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _hero.Controls.Add(_heroBody);
        _hero.Controls.Add(_heroStatus);
        _hero.Controls.Add(_apply);
        _hero.Controls.Add(_restore);
        _hero.Dock = DockStyle.Fill;
        _hero.Header = "OPTIMIZE SLEEP";
        _hero.Kicker = "POWER LADDER";
        _hero.Margin = new Padding(0, 0, 0, 12);

        _heroBody.AutoSize = false;
        _heroBody.BackColor = Color.Transparent;
        _heroBody.Font = Theme.UiSmall;
        _heroBody.ForeColor = Theme.TextDim;
        _heroBody.Location = new Point(18, 56);
        _heroBody.Size = new Size(600, 44);
        _heroBody.Text = "Sets a measured idle ladder — display 10 min, sleep 30 min, hibernate 2 h — disables wake timers, stops USB devices dropping on AC and keeps a disk copy of the session. Every value is backed up first.";

        _heroStatus.AutoSize = true;
        _heroStatus.BackColor = Color.Transparent;
        _heroStatus.Font = Theme.MonoSmall;
        _heroStatus.ForeColor = Theme.TextDim;
        _heroStatus.Location = new Point(19, 105);
        _heroStatus.Text = "Reading current power policy…";

        _apply.Height = 34;
        _apply.Location = new Point(18, 130);
        _apply.Primary = true;
        _apply.Size = new Size(200, 34);
        _apply.Text = "OPTIMIZE SLEEP";

        _restore.Height = 34;
        _restore.Location = new Point(228, 130);
        _restore.Size = new Size(180, 34);
        _restore.Text = "RESTORE PREVIOUS";

        _split.BackColor = Theme.Bg;
        _split.ColumnCount = 2;
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _split.Controls.Add(_ladder, 0, 0);
        _split.Controls.Add(_behavior, 1, 0);
        _split.Dock = DockStyle.Fill;
        _split.Margin = new Padding(0);
        _split.RowCount = 1;
        _split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _ladder.Controls.Add(_screen);
        _ladder.Controls.Add(_sleep);
        _ladder.Controls.Add(_hibernate);
        _ladder.Controls.Add(_disk);
        _ladder.Dock = DockStyle.Fill;
        _ladder.Header = "IDLE LADDER";
        _ladder.Kicker = "TIMEOUTS";
        _ladder.Margin = new Padding(0, 0, 12, 0);

        _screen.Max = 120;
        _screen.Min = 0;
        _screen.Step = 5;
        _screen.Title = "TURN OFF DISPLAY";
        _screen.Unit = "MIN";

        _sleep.Max = 240;
        _sleep.Min = 0;
        _sleep.Step = 5;
        _sleep.Title = "SLEEP AFTER";
        _sleep.Unit = "MIN";

        _hibernate.Max = 480;
        _hibernate.Min = 0;
        _hibernate.Step = 15;
        _hibernate.Title = "HIBERNATE AFTER";
        _hibernate.Unit = "MIN";

        _disk.Max = 240;
        _disk.Min = 0;
        _disk.Step = 10;
        _disk.Title = "TURN OFF DISK";
        _disk.Unit = "MIN";

        _behavior.Controls.Add(_wake);
        _behavior.Controls.Add(_usb);
        _behavior.Controls.Add(_hybrid);
        _behavior.Controls.Add(_keepAwake);
        _behavior.Controls.Add(_settingsNote);
        _behavior.Dock = DockStyle.Fill;
        _behavior.Header = "POWER BEHAVIOR";
        _behavior.Kicker = "GOTCHAS";
        _behavior.Margin = new Padding(0);

        _wake.BadgeText = "SLEEP";
        _wake.BadgeTone = Theme.Warn;
        _wake.Subtitle = "Scheduled tasks that can wake the machine";
        _wake.Title = "ALLOW WAKE TIMERS";

        _usb.BadgeText = "USB";
        _usb.BadgeTone = Theme.Accent;
        _usb.Subtitle = "Power down idle USB devices";
        _usb.Title = "USB SELECTIVE SUSPEND";

        _hybrid.BadgeText = "DISK";
        _hybrid.BadgeTone = Theme.Violet;
        _hybrid.Subtitle = "Keep a disk copy of the session while sleeping";
        _hybrid.Title = "HYBRID SLEEP";

        _keepAwake.BadgeText = "AWAKE";
        _keepAwake.BadgeTone = Theme.Accent;
        _keepAwake.Subtitle = "Stop the machine sleeping while this is on — also held automatically during installs and disk images";
        _keepAwake.Title = "KEEP AWAKE";

        _settingsNote.AutoEllipsis = true;
        _settingsNote.AutoSize = false;
        _settingsNote.BackColor = Color.Transparent;
        _settingsNote.Font = Theme.UiSmall;
        _settingsNote.ForeColor = Theme.TextDim;
        _settingsNote.Location = new Point(18, 330);
        _settingsNote.Size = new Size(600, 20);
        _settingsNote.Text = "Values apply to the active plan. Machine-wide writes may need elevation.";

        Controls.Add(_root);

        _hero.ResumeLayout(false);
        _hero.PerformLayout();
        _ladder.ResumeLayout(false);
        _behavior.ResumeLayout(false);
        _behavior.PerformLayout();
        _split.ResumeLayout(false);
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
