namespace PC_Medic;

partial class ElevationGateForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _kicker;
    private Label _title;
    private Label _body;
    private Label _warn;
    private NeonButton _elevate;
    private NeonButton _limited;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _kicker = new Label();
        _title = new Label();
        _body = new Label();
        _warn = new Label();
        _elevate = new NeonButton();
        _limited = new NeonButton();

        SuspendLayout();

        _kicker.AutoSize = true;
        _kicker.BackColor = Color.Transparent;
        _kicker.Font = Theme.Kicker;
        _kicker.ForeColor = Theme.Alpha(Theme.Accent, 215);
        _kicker.Location = new Point(30, 26);
        _kicker.Text = "ADMINISTRATOR ACCESS";

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 16f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(30, 44);
        _title.Text = "RUN WITH FULL CONTROL";

        _body.AutoSize = true;
        _body.BackColor = Color.Transparent;
        _body.Font = Theme.UiSmall;
        _body.ForeColor = Theme.TextDim;
        _body.Location = new Point(30, 84);
        _body.MaximumSize = new Size(600, 0);
        _body.Text = "PC MEDIC changes machine-wide settings: SFC and DISM repairs, restore points, power plans, driver scans and Disk Defrag. Windows only allows those with administrator rights.";

        _warn.AutoSize = true;
        _warn.BackColor = Color.Transparent;
        _warn.Font = Theme.UiSmall;
        _warn.ForeColor = Theme.Warn;
        _warn.Location = new Point(30, 158);
        _warn.MaximumSize = new Size(600, 0);
        _warn.Text = "Continuing limited keeps the dashboards live, but repairs, machine-wide tweaks and the Disk Defrag cluster map will not work.";

        _limited.Height = 34;
        _limited.Location = new Point(460, 254);
        _limited.Primary = false;
        _limited.Size = new Size(170, 34);
        _limited.Text = "CONTINUE LIMITED";

        _elevate.Height = 34;
        _elevate.Location = new Point(268, 254);
        _elevate.Primary = true;
        _elevate.Size = new Size(180, 34);
        _elevate.Text = "RESTART AS ADMIN";

        Controls.Add(_kicker);
        Controls.Add(_title);
        Controls.Add(_body);
        Controls.Add(_warn);
        Controls.Add(_limited);
        Controls.Add(_elevate);

        ResumeLayout(false);
    }

    #endregion
}
