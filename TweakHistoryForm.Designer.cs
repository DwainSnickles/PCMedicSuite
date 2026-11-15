namespace PC_Medic;

partial class TweakHistoryForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _kicker;
    private Label _title;
    private Label _subtitle;
    private Panel _list;
    private Label _empty;
    private Label _status;
    private NeonButton _undo;
    private NeonButton _close;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _kicker = new Label();
        _title = new Label();
        _subtitle = new Label();
        _list = new Panel();
        _empty = new Label();
        _status = new Label();
        _undo = new NeonButton();
        _close = new NeonButton();
        _tips = new ToolTip(components);

        SuspendLayout();

        _kicker.AutoSize = true;
        _kicker.BackColor = Color.Transparent;
        _kicker.Font = Theme.Kicker;
        _kicker.ForeColor = Theme.Alpha(Theme.Accent, 215);
        _kicker.Location = new Point(30, 28);
        _kicker.Text = "TWEAK JOURNAL";

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(30, 46);
        _title.Text = "UNDO HISTORY";

        _subtitle.AutoSize = true;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = Theme.UiSmall;
        _subtitle.ForeColor = Theme.TextDim;
        _subtitle.Location = new Point(30, 78);
        _subtitle.Text = "Newest first — every entry keeps the exact values the switch replaced.";

        _list.AutoScroll = true;
        _list.BackColor = Theme.Surface;
        _list.Controls.Add(_empty);
        _list.Location = new Point(24, 108);
        _list.Margin = new Padding(0);
        _list.Size = new Size(632, 334);

        _empty.AutoSize = true;
        _empty.BackColor = Color.Transparent;
        _empty.Font = Theme.MonoSmall;
        _empty.ForeColor = Theme.TextDim;
        _empty.Location = new Point(12, 12);
        _empty.Text = "No tweak changes recorded yet. Flip a switch and it lands here.";
        _empty.Visible = false;

        _status.AutoEllipsis = true;
        _status.AutoSize = false;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(30, 456);
        _status.Size = new Size(374, 32);
        _status.TextAlign = ContentAlignment.MiddleLeft;

        _undo.Height = 32;
        _undo.Location = new Point(404, 456);
        _undo.Primary = true;
        _undo.Size = new Size(140, 32);
        _undo.Text = "UNDO LAST";

        _close.Height = 32;
        _close.Location = new Point(554, 456);
        _close.Size = new Size(96, 32);
        _close.Text = "CLOSE";

        Controls.Add(_kicker);
        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(_list);
        Controls.Add(_status);
        Controls.Add(_undo);
        Controls.Add(_close);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
