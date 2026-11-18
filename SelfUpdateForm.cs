using System.Drawing.Drawing2D;

namespace PC_Medic;

/// <summary>Small dark dialog for checking, downloading and installing a new PC Medic build.</summary>
internal sealed partial class SelfUpdateForm : Form
{
    private const int WmNcHitTest = 0x0084;
    private const int HtCaption = 2;
    private const int HtClient = 1;


    private SelfUpdateInfo? _info;
    private bool _busy;

    public SelfUpdateForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 372);
        BackColor = Theme.Bg;
        Font = Theme.Ui;
        ForeColor = Theme.Text;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        KeyPreview = true;

        InitializeComponent();

        AttachWells();
        _loading.BringToFront();

        _install.Enabled = false;

        _check.Click += async (_, _) => await CheckAsync();
        _install.Click += async (_, _) => await InstallAsync();
        _close.Click += (_, _) => Close();

        _notes.Text = "Release notes appear here once a build is published.";
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _ = CheckAsync();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Escape && !_busy) Close();
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

        // The window edge gets the same heavy accent outline as the wells inside it, so the dialog reads
        // as a form on the desktop instead of a floating block of text.
        var frame = new RectangleF(1f, 1f, Width - 2f, Height - 2f);
        Theme.PaintSurface(g, frame, 12f, Theme.Surface, Theme.Alpha(Theme.Accent, 215), Theme.Accent);

        using GraphicsPath outline = RoundPath(frame, 12f);
        using var edge = new Pen(Theme.Alpha(Theme.Accent, 215), 2f);
        g.DrawPath(edge, outline);
    }

    /// <summary>
    /// A filled, outlined well for one block of the dialog. Without the edge these blocks sit on the same
    /// near-black as the form and their boundaries disappear.
    /// </summary>
    /// <summary>Gives the three wells their rounded fill and heavy accent edge.</summary>
    private void AttachWells()
    {
        WireWell(_currentChip, 6f);
        WireWell(_latestChip, 6f);
        WireWell(_notesWell, 8f);
    }

    private static void WireWell(Panel well, float radius)
    {
        well.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using GraphicsPath path = RoundPath(new RectangleF(1f, 1f, well.Width - 2f, well.Height - 2f), radius);
            using (var fill = new SolidBrush(Theme.Surface2)) e.Graphics.FillPath(fill, path);
            using var edge = new Pen(Theme.Alpha(Theme.Accent, 215), 2f);
            e.Graphics.DrawPath(edge, path);
        };
    }

    /// <summary>Rounded rectangle, so the well's fill and its heavy border follow the same outline.</summary>
    private static GraphicsPath RoundPath(RectangleF rect, float radius)
    {
        float diameter = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private async Task CheckAsync()
    {
        if (_busy) return;

        _busy = true;
        _check.Enabled = false;
        _install.Enabled = false;
        SetStatus($"Checking {UpdateService.FeedHost}\u2026", Theme.TextDim);
        using IDisposable busy = Busy.Enter(this);
        using IDisposable plate = ShowPlate();

        SelfUpdateInfo info = await UpdateService.CheckAsync();
        if (IsDisposed) return;

        _info = info;
        _busy = false;
        _check.Enabled = true;

        _currentValue.Text = info.CurrentVersion;
        _latestValue.Text = info.LatestVersion.Length == 0 ? "\u2014" : info.LatestVersion;
        _install.Enabled = info.Available;

        _notes.Text = string.IsNullOrWhiteSpace(info.Notes)
            ? "No release notes for this build."
            : Theme.Truncate(info.Notes.Replace("\r", " ").Replace("\n", "  "), 260);

        SetStatus(info.Message, info.Available ? Theme.Warn : info.Checked ? Theme.Good : Theme.Bad);
        ActivityLog.Write(info.Available ? LogLevel.Info : LogLevel.Success, $"Self-update: {info.Message}");
    }

    private async Task InstallAsync()
    {
        if (_busy || _info is not { Available: true } info) return;

        _busy = true;
        _check.Enabled = false;
        _install.Enabled = false;
        SetStatus("Downloading the package\u2026", Theme.TextDim);
        using IDisposable busy = Busy.Enter(this);
        using IDisposable plate = ShowPlate();

        var progress = new Progress<string>(text => SetStatus($"Downloading {text}", Theme.TextDim));
        (bool ok, string path, string message) = await UpdateService.DownloadAsync(info, progress);
        if (IsDisposed) return;

        if (!ok)
        {
            _busy = false;
            _check.Enabled = true;
            SetStatus($"Download failed: {message}", Theme.Bad);
            ActivityLog.Error($"Self-update download failed: {message}");
            return;
        }

        ActivityLog.Success($"Downloaded PC Medic {info.LatestVersion} ({Theme.Truncate(Path.GetFileName(path), 40)}).");
        SetStatus("Installing \u2014 the console will restart.", Theme.Good);

        (bool installed, string installMessage) = UpdateService.Install(path);
        ActivityLog.Write(installed ? LogLevel.Success : LogLevel.Error, installMessage);

        if (!installed)
        {
            _busy = false;
            _check.Enabled = true;
            SetStatus($"Install failed: {installMessage}", Theme.Bad);
            return;
        }

        await Task.Delay(700);
        Application.Exit();
    }

    /// <summary>
    /// Raises the busy plate for the life of the returned scope. The release-notes well is hidden for
    /// the same span: the plate is drawn over that rectangle, and two overlapping siblings leave the
    /// result to z-order, which is how the well ended up covering all but a sliver of the ring.
    /// </summary>
    private IDisposable ShowPlate()
    {
        // Everything the plate is drawn over steps aside, so the ring never lands on a chip edge and
        // the outcome cannot depend on which sibling happens to be in front.
        Control[] underneath = { _currentLabel, _currentChip, _latestLabel, _latestChip, _notesWell };
        foreach (Control control in underneath) control.Visible = false;

        IDisposable plate = _loading.Busy();
        return new PlateScope(this, plate, underneath);
    }

    private sealed class PlateScope : IDisposable
    {
        private readonly SelfUpdateForm _owner;
        private readonly IDisposable _plate;
        private readonly Control[] _restore;
        private bool _released;

        public PlateScope(SelfUpdateForm owner, IDisposable plate, Control[] restore)
        {
            _owner = owner;
            _plate = plate;
            _restore = restore;
        }

        public void Dispose()
        {
            if (_released) return;
            _released = true;

            _plate.Dispose();
            if (_owner.IsDisposed) return;

            foreach (Control control in _restore) control.Visible = true;
        }
    }

    private void SetStatus(string text, Color tone)
    {
        _status.Text = text;
        _status.ForeColor = tone;
    }
}
