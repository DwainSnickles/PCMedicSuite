namespace PC_Medic;

partial class DownloadPanel
{
    private System.ComponentModel.IContainer components = null;

    private BufferedPanel _list;
    private PassBar _bar;
    private Label _kicker;
    private Label _title;
    private Label _count;
    private Label _status;
    private NeonButton _cancel;
    private NeonButton _keep;
    private NeonButton _remove;
    private NeonButton _shut;
    private NeonButton _help;
    private NeonButton _failJump;
    private System.Windows.Forms.Timer _sweep;
    private System.Windows.Forms.Timer _dismiss;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _list = new BufferedPanel();
        _bar = new PassBar();
        _kicker = new Label();
        _title = new Label();
        _count = new Label();
        _status = new Label();
        _cancel = new NeonButton();
        _keep = new NeonButton();
        _remove = new NeonButton();
        _shut = new NeonButton();
        _help = new NeonButton();
        _failJump = new NeonButton();
        _sweep = new System.Windows.Forms.Timer(components);
        _dismiss = new System.Windows.Forms.Timer(components);

        SuspendLayout();

        _list.AutoScroll = true;
        _list.BackColor = Theme.Surface;
        _list.Location = new Point(20, 96);
        _list.Size = new Size(600, 342);

        _bar.Height = 10;
        _bar.Location = new Point(24, 74);
        _bar.Size = new Size(592, 10);
        _bar.Visible = true;

        _kicker.AutoSize = true;
        _kicker.BackColor = Color.Transparent;
        _kicker.Font = Theme.Kicker;
        _kicker.ForeColor = Theme.Alpha(Theme.Accent, 215);
        _kicker.Location = new Point(24, 14);

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(24, 32);

        _count.AutoSize = false;
        _count.BackColor = Color.Transparent;
        _count.Font = Theme.Numeric;
        _count.ForeColor = Theme.TextDim;
        _count.Location = new Point(504, 34);
        _count.Size = new Size(100, 22);
        _count.TextAlign = ContentAlignment.MiddleRight;

        _status.AutoEllipsis = true;
        _status.AutoSize = false;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(24, 452);
        _status.Size = new Size(400, 30);
        _status.TextAlign = ContentAlignment.MiddleLeft;

        _cancel.Height = 32;
        _cancel.Location = new Point(512, 452);
        _cancel.Size = new Size(112, 32);
        _cancel.Text = "CANCEL";

        _keep.Height = 32;
        _keep.Location = new Point(396, 452);
        _keep.Size = new Size(96, 32);
        _keep.Text = "KEEP";
        _keep.Visible = false;

        _remove.Height = 32;
        _remove.Location = new Point(512, 452);
        _remove.Size = new Size(112, 32);
        _remove.Text = "REMOVE";
        _remove.Tone = Theme.Bad;
        _remove.Visible = false;

        _shut.Height = 28;
        _shut.Location = new Point(596, 12);
        _shut.Size = new Size(28, 28);
        _shut.Text = "✕";

        _help.AccessibleName = "Help";
        _help.Location = new Point(562, 12);
        _help.Size = new Size(28, 28);
        _help.Text = "?";

        _failJump.Height = 32;
        _failJump.Location = new Point(400, 452);
        _failJump.Size = new Size(104, 32);
        _failJump.Text = "FAILED";
        _failJump.Tone = Theme.Bad;
        _failJump.Visible = false;

        _sweep.Interval = 40;

        _dismiss.Interval = 900;

        Controls.Add(_list);
        Controls.Add(_bar);
        Controls.Add(_kicker);
        Controls.Add(_title);
        Controls.Add(_count);
        Controls.Add(_status);
        Controls.Add(_cancel);
        Controls.Add(_keep);
        Controls.Add(_remove);
        Controls.Add(_shut);
        Controls.Add(_help);
        Controls.Add(_failJump);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
