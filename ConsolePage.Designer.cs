namespace PC_Medic;

partial class ConsolePage
{
    private System.ComponentModel.IContainer components = null;

    private TableLayoutPanel _root;
    private Card _inputCard;
    private Panel _quickBar;
    private Card _outputCard;
    private TextBox _input;
    private TextBox _output;
    private NeonButton _run;
    private NeonButton _admin;
    private NeonButton _cmdMode;
    private NeonButton _psMode;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel();
        _inputCard = new Card();
        _input = new TextBox();
        _run = new NeonButton();
        _admin = new NeonButton();
        _cmdMode = new NeonButton();
        _psMode = new NeonButton();
        _quickBar = new Panel();
        _outputCard = new Card();
        _output = new TextBox();

        _root.SuspendLayout();
        _inputCard.SuspendLayout();
        _outputCard.SuspendLayout();
        SuspendLayout();

        _root.BackColor = Theme.Bg;
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.Controls.Add(_inputCard, 0, 0);
        _root.Controls.Add(_quickBar, 0, 1);
        _root.Controls.Add(_outputCard, 0, 2);
        _root.Dock = DockStyle.Fill;
        _root.Margin = new Padding(0);
        _root.RowCount = 3;
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _inputCard.Controls.Add(_input);
        _inputCard.Controls.Add(_run);
        _inputCard.Controls.Add(_admin);
        _inputCard.Controls.Add(_cmdMode);
        _inputCard.Controls.Add(_psMode);
        _inputCard.Dock = DockStyle.Fill;
        _inputCard.Header = "WINDOWS COMMAND";
        _inputCard.Kicker = "AD-HOC";
        _inputCard.Margin = new Padding(0, 0, 0, 10);

        _input.BackColor = Theme.Surface2;
        _input.BorderStyle = BorderStyle.None;
        _input.Font = Theme.Mono;
        _input.ForeColor = Theme.Text;
        _input.Location = new Point(20, 58);
        _input.Size = new Size(400, 24);

        _run.Height = 32;
        _run.Location = new Point(600, 52);
        _run.Primary = true;
        _run.Size = new Size(90, 32);
        _run.Text = "RUN";

        _admin.Height = 32;
        _admin.Location = new Point(718, 52);
        _admin.Size = new Size(150, 32);
        _admin.Text = "ELEVATED CMD";

        _cmdMode.Height = 26;
        _cmdMode.Location = new Point(664, 12);
        _cmdMode.Size = new Size(62, 26);
        _cmdMode.Text = "CMD";

        _psMode.Height = 26;
        _psMode.Location = new Point(734, 12);
        _psMode.Size = new Size(116, 26);
        _psMode.Text = "POWERSHELL";

        _quickBar.BackColor = Theme.Bg;
        _quickBar.Dock = DockStyle.Fill;
        _quickBar.Margin = new Padding(0, 0, 0, 10);

        _outputCard.Controls.Add(_output);
        _outputCard.Dock = DockStyle.Fill;
        _outputCard.Header = "TRANSCRIPT";
        _outputCard.Kicker = "STDOUT / STDERR";

        _output.BackColor = Theme.Surface;
        _output.BorderStyle = BorderStyle.None;
        _output.Dock = DockStyle.Fill;
        _output.Font = Theme.MonoSmall;
        _output.ForeColor = Theme.Text;
        _output.Location = new Point(18, 54);
        _output.Multiline = true;
        _output.ReadOnly = true;
        _output.ScrollBars = ScrollBars.Both;
        _output.Size = new Size(600, 300);
        _output.TabIndex = 0;
        _output.Text = "PC MEDIC // COMMAND SHELL\r\nPick CMD or POWERSHELL for the runner; commands use your current token.\r\nELEVATED CMD / ELEVATED PS opens a prompt with administrator rights.\r\n\r\n";
        _output.WordWrap = false;

        Controls.Add(_root);

        _inputCard.ResumeLayout(false);
        _inputCard.PerformLayout();
        _outputCard.ResumeLayout(false);
        _outputCard.PerformLayout();
        _root.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
