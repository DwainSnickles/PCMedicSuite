namespace PC_Medic;

partial class TweaksPage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private TableLayoutPanel _split;
    private Card _appearance;
    private Label _appearanceNote;
    private Card _system;
    private Label _systemNote;
    private Card _gaming;
    private Label _gamingNote;
    private Card _godMode;
    private NeonButton _godModeButton;
    private Label _godModeNote;
    private Panel _footer;
    private Label _status;
    private NeonButton _rescan;
    private NeonButton _revert;
    private NeonButton _history;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _split = new TableLayoutPanel();
        _appearance = new Card();
        _appearanceNote = new Label();
        _system = new Card();
        _systemNote = new Label();
        _gaming = new Card();
        _gamingNote = new Label();
        _godMode = new Card();
        _godModeButton = new NeonButton();
        _godModeNote = new Label();
        _footer = new Panel();
        _status = new Label();
        _rescan = new NeonButton();
        _revert = new NeonButton();
        _history = new NeonButton();

        _root.SuspendLayout();
        _split.SuspendLayout();
        _appearance.SuspendLayout();
        _system.SuspendLayout();
        _gaming.SuspendLayout();
        _godMode.SuspendLayout();
        _footer.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_split, 0, 0);
        _root.Controls.Add(_godMode, 0, 1);
        _root.Controls.Add(_footer, 0, 2);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 3;
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        _split.BackColor = Theme.Bg;
        _split.ColumnCount = 3;
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        _split.Controls.Add(_appearance, 0, 0);
        _split.Controls.Add(_system, 1, 0);
        _split.Controls.Add(_gaming, 2, 0);
        _split.Dock = DockStyle.Fill;
        _split.Margin = new Padding(0);
        _split.RowCount = 1;
        _split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _appearance.Controls.Add(_appearanceNote);
        _appearance.Dock = DockStyle.Fill;
        _appearance.Header = "VISUAL & UI";
        _appearance.Kicker = "APPEARANCE";
        _appearance.Margin = new Padding(0, 0, 12, 12);

        _appearanceNote.AutoSize = false;
        _appearanceNote.BackColor = Color.Transparent;
        _appearanceNote.Font = Theme.UiSmall;
        _appearanceNote.ForeColor = Theme.TextDim;
        _appearanceNote.Location = new Point(18, 300);
        _appearanceNote.Size = new Size(400, 42);
        _appearanceNote.Text = "Per-user values apply at once; Explorer picks up appearance changes for new windows.";

        _system.Controls.Add(_systemNote);
        _system.Dock = DockStyle.Fill;
        _system.Header = "SYSTEM & PRIVACY";
        _system.Kicker = "SERVICES & DATA";
        _system.Margin = new Padding(0, 0, 12, 12);

        _systemNote.AutoSize = false;
        _systemNote.BackColor = Color.Transparent;
        _systemNote.Font = Theme.UiSmall;
        _systemNote.ForeColor = Theme.TextDim;
        _systemNote.Location = new Point(18, 300);
        _systemNote.Size = new Size(400, 42);
        _systemNote.Text = "Elevated switches write a policy or run powercfg — switching back removes them again.";

        _gaming.Controls.Add(_gamingNote);
        _gaming.Dock = DockStyle.Fill;
        _gaming.Header = "GAMING";
        _gaming.Kicker = "LATENCY & PRIORITY";
        _gaming.Margin = new Padding(0, 0, 0, 12);

        _gamingNote.AutoSize = false;
        _gamingNote.BackColor = Color.Transparent;
        _gamingNote.Font = Theme.UiSmall;
        _gamingNote.ForeColor = Theme.TextDim;
        _gamingNote.Location = new Point(18, 400);
        _gamingNote.Size = new Size(400, 42);
        _gamingNote.Text = "ADMIN rows need RESTART AS ADMIN; HAGS and priority changes take effect after a restart.";

        _godMode.Controls.Add(_godModeButton);
        _godMode.Controls.Add(_godModeNote);
        _godMode.Dock = DockStyle.Fill;
        _godMode.Header = "GOD MODE";
        _godMode.Kicker = "ALL SETTINGS";
        _godMode.Margin = new Padding(0, 0, 0, 12);

        _godModeButton.Height = 34;
        _godModeButton.Location = new Point(18, 56);
        _godModeButton.Primary = true;
        _godModeButton.Size = new Size(190, 34);
        _godModeButton.Text = "OPEN GOD MODE";

        _godModeNote.AutoSize = false;
        _godModeNote.BackColor = Color.Transparent;
        _godModeNote.Font = Theme.UiSmall;
        _godModeNote.ForeColor = Theme.TextDim;
        _godModeNote.Location = new Point(224, 60);
        _godModeNote.Size = new Size(600, 40);
        _godModeNote.Text = "Creates the God Mode folder on your desktop and opens every Control Panel applet in one window. Delete the folder to remove the shortcut.";

        _footer.BackColor = Theme.Bg;
        _footer.Controls.Add(_status);
        _footer.Controls.Add(_revert);
        _footer.Controls.Add(_history);
        _footer.Controls.Add(_rescan);
        _footer.Dock = DockStyle.Fill;

        _status.AutoSize = true;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(2, 9);

        _rescan.Height = 30;
        _rescan.Location = new Point(800, 2);
        _rescan.Size = new Size(108, 30);
        _rescan.Text = "RESCAN";

        _revert.Height = 30;
        _revert.Location = new Point(660, 2);
        _revert.Primary = true;
        _revert.Size = new Size(132, 30);
        _revert.Text = "REVERT ALL";

        _history.Height = 30;
        _history.Location = new Point(534, 2);
        _history.Size = new Size(118, 30);
        _history.Text = "HISTORY";

        Controls.Add(_root);

        _appearance.ResumeLayout(false);
        _appearance.PerformLayout();
        _system.ResumeLayout(false);
        _system.PerformLayout();
        _gaming.ResumeLayout(false);
        _gaming.PerformLayout();
        _godMode.ResumeLayout(false);
        _godMode.PerformLayout();
        _footer.ResumeLayout(false);
        _footer.PerformLayout();
        _split.ResumeLayout(false);
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
