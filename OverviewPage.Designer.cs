namespace PC_Medic;

partial class OverviewPage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private TableLayoutPanel _tiles;
    private StatTile _cpu;
    private StatTile _memory;
    private StatTile _disk;
    private StatTile _uptime;
    private StatTile _updates;
    private NeonButton _coolCpu;
    private NeonButton _freeMemory;
    private NeonButton _cleanDisk;
    private NeonButton _openUpdates;
    private TableLayoutPanel _split;
    private Card _profileCard;
    private InfoList _profile;
    private Card _actions;
    private NeonButton _optimize;
    private NeonButton _startup;
    private NeonButton _taskManager;
    private Card _feed;
    private ActivityConsole _console;
    private ScanOverlay _loading;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _tiles = new TableLayoutPanel();
        _cpu = new StatTile();
        _memory = new StatTile();
        _disk = new StatTile();
        _uptime = new StatTile();
        _updates = new StatTile();
        _coolCpu = new NeonButton();
        _freeMemory = new NeonButton();
        _cleanDisk = new NeonButton();
        _openUpdates = new NeonButton();
        _split = new TableLayoutPanel();
        _profileCard = new Card();
        _profile = new InfoList();
        _actions = new Card();
        _optimize = new NeonButton();
        _startup = new NeonButton();
        _taskManager = new NeonButton();
        _feed = new Card();
        _console = new ActivityConsole();
        _loading = new ScanOverlay();
        _tips = new ToolTip(components);

        _root.SuspendLayout();
        _tiles.SuspendLayout();
        _split.SuspendLayout();
        _profileCard.SuspendLayout();
        _actions.SuspendLayout();
        _feed.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_tiles, 0, 0);
        _root.Controls.Add(_split, 0, 1);
        _root.Controls.Add(_feed, 0, 2);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 3;
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 268));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _tiles.BackColor = Theme.Bg;
        _tiles.ColumnCount = 5;
        _tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        _tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        _tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        _tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        _tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        _tiles.Controls.Add(_cpu, 0, 0);
        _tiles.Controls.Add(_memory, 1, 0);
        _tiles.Controls.Add(_disk, 2, 0);
        _tiles.Controls.Add(_updates, 3, 0);
        _tiles.Controls.Add(_uptime, 4, 0);
        _tiles.Dock = DockStyle.Fill;
        _tiles.Margin = new Padding(0);
        _tiles.RowCount = 1;
        _tiles.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _cpu.Caption = "CPU LOAD";
        _cpu.Dock = DockStyle.Fill;
        _cpu.Margin = new Padding(0, 0, 12, 12);
        _cpu.Tone = Theme.Accent;

        _memory.Caption = "MEMORY";
        _memory.Dock = DockStyle.Fill;
        _memory.Margin = new Padding(0, 0, 12, 12);
        _memory.Tone = Theme.Violet;

        _disk.Caption = "SYSTEM DRIVE";
        _disk.Dock = DockStyle.Fill;
        _disk.Margin = new Padding(0, 0, 12, 12);
        _disk.Tone = Theme.Good;

        _updates.Caption = "UPDATES";
        _updates.Dock = DockStyle.Fill;
        _updates.Margin = new Padding(0, 0, 12, 12);
        _updates.Tone = Theme.Accent;

        _uptime.Caption = "UPTIME";
        _uptime.Dock = DockStyle.Fill;
        _uptime.Margin = new Padding(0, 0, 0, 12);
        _uptime.Tone = Theme.Warn;

        _coolCpu.Text = "REDUCE CPU";
        _coolCpu.Tone = Theme.Warn;

        _freeMemory.Text = "FREE MEMORY";
        _freeMemory.Tone = Theme.Violet;

        _cleanDisk.Text = "CLEAN DRIVE";
        _cleanDisk.Tone = Theme.Good;

        _openUpdates.Text = "OPEN UPDATES";
        _openUpdates.Tone = Theme.Accent;

        _split.BackColor = Theme.Bg;
        _split.ColumnCount = 2;
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        _split.Controls.Add(_profileCard, 0, 0);
        _split.Controls.Add(_actions, 1, 0);
        _split.Dock = DockStyle.Fill;
        _split.Margin = new Padding(0);
        _split.RowCount = 1;
        _split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _profileCard.Controls.Add(_profile);
        _profileCard.Dock = DockStyle.Fill;
        _profileCard.Header = "SYSTEM PROFILE";
        _profileCard.Kicker = "IDENTITY";
        _profileCard.Margin = new Padding(0, 0, 12, 12);

        _profile.Dock = DockStyle.Fill;
        _profile.RowHeight = 26;

        _actions.Controls.Add(_optimize);
        _actions.Controls.Add(_startup);
        _actions.Controls.Add(_taskManager);
        _actions.Dock = DockStyle.Fill;
        _actions.Header = "QUICK ACTIONS";
        _actions.Kicker = "REMEDIATION";
        _actions.Margin = new Padding(0, 0, 0, 12);

        _optimize.Text = "OPTIMIZE SLEEP";
        _optimize.Primary = true;

        _startup.Text = "STARTUP & BOOT";

        _taskManager.Text = "TASK MANAGER";

        _feed.Controls.Add(_console);
        _feed.Dock = DockStyle.Fill;
        _feed.Header = "LIVE ACTIVITY";
        _feed.Kicker = "TELEMETRY FEED";

        _console.Dock = DockStyle.Fill;

        _loading.Visible = false;

        Controls.Add(_loading);
        Controls.Add(_root);

        _loading.BringToFront();

        _profileCard.ResumeLayout(false);
        _actions.ResumeLayout(false);
        _actions.PerformLayout();
        _feed.ResumeLayout(false);
        _split.ResumeLayout(false);
        _tiles.ResumeLayout(false);
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
