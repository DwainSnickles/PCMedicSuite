namespace PC_Medic;

partial class RecoveryStickForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _title;
    private Label _subtitle;
    private Label _hint;
    private Label _warn;
    private Label _status;
    private NeonButton _drivePick;
    private NeonButton _action;
    private NeonButton _cancel;
    private TextBox _log;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        ClientSize = new Size(640, 470);
        Text = "PC Medic — Recovery USB";
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        KeyPreview = true;

        _title = new Label();
        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = Theme.Kicker;
        _title.ForeColor = Theme.Alpha(Theme.Accent, 215);
        _title.Location = new Point(28, 24);
        _title.Text = "PC MEDIC • RECOVERY USB";

        _subtitle = new Label();
        _subtitle.AutoSize = true;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _subtitle.ForeColor = Theme.Text;
        _subtitle.Location = new Point(28, 42);
        _subtitle.Text = "BOOTABLE RECOVERY STICK";

        _hint = new Label();
        _hint.AutoSize = true;
        _hint.BackColor = Color.Transparent;
        _hint.Font = Theme.UiSmall;
        _hint.ForeColor = Theme.TextDim;
        _hint.Location = new Point(28, 74);
        _hint.Text = "Formats a USB stick FAT32 and writes the live Windows RE image so it boots straight into recovery.";

        _warn = new Label();
        _warn.AutoSize = true;
        _warn.BackColor = Color.Transparent;
        _warn.Font = Theme.UiSmall;
        _warn.ForeColor = Theme.Bad;
        _warn.Location = new Point(28, 150);
        _warn.Text = "FORMATTING ERASES EVERYTHING ON THE SELECTED DRIVE.";

        _drivePick = new NeonButton();
        _drivePick.Location = new Point(28, 108);
        _drivePick.Size = new Size(240, 32);
        _drivePick.Text = "USB: —";

        _log = new TextBox();
        _log.BackColor = Theme.Surface;
        _log.BorderStyle = BorderStyle.None;
        _log.Font = Theme.MonoSmall;
        _log.ForeColor = Theme.TextDim;
        _log.Location = new Point(20, 178);
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Size = new Size(600, 190);

        _status = new Label();
        _status.AutoSize = false;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(28, 376);
        _status.Size = new Size(430, 28);
        _status.Text = "READY";
        _status.TextAlign = ContentAlignment.MiddleLeft;

        _action = new NeonButton();
        _action.Location = new Point(344, 404);
        _action.Size = new Size(150, 36);
        _action.Text = "BUILD STICK";

        _cancel = new NeonButton();
        _cancel.Location = new Point(502, 408);
        _cancel.Size = new Size(110, 32);
        _cancel.Text = "CANCEL";

        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(_hint);
        Controls.Add(_warn);
        Controls.Add(_drivePick);
        Controls.Add(_log);
        Controls.Add(_status);
        Controls.Add(_action);
        Controls.Add(_cancel);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }
}
