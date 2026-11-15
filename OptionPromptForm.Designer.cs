namespace PC_Medic;

partial class OptionPromptForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _body;
    private FlowLayoutPanel _buttonsPanel;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _body = new Label();
        _buttonsPanel = new FlowLayoutPanel();

        SuspendLayout();

        _body.AutoSize = false;
        _body.BackColor = Theme.Bg;
        _body.Dock = DockStyle.Top;
        _body.ForeColor = Theme.Text;
        _body.Height = 128;
        _body.Padding = new Padding(20, 16, 20, 0);

        _buttonsPanel.BackColor = Theme.Bg;
        _buttonsPanel.Dock = DockStyle.Bottom;
        _buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
        _buttonsPanel.Height = 66;
        _buttonsPanel.Padding = new Padding(14, 10, 14, 12);
        _buttonsPanel.WrapContents = false;

        Controls.Add(_body);
        Controls.Add(_buttonsPanel);

        ResumeLayout(false);
    }

    #endregion
}
