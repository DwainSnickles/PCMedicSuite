namespace PC_Medic;

partial class SelfUpdateForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _kicker;
    private Label _title;
    private Label _currentLabel;
    private Label _latestLabel;
    private Panel _currentChip;
    private Label _currentValue;
    private Panel _latestChip;
    private Label _latestValue;
    private Panel _notesWell;
    private Label _notes;
    private Label _status;
    private NeonButton _check;
    private NeonButton _install;
    private NeonButton _close;
    private ScanOverlay _loading;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _kicker = new Label();
        _title = new Label();
        _currentLabel = new Label();
        _latestLabel = new Label();
        _currentChip = new Panel();
        _currentValue = new Label();
        _latestChip = new Panel();
        _latestValue = new Label();
        _notesWell = new Panel();
        _notes = new Label();
        _status = new Label();
        _check = new NeonButton();
        _install = new NeonButton();
        _close = new NeonButton();
        _loading = new ScanOverlay();

        SuspendLayout();

        _kicker.AutoSize = true;
        _kicker.BackColor = Color.Transparent;
        _kicker.Font = Theme.Kicker;
        _kicker.ForeColor = Theme.Alpha(Theme.Accent, 215);
        _kicker.Location = new Point(30, 30);
        _kicker.Text = "PC MEDIC • SELF UPDATE";

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(30, 48);
        _title.Text = "SOFTWARE UPDATE";

        _currentLabel.AutoSize = true;
        _currentLabel.BackColor = Color.Transparent;
        _currentLabel.Font = Theme.Kicker;
        _currentLabel.ForeColor = Theme.TextDim;
        _currentLabel.Location = new Point(30, 88);
        _currentLabel.Text = "CURRENT BUILD";

        _latestLabel.AutoSize = true;
        _latestLabel.BackColor = Color.Transparent;
        _latestLabel.Font = Theme.Kicker;
        _latestLabel.ForeColor = Theme.TextDim;
        _latestLabel.Location = new Point(300, 88);
        _latestLabel.Text = "LATEST RELEASE";

        _currentChip.BackColor = Theme.Bg;
        _currentChip.Controls.Add(_currentValue);
        _currentChip.Location = new Point(30, 104);
        _currentChip.Size = new Size(240, 30);

        _currentValue.AutoSize = false;
        _currentValue.BackColor = Color.Transparent;
        _currentValue.Font = Theme.Mono;
        _currentValue.ForeColor = Theme.Text;
        _currentValue.Location = new Point(10, 5);
        _currentValue.Size = new Size(220, 20);

        _latestChip.BackColor = Theme.Bg;
        _latestChip.Controls.Add(_latestValue);
        _latestChip.Location = new Point(300, 104);
        _latestChip.Size = new Size(230, 30);

        _latestValue.AutoSize = false;
        _latestValue.BackColor = Color.Transparent;
        _latestValue.Font = Theme.Mono;
        _latestValue.ForeColor = Theme.Text;
        _latestValue.Location = new Point(12, 5);
        _latestValue.Size = new Size(200, 20);
        _latestValue.Text = "—";

        _notesWell.BackColor = Theme.Bg;
        _notesWell.Controls.Add(_notes);
        _notesWell.Location = new Point(24, 146);
        _notesWell.Size = new Size(512, 106);

        _notes.AutoSize = false;
        _notes.BackColor = Color.Transparent;
        _notes.Font = Theme.UiSmall;
        _notes.ForeColor = Theme.TextDim;
        _notes.Location = new Point(14, 10);
        _notes.Size = new Size(484, 86);

        _status.AutoSize = false;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(30, 258);
        _status.Size = new Size(500, 34);
        _status.Text = "Ready.";

        _check.Height = 32;
        _check.Location = new Point(30, 316);
        _check.Size = new Size(132, 32);
        _check.Text = "CHECK AGAIN";

        _install.Height = 32;
        _install.Location = new Point(238, 316);
        _install.Primary = true;
        _install.Size = new Size(186, 32);
        _install.Text = "DOWNLOAD & INSTALL";

        _close.Height = 32;
        _close.Location = new Point(434, 316);
        _close.Size = new Size(96, 32);
        _close.Text = "CLOSE";

        _loading.Bounds = new Rectangle(12, 88, 536, 164);
        _loading.Circular = true;
        _loading.Visible = false;

        Controls.Add(_kicker);
        Controls.Add(_title);
        Controls.Add(_currentLabel);
        Controls.Add(_latestLabel);
        Controls.Add(_currentChip);
        Controls.Add(_latestChip);
        Controls.Add(_notesWell);
        Controls.Add(_status);
        Controls.Add(_check);
        Controls.Add(_install);
        Controls.Add(_close);
        Controls.Add(_loading);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
