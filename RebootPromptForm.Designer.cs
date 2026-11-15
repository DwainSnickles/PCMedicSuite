namespace PC_Medic;

partial class RebootPromptForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _body;
    private FlowLayoutPanel _buttonsPanel;
    private NeonButton _btnNow;
    private NeonButton _btnLater;
    private NeonButton _btnNever;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _body = new Label();
        _buttonsPanel = new FlowLayoutPanel();
        _btnNow = new NeonButton();
        _btnLater = new NeonButton();
        _btnNever = new NeonButton();

        SuspendLayout();

        Text = "Restart required";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        // 3 buttons x (158 + 10 margin) + 28 padding = 532 — anything narrower clips NEVER ASK.
        ClientSize = new Size(540, 208);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;

        _body.AutoSize = false;
        _body.BackColor = Theme.Bg;
        _body.Dock = DockStyle.Top;
        _body.ForeColor = Theme.Text;
        _body.Height = 100;
        _body.Padding = new Padding(20, 16, 20, 0);

        _buttonsPanel.BackColor = Theme.Bg;
        _buttonsPanel.Controls.Add(_btnNow);
        _buttonsPanel.Controls.Add(_btnLater);
        _buttonsPanel.Controls.Add(_btnNever);
        _buttonsPanel.Dock = DockStyle.Bottom;
        _buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        _buttonsPanel.Height = 66;
        _buttonsPanel.Padding = new Padding(14, 10, 14, 12);
        _buttonsPanel.WrapContents = false;

        _btnNow.Height = 34;
        _btnNow.Margin = new Padding(0, 0, 10, 0);
        _btnNow.Primary = true;
        _btnNow.Text = "RESTART NOW";
        _btnNow.Width = 158;

        _btnLater.Height = 34;
        _btnLater.Margin = new Padding(0, 0, 10, 0);
        _btnLater.Text = "RESTART LATER";
        _btnLater.Width = 158;

        _btnNever.Height = 34;
        _btnNever.Margin = new Padding(0, 0, 10, 0);
        _btnNever.Text = "NEVER ASK";
        _btnNever.Width = 158;

        Controls.Add(_body);
        Controls.Add(_buttonsPanel);

        ResumeLayout(false);
    }

    #endregion
}
