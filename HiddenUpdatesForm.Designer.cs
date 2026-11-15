namespace PC_Medic;

partial class HiddenUpdatesForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _title;
    private Label _status;
    private ListBox _list;
    private NeonButton _unhide;
    private NeonButton _unhideAll;
    private NeonButton _openFile;
    private NeonButton _help;
    private NeonButton _close;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _title = new Label();
        _status = new Label();
        _list = new ListBox();
        _unhide = new NeonButton();
        _unhideAll = new NeonButton();
        _openFile = new NeonButton();
        _help = new NeonButton();
        _close = new NeonButton();

        SuspendLayout();

        _title.AutoSize = true;
        _title.Font = Theme.Kicker;
        _title.ForeColor = Theme.TextDim;
        _title.Location = new Point(16, 10);
        _title.Text = "HIDDEN UPDATES";

        _status.AutoSize = true;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(16, 30);

        _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _list.BackColor = Theme.Surface;
        _list.BorderStyle = BorderStyle.None;
        _list.Font = Theme.MonoSmall;
        _list.ForeColor = Theme.Text;
        _list.IntegralHeight = false;
        _list.Location = new Point(12, 52);
        _list.SelectionMode = SelectionMode.MultiExtended;
        _list.Size = new Size(596, 296);

        _unhide.Height = 30;
        _unhide.Location = new Point(12, 360);
        _unhide.Text = "UNHIDE SELECTED";
        _unhide.Width = 132;

        _unhideAll.Height = 30;
        _unhideAll.Location = new Point(152, 360);
        _unhideAll.Text = "UNHIDE ALL";
        _unhideAll.Width = 104;

        _openFile.Height = 30;
        _openFile.Location = new Point(264, 360);
        _openFile.Text = "OPEN FILE";
        _openFile.Width = 96;

        _help.AccessibleName = "Help";
        _help.Height = 30;
        _help.Location = new Point(488, 360);
        _help.Size = new Size(30, 30);
        _help.Text = "?";

        _close.Height = 30;
        _close.Location = new Point(524, 360);
        _close.Text = "CLOSE";
        _close.Width = 84;

        Controls.Add(_title);
        Controls.Add(_status);
        Controls.Add(_list);
        Controls.Add(_unhide);
        Controls.Add(_unhideAll);
        Controls.Add(_openFile);
        Controls.Add(_help);
        Controls.Add(_close);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
