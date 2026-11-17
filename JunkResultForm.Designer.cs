namespace PC_Medic;

partial class JunkResultForm
{
    private System.ComponentModel.IContainer components = null;

    private Label _title;
    private Label _subtitle;
    private StatTile _freedTile;
    private StatTile _removedTile;
    private StatTile _skippedTile;
    private StatTile _rebootTile;
    private Label _section;
    private InfoList _rows;
    private Label _note;
    private NeonButton _help;
    private NeonButton _close;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _title = new Label();
        _subtitle = new Label();
        _freedTile = new StatTile();
        _removedTile = new StatTile();
        _skippedTile = new StatTile();
        _rebootTile = new StatTile();
        _section = new Label();
        _rows = new InfoList();
        _note = new Label();
        _help = new NeonButton();
        _close = new NeonButton();

        SuspendLayout();

        _title.AutoSize = true;
        _title.BackColor = Color.Transparent;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.Location = new Point(20, 14);
        _title.Text = "CLEANUP RESULTS";

        _subtitle.AutoEllipsis = true;
        _subtitle.AutoSize = false;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = Theme.UiSmall;
        _subtitle.ForeColor = Theme.TextDim;
        _subtitle.Location = new Point(22, 46);
        _subtitle.Size = new Size(596, 20);
        _subtitle.Text = "One line per category: what came off this pass, and what Windows would not let go.";

        _freedTile.Caption = "FREED";
        _freedTile.Location = new Point(18, 76);
        _freedTile.Progress = 1D;
        _freedTile.Size = new Size(143, 120);
        _freedTile.Tone = Theme.Good;

        _removedTile.Caption = "REMOVED";
        _removedTile.Location = new Point(171, 76);
        _removedTile.Progress = 1D;
        _removedTile.Size = new Size(143, 120);
        _removedTile.Tone = Theme.Accent;

        _skippedTile.Caption = "HELD OPEN";
        _skippedTile.Location = new Point(324, 76);
        _skippedTile.Progress = 1D;
        _skippedTile.Size = new Size(143, 120);
        _skippedTile.Tone = Theme.Warn;

        _rebootTile.Caption = "ON REBOOT";
        _rebootTile.Location = new Point(477, 76);
        _rebootTile.Progress = 1D;
        _rebootTile.Size = new Size(143, 120);
        _rebootTile.Tone = Theme.Violet;

        _section.AutoSize = true;
        _section.BackColor = Color.Transparent;
        _section.Font = Theme.Kicker;
        _section.ForeColor = Theme.TextDim;
        _section.Location = new Point(20, 210);
        _section.Text = "BY CATEGORY";

        _rows.BackColor = Color.Transparent;
        _rows.Location = new Point(18, 230);
        _rows.RowHeight = 26;
        _rows.Size = new Size(604, 208);

        _note.AutoEllipsis = true;
        _note.AutoSize = false;
        _note.BackColor = Color.Transparent;
        _note.Font = Theme.MonoSmall;
        _note.ForeColor = Theme.TextFaint;
        _note.Location = new Point(20, 450);
        _note.Size = new Size(466, 36);

        _help.AccessibleName = "Help";
        _help.Location = new Point(504, 456);
        _help.Size = new Size(30, 30);
        _help.Text = "?";

        _close.Location = new Point(540, 456);
        _close.Size = new Size(84, 30);
        _close.Text = "CLOSE";

        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(_freedTile);
        Controls.Add(_removedTile);
        Controls.Add(_skippedTile);
        Controls.Add(_rebootTile);
        Controls.Add(_section);
        Controls.Add(_rows);
        Controls.Add(_note);
        Controls.Add(_help);
        Controls.Add(_close);

        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
