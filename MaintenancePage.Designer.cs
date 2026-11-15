namespace PC_Medic;

partial class MaintenancePage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private TableLayoutPanel _grid;
    private Label _status;
    private ToolTip _tips;
    private ToolTile _tileCleanup;
    private ToolTile _tileDefrag;
    private ToolTile _tileRestorePoint;
    private ToolTile _tileWindowsUpdate;
    private ToolTile _tileUninstallApps;
    private ToolTile _tileStartupApps;
    private ToolTile _tileAnimation;
    private ToolTile _tileSfc;
    private ToolTile _tileMemoryDiag;
    private ToolTile _tileSystemConfig;
    private ToolTile _tilePowerPlan;
    private ToolTile _tileUltimatePlan;
    private ToolTile _tileStorageSense;
    private ToolTile _tileTaskManager;
    private ToolTile _tileEventViewer;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _grid = new TableLayoutPanel();
        _status = new Label();
        _tips = new ToolTip(components);
        _tileCleanup = new ToolTile();
        _tileDefrag = new ToolTile();
        _tileRestorePoint = new ToolTile();
        _tileWindowsUpdate = new ToolTile();
        _tileUninstallApps = new ToolTile();
        _tileStartupApps = new ToolTile();
        _tileAnimation = new ToolTile();
        _tileSfc = new ToolTile();
        _tileMemoryDiag = new ToolTile();
        _tileSystemConfig = new ToolTile();
        _tilePowerPlan = new ToolTile();
        _tileUltimatePlan = new ToolTile();
        _tileStorageSense = new ToolTile();
        _tileTaskManager = new ToolTile();
        _tileEventViewer = new ToolTile();

        _root.SuspendLayout();
        _grid.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_grid, 0, 0);
        _root.Controls.Add(_status, 0, 1);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 2;
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _grid.BackColor = Theme.Bg;
        _grid.ColumnCount = 3;
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333f));
        _grid.Controls.Add(_tileCleanup, 0, 0);
        _grid.Controls.Add(_tileDefrag, 1, 0);
        _grid.Controls.Add(_tileRestorePoint, 2, 0);
        _grid.Controls.Add(_tileWindowsUpdate, 0, 1);
        _grid.Controls.Add(_tileUninstallApps, 1, 1);
        _grid.Controls.Add(_tileStartupApps, 2, 1);
        _grid.Controls.Add(_tileAnimation, 0, 2);
        _grid.Controls.Add(_tileSfc, 1, 2);
        _grid.Controls.Add(_tileMemoryDiag, 2, 2);
        _grid.Controls.Add(_tileSystemConfig, 0, 3);
        _grid.Controls.Add(_tilePowerPlan, 1, 3);
        _grid.Controls.Add(_tileUltimatePlan, 2, 3);
        _grid.Controls.Add(_tileStorageSense, 0, 4);
        _grid.Controls.Add(_tileTaskManager, 1, 4);
        _grid.Controls.Add(_tileEventViewer, 2, 4);
        _grid.Dock = DockStyle.Fill;
        _grid.Margin = new Padding(0);
        _grid.RowCount = 5;
        for (int i = 0; i < 5; i++) _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        foreach (ToolTile tile in new[]
        {
            _tileCleanup, _tileDefrag, _tileRestorePoint, _tileWindowsUpdate, _tileUninstallApps,
            _tileStartupApps, _tileAnimation, _tileSfc, _tileMemoryDiag, _tileSystemConfig,
            _tilePowerPlan, _tileUltimatePlan, _tileStorageSense, _tileTaskManager, _tileEventViewer,
        })
        {
            tile.Dock = DockStyle.Fill;
            tile.Margin = new Padding(0, 0, 10, 10);
        }

        _status.AutoSize = true;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(2, 6);
        _status.Text = "STANDARD USER — ADMIN TOOLS WILL ASK FOR ELEVATION";

        Controls.Add(_root);

        _grid.ResumeLayout(false);
        _root.ResumeLayout(false);
        _root.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}
