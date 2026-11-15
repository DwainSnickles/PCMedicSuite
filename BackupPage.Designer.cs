namespace PC_Medic;

partial class BackupPage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private TableLayoutPanel _grid;
    private Card _backupCard;
    private NeonButton _srcPick;
    private Label _destLabel;
    private NeonButton _browseDest;
    private NeonButton _compPick;
    private NeonButton _startBackup;
    private NeonButton _backupToDisk;
    private NeonButton _bootableDisk;
    private Label _backupStatus;
    private PassBar _backupBar;
    private ScanOverlay _backupOverlay;
    private Card _restoreCard;
    private Label _wimLabel;
    private NeonButton _pointerPick;
    private NeonButton _browseWim;
    private NeonButton _imagePick;
    private Label _imageName;
    private BufferedPanel _imageHost;
    private InfoList _imageList;
    private NeonButton _dstPick;
    private NeonButton _restoreBtn;
    private NeonButton _checkStick;
    private Label _restoreStatus;
    private PassBar _restoreBar;
    private ScanOverlay _restoreOverlay;
    private Card _bootCard;
    private NeonButton _bootPick;
    private NeonButton _firmwarePick;
    private NeonButton _bootRepair;
    private Label _bootStatus;
    private Label _bootNote;
    private Card _healthCard;
    private NeonButton _healthRun;
    private Label _healthStatus;
    private Label _healthSummary;
    private Label _healthNote;
    private Panel _footerPanel;
    private Label _status;
    private NeonButton _refresh;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _grid = new TableLayoutPanel();
        _backupCard = new Card();
        _srcPick = new NeonButton();
        _destLabel = new Label();
        _browseDest = new NeonButton();
        _compPick = new NeonButton();
        _startBackup = new NeonButton();
        _backupToDisk = new NeonButton();
        _bootableDisk = new NeonButton();
        _backupStatus = new Label();
        _backupBar = new PassBar();
        _backupOverlay = new ScanOverlay();
        _restoreCard = new Card();
        _wimLabel = new Label();
        _pointerPick = new NeonButton();
        _browseWim = new NeonButton();
        _imagePick = new NeonButton();
        _imageName = new Label();
        _imageHost = new BufferedPanel();
        _imageList = new InfoList();
        _dstPick = new NeonButton();
        _restoreBtn = new NeonButton();
        _checkStick = new NeonButton();
        _restoreStatus = new Label();
        _restoreBar = new PassBar();
        _restoreOverlay = new ScanOverlay();
        _bootCard = new Card();
        _bootPick = new NeonButton();
        _firmwarePick = new NeonButton();
        _bootRepair = new NeonButton();
        _bootStatus = new Label();
        _bootNote = new Label();
        _healthCard = new Card();
        _healthRun = new NeonButton();
        _healthStatus = new Label();
        _healthSummary = new Label();
        _healthNote = new Label();
        _footerPanel = new Panel();
        _status = new Label();
        _refresh = new NeonButton();
        _tips = new ToolTip(components);

        _root.SuspendLayout();
        _grid.SuspendLayout();
        _backupCard.SuspendLayout();
        _restoreCard.SuspendLayout();
        _imageHost.SuspendLayout();
        _bootCard.SuspendLayout();
        _healthCard.SuspendLayout();
        _footerPanel.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_grid, 0, 0);
        _root.Controls.Add(_footerPanel, 0, 1);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 2;
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        _grid.BackColor = Theme.Bg;
        _grid.ColumnCount = 2;
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _grid.Controls.Add(_backupCard, 0, 0);
        _grid.Controls.Add(_restoreCard, 1, 0);
        _grid.Controls.Add(_bootCard, 0, 1);
        _grid.Controls.Add(_healthCard, 1, 1);
        _grid.Dock = DockStyle.Fill;
        _grid.Margin = new Padding(0);
        _grid.RowCount = 2;
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 64));
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 36));

        _backupCard.Controls.Add(_srcPick);
        _backupCard.Controls.Add(_destLabel);
        _backupCard.Controls.Add(_browseDest);
        _backupCard.Controls.Add(_compPick);
        _backupCard.Controls.Add(_startBackup);
        _backupCard.Controls.Add(_backupToDisk);
        _backupCard.Controls.Add(_bootableDisk);
        _backupCard.Controls.Add(_backupStatus);
        _backupCard.Controls.Add(_backupBar);
        _backupCard.Controls.Add(_backupOverlay);
        _backupCard.Dock = DockStyle.Fill;
        _backupCard.Header = "DISK IMAGE BACKUP";
        _backupCard.Kicker = "WIMLIB CAPTURE";
        _backupCard.Margin = new Padding(0, 0, 12, 12);

        _srcPick.Location = new Point(18, 58);
        _srcPick.Size = new Size(400, 32);
        _srcPick.Text = "SOURCE: —";

        _destLabel.AutoEllipsis = true;
        _destLabel.AutoSize = false;
        _destLabel.BackColor = Color.Transparent;
        _destLabel.Font = Theme.MonoSmall;
        _destLabel.ForeColor = Theme.TextDim;
        _destLabel.Location = new Point(18, 103);
        _destLabel.Size = new Size(270, 22);

        _browseDest.Height = 30;
        _browseDest.Location = new Point(300, 98);
        _browseDest.Size = new Size(120, 30);
        _browseDest.Text = "BROWSE…";
        _browseDest.Width = 120;

        _compPick.Location = new Point(18, 140);
        _compPick.Size = new Size(190, 30);
        _compPick.Text = "COMPRESSION: FAST";

        _startBackup.Height = 32;
        _startBackup.Location = new Point(230, 140);
        _startBackup.Primary = true;
        _startBackup.Size = new Size(170, 32);
        _startBackup.Text = "START BACKUP";

        _backupToDisk.Height = 32;
        _backupToDisk.Location = new Point(18, 186);
        _backupToDisk.Size = new Size(200, 32);
        _backupToDisk.Text = "BACK UP TO DISK…";

        _bootableDisk.Height = 30;
        _bootableDisk.Location = new Point(268, 187);
        _bootableDisk.Size = new Size(140, 30);
        _bootableDisk.Text = "BOOTABLE DISK…";

        _backupStatus.AutoSize = false;
        _backupStatus.BackColor = Color.Transparent;
        _backupStatus.Font = Theme.MonoSmall;
        _backupStatus.ForeColor = Theme.TextDim;
        _backupStatus.Location = new Point(18, 300);
        _backupStatus.Size = new Size(400, 20);

        _backupBar.Location = new Point(18, 324);
        _backupBar.Size = new Size(400, 6);

        _backupOverlay.Visible = false;

        _restoreCard.Controls.Add(_wimLabel);
        _restoreCard.Controls.Add(_pointerPick);
        _restoreCard.Controls.Add(_browseWim);
        _restoreCard.Controls.Add(_imagePick);
        _restoreCard.Controls.Add(_imageName);
        _restoreCard.Controls.Add(_imageHost);
        _restoreCard.Controls.Add(_dstPick);
        _restoreCard.Controls.Add(_restoreBtn);
        _restoreCard.Controls.Add(_checkStick);
        _restoreCard.Controls.Add(_restoreStatus);
        _restoreCard.Controls.Add(_restoreBar);
        _restoreCard.Controls.Add(_restoreOverlay);
        _restoreCard.Dock = DockStyle.Fill;
        _restoreCard.Header = "IMAGE RESTORE";
        _restoreCard.Kicker = "WIMLIB APPLY + BOOT REPAIR";
        _restoreCard.Margin = new Padding(0, 0, 0, 12);

        _wimLabel.AutoEllipsis = true;
        _wimLabel.AutoSize = false;
        _wimLabel.BackColor = Color.Transparent;
        _wimLabel.Font = Theme.MonoSmall;
        _wimLabel.ForeColor = Theme.TextDim;
        _wimLabel.Location = new Point(18, 103);
        _wimLabel.Size = new Size(150, 22);

        _pointerPick.Height = 30;
        _pointerPick.Location = new Point(270, 98);
        _pointerPick.Size = new Size(116, 30);
        _pointerPick.Text = "STICK…";
        _pointerPick.Width = 116;

        _browseWim.Height = 30;
        _browseWim.Location = new Point(394, 98);
        _browseWim.Size = new Size(120, 30);
        _browseWim.Text = "BROWSE…";
        _browseWim.Width = 120;

        _imagePick.Location = new Point(18, 140);
        _imagePick.Size = new Size(150, 30);
        _imagePick.Text = "IMAGE 1/1";

        _imageName.AutoEllipsis = true;
        _imageName.AutoSize = false;
        _imageName.BackColor = Color.Transparent;
        _imageName.Font = Theme.MonoSmall;
        _imageName.ForeColor = Theme.TextDim;
        _imageName.Location = new Point(178, 145);
        _imageName.Size = new Size(200, 22);

        _imageHost.AutoScroll = true;
        _imageHost.BackColor = Theme.Surface;
        _imageHost.Controls.Add(_imageList);
        _imageHost.Location = new Point(18, 180);
        _imageHost.Size = new Size(400, 120);
        _imageHost.Visible = false;

        _imageList.Dock = DockStyle.Fill;

        _dstPick.Location = new Point(18, 340);
        _dstPick.Size = new Size(400, 32);
        _dstPick.Text = "TARGET: —";

        _restoreBtn.Height = 32;
        _restoreBtn.Location = new Point(18, 380);
        _restoreBtn.Primary = true;
        _restoreBtn.Size = new Size(170, 32);
        _restoreBtn.Text = "RESTORE IMAGE";

        _checkStick.Height = 32;
        _checkStick.Location = new Point(200, 380);
        _checkStick.Size = new Size(150, 32);
        _checkStick.Text = "CHECK STICK";

        _restoreStatus.AutoSize = false;
        _restoreStatus.BackColor = Color.Transparent;
        _restoreStatus.Font = Theme.MonoSmall;
        _restoreStatus.ForeColor = Theme.TextDim;
        _restoreStatus.Location = new Point(18, 420);
        _restoreStatus.Size = new Size(400, 20);

        _restoreBar.Location = new Point(18, 444);
        _restoreBar.Size = new Size(400, 6);

        _restoreOverlay.Visible = false;

        _bootCard.Controls.Add(_bootPick);
        _bootCard.Controls.Add(_firmwarePick);
        _bootCard.Controls.Add(_bootRepair);
        _bootCard.Controls.Add(_bootStatus);
        _bootCard.Controls.Add(_bootNote);
        _bootCard.Dock = DockStyle.Fill;
        _bootCard.Header = "BOOT REPAIR";
        _bootCard.Kicker = "BCDBOOT FOR OFFLINE WINDOWS";
        _bootCard.Margin = new Padding(0, 0, 12, 0);

        _bootPick.Location = new Point(18, 58);
        _bootPick.Size = new Size(300, 32);
        _bootPick.Text = "TARGET: —";

        _firmwarePick.Location = new Point(330, 58);
        _firmwarePick.Size = new Size(140, 32);
        _firmwarePick.Text = "FIRMWARE: UEFI";

        _bootRepair.Height = 32;
        _bootRepair.Location = new Point(478, 58);
        _bootRepair.Size = new Size(130, 32);
        _bootRepair.Text = "REPAIR BOOT";

        _bootStatus.AutoSize = false;
        _bootStatus.BackColor = Color.Transparent;
        _bootStatus.Font = Theme.MonoSmall;
        _bootStatus.ForeColor = Theme.TextDim;
        _bootStatus.Location = new Point(18, 100);
        _bootStatus.Size = new Size(590, 60);

        _bootNote.AutoEllipsis = true;
        _bootNote.AutoSize = false;
        _bootNote.BackColor = Color.Transparent;
        _bootNote.Font = Theme.UiSmall;
        _bootNote.ForeColor = Theme.TextDim;
        _bootNote.Location = new Point(18, 240);
        _bootNote.Size = new Size(590, 20);
        _bootNote.Text = "Boot files are written with bcdboot — REPAIR BOOT needs an elevated run.";

        _healthCard.Controls.Add(_healthRun);
        _healthCard.Controls.Add(_healthStatus);
        _healthCard.Controls.Add(_healthSummary);
        _healthCard.Controls.Add(_healthNote);
        _healthCard.Dock = DockStyle.Fill;
        _healthCard.Header = "COMPONENT STORE HEALTH";
        _healthCard.Kicker = "DISM + SFC";
        _healthCard.Margin = new Padding(0);

        _healthRun.Height = 32;
        _healthRun.Location = new Point(18, 56);
        _healthRun.Primary = true;
        _healthRun.Size = new Size(210, 32);
        _healthRun.Text = "RUN HEALTH CHECK";

        _healthStatus.AutoSize = false;
        _healthStatus.BackColor = Color.Transparent;
        _healthStatus.Font = Theme.MonoSmall;
        _healthStatus.ForeColor = Theme.TextDim;
        _healthStatus.Location = new Point(238, 56);
        _healthStatus.Size = new Size(380, 32);
        _healthStatus.TextAlign = ContentAlignment.MiddleLeft;

        _healthSummary.AutoEllipsis = true;
        _healthSummary.AutoSize = false;
        _healthSummary.BackColor = Color.Transparent;
        _healthSummary.Font = Theme.MonoSmall;
        _healthSummary.ForeColor = Theme.TextDim;
        _healthSummary.Location = new Point(18, 100);
        _healthSummary.Size = new Size(600, 100);

        _healthNote.AutoEllipsis = true;
        _healthNote.AutoSize = false;
        _healthNote.BackColor = Color.Transparent;
        _healthNote.Font = Theme.UiSmall;
        _healthNote.ForeColor = Theme.TextDim;
        _healthNote.Location = new Point(18, 240);
        _healthNote.Size = new Size(600, 20);
        _healthNote.Text = "DISM and SFC inspect the live component store; repairs need an elevated run.";

        _footerPanel.BackColor = Theme.Bg;
        _footerPanel.Controls.Add(_status);
        _footerPanel.Controls.Add(_refresh);
        _footerPanel.Dock = DockStyle.Fill;
        _footerPanel.Margin = new Padding(0);

        _status.AutoSize = true;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(2, 10);

        _refresh.Height = 30;
        _refresh.Location = new Point(700, 3);
        _refresh.Size = new Size(96, 30);
        _refresh.Text = "REFRESH";

        Controls.Add(_root);

        _backupCard.ResumeLayout(false);
        _backupCard.PerformLayout();
        _restoreCard.ResumeLayout(false);
        _restoreCard.PerformLayout();
        _imageHost.ResumeLayout(false);
        _bootCard.ResumeLayout(false);
        _bootCard.PerformLayout();
        _healthCard.ResumeLayout(false);
        _healthCard.PerformLayout();
        _footerPanel.ResumeLayout(false);
        _footerPanel.PerformLayout();
        _grid.ResumeLayout(false);
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
