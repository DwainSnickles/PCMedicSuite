namespace PC_Medic;

partial class RigSetupForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _kicker;
    private Label _title;
    private Label _subtitle;
    private BufferedPanel _list;
    private Label _status;
    private NeonButton _action;
    private System.Windows.Forms.Timer _sweep;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _kicker = new Label();
        _title = new Label();
        _subtitle = new Label();
        _list = new BufferedPanel();
        _status = new Label();
        _action = new NeonButton();
        _sweep = new System.Windows.Forms.Timer(components);

        SuspendLayout();

        _kicker.AutoSize = true;
        _kicker.BackColor = Color.Transparent;
        _kicker.Font = Theme.Kicker;
        _kicker.ForeColor = Theme.Alpha(Theme.Accent, 215);
        _kicker.Location = new Point(28, 26);
        _kicker.Text = "PC MEDIC • BENCHMARK RIGS";

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(28, 44);
        _title.Text = "INSTALL MISSING RIGS";

        _subtitle.AutoSize = true;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = Theme.UiSmall;
        _subtitle.ForeColor = Theme.TextDim;
        _subtitle.Location = new Point(28, 76);
        _subtitle.Text = "winget downloads each vendor package and verifies its hash — one bar per rig.";

        _list.AutoScroll = true;
        _list.BackColor = Theme.Surface;
        _list.Location = new Point(20, 100);
        _list.Margin = new Padding(0);
        _list.Size = new Size(640, 100);

        _status.AutoEllipsis = false;
        _status.AutoSize = false;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(28, 208);
        _status.Size = new Size(480, 30);
        _status.Text = "READY TO INSTALL";
        _status.TextAlign = ContentAlignment.MiddleLeft;

        _action.Height = 32;
        _action.Size = new Size(116, 32);
        _action.Text = "CANCEL";

        _sweep.Interval = 40;

        Controls.Add(_kicker);
        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(_list);
        Controls.Add(_status);
        Controls.Add(_action);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
