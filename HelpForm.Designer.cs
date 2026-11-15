namespace PC_Medic;

partial class HelpForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _title;
    private Label _subtitle;
    private ListBox _topics;
    private BufferedPanel _body;
    private Label _hint;
    private NeonButton _close;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _title = new Label();
        _subtitle = new Label();
        _topics = new ListBox();
        _body = new BufferedPanel();
        _hint = new Label();
        _close = new NeonButton();

        SuspendLayout();

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(20, 14);
        _title.Text = "HELP";

        _subtitle.AutoSize = false;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = Theme.UiSmall;
        _subtitle.ForeColor = Theme.TextDim;
        _subtitle.Location = new Point(22, 46);
        _subtitle.Size = new Size(900, 20);
        _subtitle.Text = "One topic per page and dialog — what it reads, what it writes, and what it will not do. The ? in the header opens the page you are on; F1 does the same.";

        _topics.BackColor = Theme.Surface;
        _topics.BorderStyle = BorderStyle.None;
        _topics.DrawMode = DrawMode.OwnerDrawFixed;
        _topics.Font = Theme.Ui;
        _topics.ForeColor = Theme.Text;
        _topics.IntegralHeight = false;
        _topics.ItemHeight = 42;
        _topics.Location = new Point(18, 82);
        _topics.Size = new Size(248, 612);

        _body.AutoScroll = true;
        _body.BackColor = Theme.Bg;
        _body.Location = new Point(282, 82);
        _body.Size = new Size(820, 612);

        _hint.AutoEllipsis = true;
        _hint.AutoSize = false;
        _hint.BackColor = Color.Transparent;
        _hint.Font = Theme.MonoSmall;
        _hint.ForeColor = Theme.TextFaint;
        _hint.Location = new Point(20, 716);
        _hint.Size = new Size(768, 22);

        _close.Height = 34;
        _close.Location = new Point(998, 706);
        _close.Text = "CLOSE";
        _close.Width = 104;

        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(_topics);
        Controls.Add(_body);
        Controls.Add(_hint);
        Controls.Add(_close);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
