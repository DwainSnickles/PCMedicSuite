namespace PC_Medic;

partial class AuditPage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private Card _registryCard;
    private TableLayoutPanel _registryInner;
    private Panel _registryToolbar;
    private BufferedPanel _registryList;
    private ScanOverlay _registryLoading;
    private Label _registryStatus;
    private NeonButton _registryAll;
    private NeonButton _registryRemove;
    private NeonButton _registryRestore;
    private NeonButton _registryScan;
    private Card _shortcutCard;
    private TableLayoutPanel _shortcutInner;
    private Panel _shortcutToolbar;
    private BufferedPanel _shortcutList;
    private ScanOverlay _shortcutLoading;
    private Label _shortcutStatus;
    private NeonButton _shortcutAll;
    private NeonButton _shortcutKeep;
    private NeonButton _shortcutRecycle;
    private NeonButton _shortcutScan;
    private Label _footer;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _registryCard = new Card();
        _registryInner = new TableLayoutPanel();
        _registryToolbar = new Panel();
        _registryList = new BufferedPanel();
        _registryLoading = new ScanOverlay();
        _registryStatus = new Label();
        _registryAll = new NeonButton();
        _registryRemove = new NeonButton();
        _registryRestore = new NeonButton();
        _registryScan = new NeonButton();
        _shortcutCard = new Card();
        _shortcutInner = new TableLayoutPanel();
        _shortcutToolbar = new Panel();
        _shortcutList = new BufferedPanel();
        _shortcutLoading = new ScanOverlay();
        _shortcutStatus = new Label();
        _shortcutAll = new NeonButton();
        _shortcutKeep = new NeonButton();
        _shortcutRecycle = new NeonButton();
        _shortcutScan = new NeonButton();
        _footer = new Label();
        _tips = new ToolTip(components);

        _root.SuspendLayout();
        _registryCard.SuspendLayout();
        _registryInner.SuspendLayout();
        _registryToolbar.SuspendLayout();
        _shortcutCard.SuspendLayout();
        _shortcutInner.SuspendLayout();
        _shortcutToolbar.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 2;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _root.Controls.Add(_registryCard, 0, 0);
        _root.Controls.Add(_shortcutCard, 1, 0);
        _root.Controls.Add(_footer, 0, 1);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 2;
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        _registryCard.Controls.Add(_registryInner);
        _registryCard.Controls.Add(_registryLoading);
        _registryCard.Dock = DockStyle.Fill;
        _registryCard.Header = "REGISTRY AUDIT";
        _registryCard.Kicker = "PROVEN DEAD ENTRIES";
        _registryCard.Margin = new Padding(0, 0, 6, 8);

        _registryInner.BackColor = Theme.Surface;
        _registryInner.ColumnCount = 1;
        _registryInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _registryInner.Controls.Add(_registryToolbar, 0, 0);
        _registryInner.Controls.Add(_registryList, 0, 1);
        _registryInner.Dock = DockStyle.Fill;
        _registryInner.Margin = new Padding(0);
        _registryInner.RowCount = 2;
        _registryInner.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        _registryInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _registryToolbar.BackColor = Theme.Surface;
        _registryToolbar.Controls.Add(_registryStatus);
        _registryToolbar.Controls.Add(_registryAll);
        _registryToolbar.Controls.Add(_registryRemove);
        _registryToolbar.Controls.Add(_registryRestore);
        _registryToolbar.Controls.Add(_registryScan);
        _registryToolbar.Dock = DockStyle.Fill;
        _registryToolbar.Margin = new Padding(0);

        _registryStatus.AutoEllipsis = true;
        _registryStatus.AutoSize = false;
        _registryStatus.BackColor = Color.Transparent;
        _registryStatus.Font = Theme.MonoSmall;
        _registryStatus.ForeColor = Theme.TextDim;
        _registryStatus.Location = new Point(2, 4);
        _registryStatus.Size = new Size(400, 18);
        _registryStatus.Text = "NOT SCANNED";
        _registryStatus.TextAlign = ContentAlignment.MiddleLeft;

        _registryAll.Height = 28;
        _registryAll.Location = new Point(400, 26);
        _registryAll.Size = new Size(56, 28);
        _registryAll.Text = "ALL";

        _registryRemove.Height = 28;
        _registryRemove.Location = new Point(336, 26);
        _registryRemove.Primary = true;
        _registryRemove.Size = new Size(116, 28);
        _registryRemove.Text = "REMOVE (0)";

        _registryRestore.Height = 28;
        _registryRestore.Location = new Point(238, 26);
        _registryRestore.Size = new Size(92, 28);
        _registryRestore.Text = "RESTORE";

        _registryScan.Height = 28;
        _registryScan.Location = new Point(152, 26);
        _registryScan.Size = new Size(78, 28);
        _registryScan.Text = "RESCAN";

        _registryList.AutoScroll = true;
        _registryList.BackColor = Theme.Surface;
        _registryList.Dock = DockStyle.Fill;
        _registryList.Margin = new Padding(0);

        _registryLoading.Visible = false;

        _shortcutCard.Controls.Add(_shortcutInner);
        _shortcutCard.Controls.Add(_shortcutLoading);
        _shortcutCard.Dock = DockStyle.Fill;
        _shortcutCard.Header = "SHORTCUTS";
        _shortcutCard.Kicker = "DEAD TARGETS";
        _shortcutCard.Margin = new Padding(6, 0, 0, 8);

        _shortcutInner.BackColor = Theme.Surface;
        _shortcutInner.ColumnCount = 1;
        _shortcutInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _shortcutInner.Controls.Add(_shortcutToolbar, 0, 0);
        _shortcutInner.Controls.Add(_shortcutList, 0, 1);
        _shortcutInner.Dock = DockStyle.Fill;
        _shortcutInner.Margin = new Padding(0);
        _shortcutInner.RowCount = 2;
        _shortcutInner.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        _shortcutInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _shortcutToolbar.BackColor = Theme.Surface;
        _shortcutToolbar.Controls.Add(_shortcutStatus);
        _shortcutToolbar.Controls.Add(_shortcutAll);
        _shortcutToolbar.Controls.Add(_shortcutKeep);
        _shortcutToolbar.Controls.Add(_shortcutRecycle);
        _shortcutToolbar.Controls.Add(_shortcutScan);
        _shortcutToolbar.Dock = DockStyle.Fill;
        _shortcutToolbar.Margin = new Padding(0);

        _shortcutStatus.AutoEllipsis = true;
        _shortcutStatus.AutoSize = false;
        _shortcutStatus.BackColor = Color.Transparent;
        _shortcutStatus.Font = Theme.MonoSmall;
        _shortcutStatus.ForeColor = Theme.TextDim;
        _shortcutStatus.Location = new Point(2, 4);
        _shortcutStatus.Size = new Size(400, 18);
        _shortcutStatus.Text = "NOT SCANNED";
        _shortcutStatus.TextAlign = ContentAlignment.MiddleLeft;

        _shortcutAll.Height = 28;
        _shortcutAll.Location = new Point(400, 26);
        _shortcutAll.Size = new Size(56, 28);
        _shortcutAll.Text = "ALL";

        _shortcutKeep.Height = 28;
        _shortcutKeep.Location = new Point(290, 26);
        _shortcutKeep.Size = new Size(104, 28);
        _shortcutKeep.Text = "NOT DEAD";

        _shortcutRecycle.Height = 28;
        _shortcutRecycle.Location = new Point(166, 26);
        _shortcutRecycle.Primary = true;
        _shortcutRecycle.Size = new Size(118, 28);
        _shortcutRecycle.Text = "RECYCLE (0)";

        _shortcutScan.Height = 28;
        _shortcutScan.Location = new Point(80, 26);
        _shortcutScan.Size = new Size(78, 28);
        _shortcutScan.Text = "RESCAN";

        _shortcutList.AutoScroll = true;
        _shortcutList.BackColor = Theme.Surface;
        _shortcutList.Dock = DockStyle.Fill;
        _shortcutList.Margin = new Padding(0);

        _shortcutLoading.Visible = false;

        _footer.AutoEllipsis = true;
        _footer.AutoSize = false;
        _footer.BackColor = Color.Transparent;
        _footer.Dock = DockStyle.Fill;
        _footer.Font = Theme.MonoSmall;
        _footer.ForeColor = Theme.TextDim;
        _footer.Location = new Point(2, 0);
        _footer.Margin = new Padding(2, 0, 0, 0);
        _footer.Size = new Size(800, 24);
        _footer.Text = "BACKUPS — RECYCLED SHORTCUTS RESTORE FROM THE BIN";
        _footer.TextAlign = ContentAlignment.MiddleLeft;

        Controls.Add(_root);

        _registryToolbar.ResumeLayout(false);
        _registryToolbar.PerformLayout();
        _registryInner.ResumeLayout(false);
        _shortcutToolbar.ResumeLayout(false);
        _shortcutToolbar.PerformLayout();
        _shortcutInner.ResumeLayout(false);
        _registryCard.ResumeLayout(false);
        _shortcutCard.ResumeLayout(false);
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
