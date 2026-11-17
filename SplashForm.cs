using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace PC_Medic;

/// <summary>
/// Brief launch card: the embedded brand artwork with the boot sweep and status laid over it.
/// The artwork is cosmetic, so a missing or unreadable resource falls back to the painted card.
/// </summary>
internal sealed partial class SplashForm : Form
{
    private const int BootMilliseconds = 1150;
    private const int CardWidth = 412;
    private const int CardHeight = 190;
    private const int HeroWidth = 640;
    private const int HeroHeight = 400;
    private const string HeroResource = "brand-hero.png";

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Image? _hero = LoadHero();
    private float _progress;

    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = _hero is null ? new Size(CardWidth, CardHeight) : new Size(HeroWidth, HeroHeight);
        BackColor = Theme.Bg;
        Font = Theme.Ui;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;

        InitializeComponent();

        try
        {
            Icon = Brand.CreateIcon(64);
        }
        catch
        {
            // The mark is cosmetic; a missing icon must never block launch.
        }

        _timer.Tick += (_, _) =>
        {
            _progress = Math.Min(1f, (float)(_clock.Elapsed.TotalMilliseconds / BootMilliseconds));
            Invalidate();
        };
        _timer.Start();
    }

    /// <summary>True once the minimum boot window has elapsed.</summary>
    public bool Finished => _progress >= 1f;

    /// <summary>Loads the embedded artwork; null means the splash paints the plain card instead.</summary>
    private static Image? LoadHero()
    {
        try
        {
            Assembly assembly = typeof(SplashForm).Assembly;

            string? name = assembly.GetManifestResourceNames().FirstOrDefault(candidate =>
                candidate.EndsWith(HeroResource, StringComparison.OrdinalIgnoreCase));

            if (name is null) return null;

            using Stream? stream = assembly.GetManifestResourceStream(name);
            if (stream is null) return null;

            using var image = Image.FromStream(stream);
            return new Bitmap(image); // detached from the stream, safe for the form's lifetime
        }
        catch
        {
            return null;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (_hero is null) PaintCard(g);
        else PaintHero(g);

        PaintBoot(g);

        if (_progress >= 1f)
        {
            _timer.Stop();
        }
    }

    /// <summary>The artwork, clipped to the card, with a scrim under the boot row.</summary>
    private void PaintHero(Graphics g)
    {
        var card = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

        using (GraphicsPath path = Theme.Round(card, 12f))
        {
            GraphicsState state = g.Save();
            g.SetClip(path);
            Cover(g, _hero!);
            g.Restore(state);

            // The artwork's lower panels sit where the status row goes: fade them under a scrim.
            var scrim = new RectangleF(0f, Height * 0.66f, Width, Height - (Height * 0.66f));
            using (var brush = new LinearGradientBrush(
                       scrim,
                       Color.FromArgb(0, 4, 8, 12),
                       Color.FromArgb(238, 4, 8, 12),
                       90f))
            {
                g.FillRectangle(brush, scrim);
            }

            using var edge = new Pen(Theme.Alpha(Theme.Accent, 200), 1.2f);
            g.DrawPath(edge, path);
        }
    }

    /// <summary>Scales the artwork to cover the card without distorting it.</summary>
    private void Cover(Graphics g, Image image)
    {
        float scale = Math.Max(Width / (float)image.Width, Height / (float)image.Height);
        float width = image.Width * scale;
        float height = image.Height * scale;

        g.DrawImage(image, new RectangleF((Width - width) / 2f, (Height - height) / 2f, width, height));
    }

    /// <summary>The original launch card, used when the artwork is unavailable.</summary>
    private void PaintCard(Graphics g)
    {
        var card = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        Theme.PaintSurface(g, card, 12f, Theme.Surface, Theme.Alpha(Theme.Accent, 170), Theme.Accent);

        Brand.PaintMark(g, new RectangleF(30f, 46f, 60f, 60f));

        TextRenderer.DrawText(g, "PC MEDIC", Theme.H1, new Point(106, 50), Theme.Text, TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(
            g,
            "SYSTEM OPTIMIZER",
            Theme.Kicker,
            new Point(108, 84),
            Theme.Alpha(Theme.Accent, 215),
            TextFormatFlags.NoPrefix);

        using (var rule = new Pen(Theme.BorderSoft))
        {
            g.DrawLine(rule, 30, 124, Width - 30, 124);
        }
    }

    /// <summary>Boot status, version and the sweep, placed for whichever card is showing.</summary>
    private void PaintBoot(Graphics g)
    {
        bool hero = _hero is not null;
        int left = hero ? 26 : 30;
        int right = Width - left;
        int statusTop = hero ? Height - 46 : 136;
        float barTop = hero ? Height - 26f : Height - 30f;

        TextRenderer.DrawText(
            g,
            _progress >= 1f ? "CONSOLE READY" : "INITIALIZING TELEMETRY",
            Theme.Kicker,
            new Point(left, statusTop),
            _progress >= 1f ? Theme.Good : hero ? Theme.Text : Theme.TextDim,
            TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(
            g,
            $"PORTABLE BUILD {AppInfo.CurrentVersionText}",
            Theme.Kicker,
            new Rectangle(left, statusTop, right - left, 14),
            Theme.TextDim,
            TextFormatFlags.Right | TextFormatFlags.NoPrefix);

        var track = new RectangleF(left, barTop, right - left, 3f);
        using (var backdrop = new SolidBrush(hero ? Color.FromArgb(120, 10, 18, 26) : Theme.BorderSoft))
        {
            g.FillRectangle(backdrop, track);
        }

        float sweep = Math.Max(0.02f, _progress);
        var fill = new RectangleF(track.X, track.Y, track.Width * sweep, track.Height);
        using (var brush = new LinearGradientBrush(
                   new RectangleF(track.X, track.Y, Math.Max(2f, track.Width * sweep), track.Height),
                   Theme.Alpha(Theme.AccentDeep, 255),
                   Theme.Accent,
                   0f))
        {
            g.FillRectangle(brush, fill);
        }

        using (var head = new SolidBrush(Theme.Accent))
        {
            g.FillEllipse(head, fill.Right - 2f, track.Y - 1.5f, 6f, 6f);
        }
    }
}
