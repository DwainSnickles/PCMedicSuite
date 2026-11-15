namespace PC_Medic;

partial class BenchPage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private TableLayoutPanel _grid;
    private ToolTile _tileCinebench;
    private ToolTile _tileFurmark;
    private ToolTile _tileHwinfo;
    private ToolTile _tileDiskMark;
    private ToolTile _tileDiskInfo;
    private ToolTile _tileFolder;
    private Panel _footer;
    private Label _status;
    private Label _pathLabel;
    private NeonButton _findMissing;
    private NeonButton _getMissing;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _grid = new TableLayoutPanel();
        _tileCinebench = new ToolTile();
        _tileFurmark = new ToolTile();
        _tileHwinfo = new ToolTile();
        _tileDiskMark = new ToolTile();
        _tileDiskInfo = new ToolTile();
        _tileFolder = new ToolTile();
        _footer = new Panel();
        _status = new Label();
        _pathLabel = new Label();
        _findMissing = new NeonButton();
        _getMissing = new NeonButton();
        _tips = new ToolTip(components);

        _root.SuspendLayout();
        _grid.SuspendLayout();
        _footer.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_grid, 0, 0);
        _root.Controls.Add(_footer, 0, 1);
        _root.Controls.Add(_pathLabel, 0, 2);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 3;
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        _grid.BackColor = Theme.Bg;
        _grid.ColumnCount = 3;
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
        _grid.Controls.Add(_tileCinebench, 0, 0);
        _grid.Controls.Add(_tileFurmark, 1, 0);
        _grid.Controls.Add(_tileHwinfo, 2, 0);
        _grid.Controls.Add(_tileDiskMark, 0, 1);
        _grid.Controls.Add(_tileDiskInfo, 1, 1);
        _grid.Controls.Add(_tileFolder, 2, 1);
        _grid.Dock = DockStyle.Fill;
        _grid.Margin = new Padding(0);
        _grid.RowCount = 2;
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        foreach (ToolTile tile in new[]
        {
            _tileCinebench, _tileFurmark, _tileHwinfo, _tileDiskMark, _tileDiskInfo, _tileFolder,
        })
        {
            tile.Dock = DockStyle.Fill;
            tile.Margin = new Padding(0, 0, 10, 10);
        }

        _tileFolder.ActionText = "OPEN ↗";
        _tileFolder.ActionTone = Theme.Accent;
        _tileFolder.Glyph = "";
        _tileFolder.Mark = RigMark.Folder;
        _tileFolder.Status = "PATH";
        _tileFolder.StatusTone = Theme.TextDim;
        _tileFolder.Subtitle = "Drop portable test rigs here";
        _tileFolder.Title = "SUITE FOLDER";
        _tileFolder.Tone = Theme.Violet;

        _footer.BackColor = Theme.Bg;
        _footer.Controls.Add(_status);
        _footer.Controls.Add(_findMissing);
        _footer.Controls.Add(_getMissing);
        _footer.Dock = DockStyle.Fill;
        _footer.Margin = new Padding(0);

        _status.AutoSize = true;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(2, 7);

        _pathLabel.AutoEllipsis = true;
        _pathLabel.AutoSize = false;
        _pathLabel.BackColor = Color.Transparent;
        _pathLabel.Dock = DockStyle.Fill;
        _pathLabel.Font = Theme.MonoSmall;
        _pathLabel.ForeColor = Theme.TextDim;
        _pathLabel.Margin = new Padding(2, 0, 0, 0);
        _pathLabel.Text = Tools.PortableRoot;
        _pathLabel.TextAlign = ContentAlignment.MiddleLeft;

        _findMissing.Height = 28;
        _findMissing.Location = new Point(400, 1);
        _findMissing.Size = new Size(168, 28);
        _findMissing.Text = "SEARCH THIS PC";

        _getMissing.Height = 28;
        _getMissing.Location = new Point(576, 1);
        _getMissing.Size = new Size(210, 28);
        _getMissing.Text = "GET ALL MISSING";

        Controls.Add(_root);

        _grid.ResumeLayout(false);
        _footer.ResumeLayout(false);
        _footer.PerformLayout();
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
