namespace PC_Medic;

partial class BulkInstallForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _title;
    private Label _subtitle;
    private Label _status;
    private BufferedPanel _list;
    private NeonButton _pickFolder;
    private NeonButton _addFiles;
    private NeonButton _all;
    private NeonButton _none;
    private NeonButton _silent;
    private NeonButton _defer;
    private NeonButton _run;
    private NeonButton _restart;
    private NeonButton _help;
    private NeonButton _close;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _title = new Label();
        _subtitle = new Label();
        _status = new Label();
        _list = new BufferedPanel();
        _pickFolder = new NeonButton();
        _addFiles = new NeonButton();
        _all = new NeonButton();
        _none = new NeonButton();
        _silent = new NeonButton();
        _defer = new NeonButton();
        _run = new NeonButton();
        _restart = new NeonButton();
        _help = new NeonButton();
        _close = new NeonButton();
        _tips = new ToolTip(components);

        SuspendLayout();

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(18, 14);
        _title.Text = "BULK INSTALLER";

        _subtitle.AutoSize = false;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = Theme.UiSmall;
        _subtitle.ForeColor = Theme.TextDim;
        _subtitle.Location = new Point(20, 44);
        _subtitle.Size = new Size(920, 34);
        _subtitle.Text = "Tick the installers to run. They go one at a time, quietly where the format has a switch for it, and no restart is allowed until the last package has finished.";

        _status.AutoEllipsis = true;
        _status.AutoSize = false;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(18, 126);
        _status.Size = new Size(924, 18);
        _status.TextAlign = ContentAlignment.MiddleLeft;

        _list.AutoScroll = true;
        _list.BackColor = Theme.Surface;
        _list.Location = new Point(16, 150);
        _list.Margin = new Padding(0);
        _list.Size = new Size(928, 348);

        _pickFolder.Height = 30;
        _pickFolder.Location = new Point(18, 88);
        _pickFolder.Size = new Size(132, 30);
        _pickFolder.Text = "PICK FOLDER";

        _addFiles.Height = 30;
        _addFiles.Location = new Point(158, 88);
        _addFiles.Size = new Size(116, 30);
        _addFiles.Text = "ADD FILES…";

        _all.Height = 30;
        _all.Location = new Point(282, 88);
        _all.Size = new Size(74, 30);
        _all.Text = "ALL";

        _none.Height = 30;
        _none.Location = new Point(364, 88);
        _none.Size = new Size(74, 30);
        _none.Text = "NONE";

        _silent.Height = 30;
        _silent.Location = new Point(18, 510);
        _silent.Size = new Size(156, 30);

        _defer.Height = 30;
        _defer.Location = new Point(184, 510);
        _defer.Size = new Size(208, 30);

        _run.Height = 34;
        _run.Location = new Point(18, 568);
        _run.Primary = true;
        _run.Size = new Size(178, 34);
        _run.Text = "RUN SELECTED";

        _restart.Height = 34;
        _restart.Location = new Point(206, 568);
        _restart.Size = new Size(150, 34);
        _restart.Text = "RESTART NOW";
        _restart.Visible = false;

        _help.AccessibleName = "Help";
        _help.Location = new Point(538, 568);
        _help.Size = new Size(32, 34);
        _help.Text = "?";

        _close.Height = 34;
        _close.Location = new Point(856, 568);
        _close.Size = new Size(104, 34);
        _close.Text = "CLOSE";

        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(_status);
        Controls.Add(_list);
        Controls.Add(_pickFolder);
        Controls.Add(_addFiles);
        Controls.Add(_all);
        Controls.Add(_none);
        Controls.Add(_silent);
        Controls.Add(_defer);
        Controls.Add(_run);
        Controls.Add(_restart);
        Controls.Add(_help);
        Controls.Add(_close);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
