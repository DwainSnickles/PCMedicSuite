using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PC_Medic;

/// <summary>
/// The PC Medic mark: a cyan instrument plate with a diagnostic pulse trace.
/// Drawn in code so it stays crisp at any size and needs no image asset.
/// </summary>
internal static class Brand
{
    // Normalised pulse trace: flat line, sharp spike, settle, flat line.
    private static readonly PointF[] Pulse =
    {
        new(0.20f, 0.55f),
        new(0.33f, 0.55f),
        new(0.42f, 0.33f),
        new(0.54f, 0.74f),
        new(0.63f, 0.55f),
        new(0.80f, 0.55f),
    };

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>Builds the window/taskbar icon from the square core plate, so no .ico asset is required.</summary>
    public static Icon CreateIcon(int size = 64)
    {
        using var bitmap = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            PaintIconPlate(g, new RectangleF(0f, 0f, size, size));
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    /// <summary>
    /// The square app icon: the diagnostic core from the brand artwork — dark instrument plate,
    /// accent edge, corner brackets and the pulse trace — filling the given square.
    /// </summary>
    public static void PaintIconPlate(Graphics g, RectangleF bounds)
    {
        float size = Math.Min(bounds.Width, bounds.Height);
        if (size < 4) return;

        g.SmoothingMode = SmoothingMode.AntiAlias;

        float left = bounds.X + ((bounds.Width - size) / 2f);
        float top = bounds.Y + ((bounds.Height - size) / 2f);

        float inset = size * 0.04f;
        var plate = new RectangleF(left + inset, top + inset, size - (inset * 2f), size - (inset * 2f));

        using (GraphicsPath path = Theme.Round(plate, size * 0.23f))
        {
            using (var fill = new LinearGradientBrush(
                       plate,
                       Color.FromArgb(28, 58, 78),
                       Color.FromArgb(4, 9, 14),
                       90f))
            {
                g.FillPath(fill, path);
            }

            using var edge = new Pen(Theme.Alpha(Theme.Accent, 240), Math.Max(1f, size * 0.03f));
            g.DrawPath(edge, path);
        }

        // Brackets turn to mush below 48px, so they only appear where they can be read.
        if (size >= 48)
        {
            float arm = size * 0.17f;
            float pad = size * 0.15f;
            float l = plate.Left + pad;
            float t = plate.Top + pad;
            float r = plate.Right - pad;
            float b = plate.Bottom - pad;

            using var bracket = new Pen(Theme.Alpha(Theme.Accent, 200), Math.Max(1f, size * 0.022f));
            g.DrawLine(bracket, l, t + arm, l, t);
            g.DrawLine(bracket, l, t, l + arm, t);
            g.DrawLine(bracket, r, t + arm, r, t);
            g.DrawLine(bracket, r, t, r - arm, t);
            g.DrawLine(bracket, l, b - arm, l, b);
            g.DrawLine(bracket, l, b, l + arm, b);
            g.DrawLine(bracket, r, b - arm, r, b);
            g.DrawLine(bracket, r, b, r - arm, b);
        }

        var points = new PointF[Pulse.Length];
        for (int i = 0; i < Pulse.Length; i++)
        {
            points[i] = new PointF(
                plate.X + (Pulse[i].X * plate.Width),
                plate.Y + (Pulse[i].Y * plate.Height));
        }

        using (var halo = new Pen(Theme.Alpha(Theme.Accent, 70), Math.Max(2f, size * 0.13f))
               {
                   LineJoin = LineJoin.Round,
                   StartCap = LineCap.Round,
                   EndCap = LineCap.Round,
               })
        {
            g.DrawLines(halo, points);
        }

        // Small frames get a fatter, fully saturated trace; big ones keep the pale instrument look.
        using (var trace = new Pen(
                   size >= 48 ? Color.FromArgb(236, 248, 255) : Theme.Accent,
                   Math.Max(1.4f, size * (size < 32 ? 0.10f : 0.075f)))
               {
                   LineJoin = LineJoin.Round,
                   StartCap = LineCap.Round,
                   EndCap = LineCap.Round,
               })
        {
            g.DrawLines(trace, points);
        }

        using var node = new SolidBrush(Theme.Accent);
        float dot = Math.Max(2f, size * 0.045f);
        g.FillEllipse(node, points[0].X - (dot / 2f), points[0].Y - (dot / 2f), dot, dot);
        g.FillEllipse(node, points[^1].X - (dot / 2f), points[^1].Y - (dot / 2f), dot, dot);
    }

    public static void PaintMark(Graphics g, RectangleF bounds)
    {
        float size = Math.Min(bounds.Width, bounds.Height);
        if (size < 6) return;

        g.SmoothingMode = SmoothingMode.AntiAlias;

        float x = bounds.X + ((bounds.Width - size) / 2f);
        float y = bounds.Y + ((bounds.Height - size) / 2f);
        var plate = new RectangleF(x + (size * 0.05f), y + (size * 0.05f), size * 0.9f, size * 0.9f);

        using (GraphicsPath platePath = Theme.Round(plate, size * 0.19f))
        {
            using (var fill = new LinearGradientBrush(
                       plate,
                       Color.FromArgb(19, 30, 41),
                       Color.FromArgb(6, 9, 14),
                       90f))
            {
                g.FillPath(fill, platePath);
            }

            using (var edge = new Pen(Theme.Alpha(Theme.Accent, 215), Math.Max(1f, size * 0.05f)))
            {
                g.DrawPath(edge, platePath);
            }
        }

        var points = new PointF[Pulse.Length];
        for (int i = 0; i < Pulse.Length; i++)
        {
            points[i] = new PointF(
                plate.X + (Pulse[i].X * plate.Width),
                plate.Y + (Pulse[i].Y * plate.Height));
        }

        using (var trace = new Pen(Theme.Accent, Math.Max(1.2f, size * 0.062f)))
        {
            trace.LineJoin = LineJoin.Round;
            trace.StartCap = LineCap.Round;
            trace.EndCap = LineCap.Round;
            g.DrawLines(trace, points);
        }

        using var node = new SolidBrush(Theme.Alpha(Theme.Accent, 200));
        float dot = Math.Max(1.6f, size * 0.055f);
        g.FillEllipse(node, points[0].X - (dot / 2f), points[0].Y - (dot / 2f), dot, dot);
        g.FillEllipse(node, points[^1].X - (dot / 2f), points[^1].Y - (dot / 2f), dot, dot);
    }
}

/// <summary>Sidebar logo plate. Paints the brand mark only; no image files involved.</summary>
internal sealed class BrandMark : SkinnedControl
{
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 6 || Height < 6) return;
        Brand.PaintMark(e.Graphics, new RectangleF(0f, 0f, Width, Height));
    }
}
