using System.Drawing.Drawing2D;

namespace PC_Medic;

/// <summary>
/// Circular progress plate for dialog work that can say how far along it is: the ring fills clockwise
/// from twelve o'clock with the number in the middle. Where the work has no honest denominator, the
/// same ring sweeps a short lit arc instead, so a scan still reads as moving rather than stuck.
///
/// The drawn value eases toward <see cref="Percent"/> rather than snapping, so a pass that reports in
/// whole categories still looks like movement instead of eight jumps.
/// </summary>
internal sealed class RingProgress : SkinnedControl
{
    private readonly System.Windows.Forms.Timer _pulse = new() { Interval = 30 };
    private double _shown;
    private int _target;
    private float _sweep;
    private bool _indeterminate;
    private bool _running;
    private string _caption = "WORKING";

    public RingProgress()
    {
        Size = new Size(184, 152);
        Visible = false;
        _pulse.Tick += OnPulse;
    }

    /// <summary>Colour of the lit arc; the plate itself stays on the shared surface.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color Tone { get; set; } = Theme.Accent;

    /// <summary>Line under the ring: SCANNING, CLEANING.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Caption
    {
        get => _caption;
        set
        {
            if (_caption == value) return;
            _caption = value;
            Invalidate();
        }
    }

    /// <summary>The percentage the ring fills toward; what is drawn eases in behind it.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Percent
    {
        get => _target;
        set
        {
            _indeterminate = false;
            _target = Math.Clamp(value, 0, 100);
            if (_running) _pulse.Start();
            Invalidate();
        }
    }

    /// <summary>True while the ring is sweeping instead of filling.</summary>
    public bool Indeterminate => _indeterminate;

    /// <summary>Shows the plate and fills toward <paramref name="percent"/>.</summary>
    public void Begin(int percent = 0, string caption = "WORKING")
    {
        Caption = caption;
        _indeterminate = false;
        _target = Math.Clamp(percent, 0, 100);
        _shown = _target;
        _running = true;
        Visible = true;
        BringToFront();
        _pulse.Start();
        Invalidate();
    }

    /// <summary>Shows the plate for work with no known total: a lit arc sweeps the ring.</summary>
    public void BeginUncounted(string caption = "WORKING")
    {
        Caption = caption;
        _indeterminate = true;
        _sweep = 0f;
        _running = true;
        Visible = true;
        BringToFront();
        _pulse.Start();
        Invalidate();
    }

    public void End()
    {
        _running = false;
        _pulse.Stop();
        if (IsDisposed) return;
        Visible = false;
    }

    private void OnPulse(object? sender, EventArgs e)
    {
        if (_indeterminate)
        {
            _sweep += 0.016f;
            if (_sweep > 1f) _sweep -= 1f;
            Invalidate();
            return;
        }

        double delta = _target - _shown;
        if (Math.Abs(delta) < 0.5)
        {
            if (_shown != _target)
            {
                _shown = _target;
                Invalidate();
            }

            // Settled: nothing left to animate until Percent moves again.
            _pulse.Stop();
            return;
        }

        _shown += delta * 0.2;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pulse.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 96 || Height < 96) return;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Theme.PaintSurface(
            g,
            new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f),
            10f,
            Theme.Surface,
            Theme.Edge,
            Tone);

        Draw(g, ClientRectangle, _shown, _caption, Tone, _indeterminate, _sweep);
    }

    /// <summary>
    /// Paints the ring, its number and its caption inside <paramref name="area"/>. Kept static so the
    /// update plates (<see cref="ScanOverlay"/>) draw the same ring instead of a second copy of it.
    /// </summary>
    internal static void Draw(
        Graphics g,
        RectangleF area,
        double shown,
        string caption,
        Color tone,
        bool indeterminate,
        float sweep)
    {
        const int CaptionBand = 22;
        int size = (int)Math.Min(area.Width - 40, area.Height - CaptionBand - 24);
        if (size < 44) return;

        float thickness = Math.Max(5f, size * 0.085f);
        float inset = thickness / 2f;
        var ring = new RectangleF(
            area.X + ((area.Width - size) / 2f) + inset,
            area.Y + 12f + inset,
            size - thickness,
            size - thickness);

        using (var track = new Pen(Theme.BorderSoft, thickness))
        {
            g.DrawEllipse(track, ring);
        }

        using (var glow = new SolidBrush(Theme.Alpha(tone, 10)))
        {
            g.FillEllipse(glow, RectangleF.Inflate(ring, -inset, -inset));
        }

        float start = indeterminate ? -90f + (sweep * 360f) : -90f;
        float lit = indeterminate ? 96f : (float)(shown * 3.6);
        if (lit > 0.2f)
        {
            using var arc = new Pen(tone, thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(arc, ring, start, Math.Min(lit, 359.5f));
        }

        string value = indeterminate ? "\u2014" : $"{(int)Math.Round(shown)}%";
        TextRenderer.DrawText(
            g,
            value,
            Theme.Numeric,
            new Rectangle((int)area.X, (int)ring.Top, (int)area.Width, (int)ring.Height),
            indeterminate ? Theme.TextDim : Theme.Text,
            Centered);

        TextRenderer.DrawText(
            g,
            caption.ToUpperInvariant(),
            Theme.Kicker,
            new Rectangle((int)area.X + 10, (int)area.Bottom - CaptionBand - 4, (int)area.Width - 20, 16),
            indeterminate ? tone : Theme.TextDim,
            Centered);
    }
}
