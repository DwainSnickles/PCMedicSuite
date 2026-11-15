namespace PC_Medic;

partial class JunkCleanupForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _title;
    private Label _status;
    private CheckedListBox _list;
    private NeonButton _clean;
    private NeonButton _help;
    private NeonButton _close;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _title = new Label();
        _status = new Label();
        _list = new CheckedListBox();
        _clean = new NeonButton();
        _help = new NeonButton();
        _close = new NeonButton();

        SuspendLayout();

        _title.AutoSize = true;
        _title.Font = Theme.Kicker;
        _title.ForeColor = Theme.TextDim;
        _title.Location = new Point(16, 10);
        _title.Text = "FIND & REMOVE JUNK";

        _status.AutoSize = true;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(16, 30);

        _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _list.BackColor = Theme.Surface;
        _list.BorderStyle = BorderStyle.None;
        _list.CheckOnClick = true;
        _list.Font = Theme.MonoSmall;
        _list.ForeColor = Theme.Text;
        _list.IntegralHeight = false;
        _list.Location = new Point(12, 52);
        _list.Size = new Size(616, 326);

        _clean.Height = 30;
        _clean.Location = new Point(12, 390);
        _clean.Primary = true;
        _clean.Text = "CLEAN SELECTED";
        _clean.Width = 132;

        _help.AccessibleName = "Help";
        _help.Height = 30;
        _help.Location = new Point(508, 390);
        _help.Size = new Size(30, 30);
        _help.Text = "?";

        _close.Height = 30;
        _close.Location = new Point(544, 390);
        _close.Text = "CLOSE";
        _close.Width = 84;

        Controls.Add(_title);
        Controls.Add(_status);
        Controls.Add(_list);
        Controls.Add(_clean);
        Controls.Add(_help);
        Controls.Add(_close);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
