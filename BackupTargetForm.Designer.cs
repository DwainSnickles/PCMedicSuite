namespace PC_Medic;

partial class BackupTargetForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _title;
    private Label _subtitle;
    private NeonButton _sourcePick;
    private Label _sourceDetail;
    private NeonButton _targetPick;
    private Label _targetDetail;
    private NeonButton _browse;
    private Label _pathLabel;
    private NeonButton _compressionPick;
    private NeonButton _stickPick;
    private NeonButton _recoveryDrive;
    private Label _stickNote;
    private Label _verdict;
    private NeonButton _start;
    private NeonButton _cancel;
    private NeonButton _help;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _title = new Label();
        _subtitle = new Label();
        _sourcePick = new NeonButton();
        _sourceDetail = new Label();
        _targetPick = new NeonButton();
        _targetDetail = new Label();
        _browse = new NeonButton();
        _pathLabel = new Label();
        _compressionPick = new NeonButton();
        _stickPick = new NeonButton();
        _recoveryDrive = new NeonButton();
        _stickNote = new Label();
        _verdict = new Label();
        _start = new NeonButton();
        _cancel = new NeonButton();
        _help = new NeonButton();
        _tips = new ToolTip(components);

        SuspendLayout();

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(18, 14);
        _title.Text = "BACK UP TO DISK";

        _subtitle.AutoSize = false;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = Theme.UiSmall;
        _subtitle.ForeColor = Theme.TextDim;
        _subtitle.Location = new Point(20, 44);
        _subtitle.Size = new Size(680, 38);
        _subtitle.Text = "Write the image to a disk that can hold it — an external drive, a second internal disk — and put a pointer on the small USB stick so a recovery boot knows where to find it.";

        _sourcePick.Height = 32;
        _sourcePick.Location = new Point(18, 96);
        _sourcePick.Size = new Size(684, 32);
        _sourcePick.Text = "SOURCE: —";

        _sourceDetail.AutoEllipsis = true;
        _sourceDetail.AutoSize = false;
        _sourceDetail.BackColor = Color.Transparent;
        _sourceDetail.Font = Theme.MonoSmall;
        _sourceDetail.ForeColor = Theme.TextDim;
        _sourceDetail.Location = new Point(20, 132);
        _sourceDetail.Size = new Size(680, 18);

        _targetPick.Height = 32;
        _targetPick.Location = new Point(18, 168);
        _targetPick.Size = new Size(562, 32);
        _targetPick.Text = "DESTINATION: —";

        _targetDetail.AutoEllipsis = true;
        _targetDetail.AutoSize = false;
        _targetDetail.BackColor = Color.Transparent;
        _targetDetail.Font = Theme.MonoSmall;
        _targetDetail.ForeColor = Theme.TextDim;
        _targetDetail.Location = new Point(20, 204);
        _targetDetail.Size = new Size(680, 18);

        _browse.Height = 30;
        _browse.Location = new Point(586, 169);
        _browse.Text = "BROWSE…";
        _browse.Width = 116;

        _pathLabel.AutoEllipsis = true;
        _pathLabel.AutoSize = false;
        _pathLabel.BackColor = Color.Transparent;
        _pathLabel.Font = Theme.MonoSmall;
        _pathLabel.ForeColor = Theme.TextDim;
        _pathLabel.Location = new Point(20, 226);
        _pathLabel.Size = new Size(680, 18);

        _compressionPick.Height = 32;
        _compressionPick.Location = new Point(18, 262);
        _compressionPick.Size = new Size(170, 32);

        _stickPick.Height = 32;
        _stickPick.Location = new Point(198, 262);
        _stickPick.Size = new Size(304, 32);
        _stickPick.Text = "RECOVERY USB: NONE";

        _recoveryDrive.Height = 32;
        _recoveryDrive.Location = new Point(522, 262);
        _recoveryDrive.Size = new Size(180, 32);
        _recoveryDrive.Text = "RECOVERY DRIVE…";
        _recoveryDrive.Width = 180;

        _stickNote.AutoSize = false;
        _stickNote.BackColor = Color.Transparent;
        _stickNote.Font = Theme.UiSmall;
        _stickNote.ForeColor = Theme.TextDim;
        _stickNote.Location = new Point(20, 302);
        _stickNote.Size = new Size(680, 52);
        _stickNote.TextAlign = ContentAlignment.TopLeft;

        _verdict.AutoEllipsis = true;
        _verdict.AutoSize = false;
        _verdict.BackColor = Color.Transparent;
        _verdict.Font = Theme.MonoSmall;
        _verdict.ForeColor = Theme.TextDim;
        _verdict.Location = new Point(20, 362);
        _verdict.Size = new Size(680, 18);

        _start.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        _start.Height = 34;
        _start.Location = new Point(18, 418);
        _start.Primary = true;
        _start.Size = new Size(178, 34);
        _start.Text = "START BACKUP";

        _cancel.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        _cancel.Height = 34;
        _cancel.Location = new Point(598, 418);
        _cancel.Size = new Size(104, 34);
        _cancel.Text = "CANCEL";

        _help.AccessibleName = "Help";
        _help.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        _help.Location = new Point(486, 418);
        _help.Size = new Size(32, 34);
        _help.Text = "?";

        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(_sourcePick);
        Controls.Add(_sourceDetail);
        Controls.Add(_targetPick);
        Controls.Add(_targetDetail);
        Controls.Add(_browse);
        Controls.Add(_pathLabel);
        Controls.Add(_compressionPick);
        Controls.Add(_stickPick);
        Controls.Add(_recoveryDrive);
        Controls.Add(_stickNote);
        Controls.Add(_verdict);
        Controls.Add(_start);
        Controls.Add(_cancel);
        Controls.Add(_help);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
