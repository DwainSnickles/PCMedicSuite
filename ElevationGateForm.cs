using System.Drawing.Drawing2D;

namespace PC_Medic;

/// <summary>First screen: PC MEDIC asks for administrator rights and lets the user continue read-only instead.</summary>
internal sealed partial class ElevationGateForm : Form
{
    private const int WmNcHitTest = 0x0084;
    private const int HtCaption = 2;
    private const int HtClient = 1;


    public ElevationGateForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(660, 318);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        KeyPreview = true;
        Text = "Administrator Access";

        InitializeComponent();

        _elevate.Click += (_, _) =>
        {
            RestartRequested = true;
            DialogResult = DialogResult.OK;
            Close();
        };

        _limited.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
    }

    /// <summary>True when the user chose to relaunch through UAC instead of continuing limited.</summary>
    public bool RestartRequested { get; private set; }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode != Keys.Escape) return;

        DialogResult = DialogResult.Cancel;
        Close();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WmNcHitTest && (int)m.Result == HtClient) m.Result = HtCaption;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Theme.PaintSurface(g, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 12f, Theme.Surface, Theme.Alpha(Theme.Accent, 170), Theme.Accent);
    }
}
