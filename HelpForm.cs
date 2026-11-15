using System.Reflection;

namespace PC_Medic;

/// <summary>
/// The help window: a topic rail on the left, the topic itself on the right.
///
/// Each topic shows a one-line summary, the screenshot captured from this build of the page when one
/// is embedded, then titled sections and an optional "worth knowing" list. Every page and the dialogs
/// that do real work have a topic in <see cref="HelpContent"/>, so the header "?" always has an answer
/// for whatever is on screen.
/// </summary>
internal sealed partial class HelpForm : Form
{

    private HelpTopic? _topic;

    public HelpForm(string? key = null)
    {
        Text = "PC MEDIC \u2014 Help";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(880, 560);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        KeyPreview = true;

        InitializeComponent();

        _topics.DrawItem += DrawTopic;
        _topics.SelectedIndexChanged += (_, _) =>
        {
            if (_topics.SelectedItem is HelpTopic picked) ShowTopic(picked);
        };

        _close.Click += (_, _) => Close();

        // CenterParent alone is not enough: it does nothing without an owner, and it happily puts a
        // tall window half off a short screen. Positioning in Load settles both before the first paint.
        Load += (_, _) => CenterOnOwner();

        Resize += (_, _) => Arrange();
        Arrange();

        foreach (HelpTopic topic in HelpContent.Topics) _topics.Items.Add(topic);

        HelpTopic first = HelpContent.Find(key) ?? HelpContent.Topics[0];   // overview leads the list
        _topics.SelectedIndex = Array.IndexOf(HelpContent.Topics, first);
    }

    /// <summary>Opens the manual for a page or dialog key, owned by the window that asked for it.</summary>
    public static void Open(Control? owner, string? key)
    {
        using var form = new HelpForm(key);

        // The owner is what CenterOnOwner centres against, so it is never left null when a window
        // is available at all.
        form.ShowDialog(owner?.FindForm() ?? Form.ActiveForm);
    }

    /// <summary>
    /// Centres the manual on the window that opened it - the shell, or the dialog whose "?" was
    /// pressed - and keeps it inside that screen's working area. Sized down to the screen when the
    /// default window would not fit, never below the minimum that keeps the two panes readable.
    /// </summary>
    private void CenterOnOwner()
    {
        Rectangle anchor = Owner?.Bounds ?? Screen.FromControl(this).WorkingArea;
        Rectangle screen = Screen.FromRectangle(anchor).WorkingArea;

        ClientSize = new Size(
            Math.Max(MinimumSize.Width, Math.Min(ClientSize.Width, screen.Width - 40)),
            Math.Max(MinimumSize.Height, Math.Min(ClientSize.Height, screen.Height - 40)));

        int left = anchor.Left + (anchor.Width - Width) / 2;
        int top = anchor.Top + (anchor.Height - Height) / 2;

        Location = new Point(
            Math.Clamp(left, screen.Left, Math.Max(screen.Left, screen.Right - Width)),
            Math.Clamp(top, screen.Top, Math.Max(screen.Top, screen.Bottom - Height)));
    }

    /// <summary>A dialog's own "?": the same caption, size and wiring in every window.</summary>
    public static NeonButton DialogButton(Control owner, string key, Size size)
    {
        var button = new NeonButton { Text = "?", Size = size, AccessibleName = "Help" };
        button.Click += (_, _) => Open(owner, key);
        return button;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ------------------------------------------------------------------ layout

    private void Arrange()
    {
        int wide = ClientSize.Width;
        int tall = ClientSize.Height;

        _topics.SetBounds(18, 82, 248, Math.Max(120, tall - 82 - 66));
        _body.SetBounds(282, 82, Math.Max(320, wide - 282 - 18), Math.Max(120, tall - 82 - 66));

        _hint.SetBounds(20, tall - 44, Math.Max(120, wide - 20 - _close.Width - 36), 22);
        _close.SetBounds(wide - 18 - _close.Width, tall - 50, _close.Width, 34);

        // A wider window means a wider measure for the text and a bigger screenshot.
        if (_topic is not null) ShowTopic(_topic);
    }

    /// <summary>The column’s measure, with the scrollbar reserved so no horizontal bar appears.</summary>
    private int BodyWidth() => Math.Max(
        300, _body.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 10);

    // ------------------------------------------------------------------ topic

    private void ShowTopic(HelpTopic topic)
    {
        _topic = topic;

        _body.SuspendLayout();
        foreach (Control child in _body.Controls.Cast<Control>().ToArray()) child.Dispose();

        int width = BodyWidth();
        int y = 6;

        Add(topic.Title, new Font(Theme.H2.FontFamily, 16f, FontStyle.Bold), Theme.Text, width, ref y);
        y += 6;

        Add(topic.Summary, Theme.Ui, Theme.TextDim, width, ref y);
        y += 16;

        AddScreenshot(topic, width, ref y);

        foreach (HelpSection section in topic.Sections)
        {
            Add(section.Heading.ToUpperInvariant(), Theme.Kicker, Theme.Accent, width, ref y);
            y += 6;
            Add(section.Body, Theme.Ui, Theme.Text, width, ref y);
            y += 18;
        }

        if (topic.Tips is { Length: > 0 } tips)
        {
            Add("WORTH KNOWING", Theme.Kicker, Theme.Violet, width, ref y);
            y += 6;

            foreach (string tip in tips) Add("\u2022  " + tip, Theme.UiSmall, Theme.TextDim, width - 18, ref y);
            y += 10;
        }

        _body.AutoScrollMinSize = new Size(0, y + 14);
        _hint.Text = $"SHOWING {topic.Group} \u00b7 {topic.Title.ToUpperInvariant()}";
        _body.ResumeLayout();
    }

    private void AddScreenshot(HelpTopic topic, int width, ref int y)
    {
        using Stream? stream = typeof(HelpForm).Assembly.GetManifestResourceStream(HelpContent.ImageResource(topic.Key));
        if (stream is null) return;

        Image? picture;
        try
        {
            using var loaded = new Bitmap(stream);
            picture = new Bitmap(loaded);   // copied so the stream can close
        }
        catch
        {
            // A screenshot is a nicety: a broken resource must never stop the window opening.
            return;
        }

        // Full aspect at the column width: the body scrolls, so nothing is gained by squeezing the
        // screenshot into a fixed height and letterboxing it.
        int height = Math.Max(120, (int)(width * ((double)picture.Height / picture.Width)));

        _body.Controls.Add(new PictureBox
        {
            Image = picture,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Theme.Surface,
            Location = new Point(2, y),
            Size = new Size(width, height),
        });

        y += height + 20;
    }

    private void Add(string text, Font font, Color color, int width, ref int y)
    {
        var label = new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = Color.Transparent,
            AutoSize = true,
            MaximumSize = new Size(width, 0),
            Location = new Point(2, y),
        };

        _body.Controls.Add(label);
        y += label.Height + 2;
    }

    // ------------------------------------------------------------------ rail

    private void DrawTopic(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _topics.Items[e.Index] is not HelpTopic topic) return;

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        Rectangle row = e.Bounds;

        using (var back = new SolidBrush(selected ? Theme.Surface2 : Theme.Surface))
            e.Graphics.FillRectangle(back, row);

        if (selected)
        {
            using var accent = new SolidBrush(Theme.Accent);
            e.Graphics.FillRectangle(accent, new Rectangle(row.X, row.Y + 6, 3, row.Height - 12));
        }

        using (var pen = new Pen(Theme.BorderSoft))
            e.Graphics.DrawLine(pen, row.Left + 10, row.Bottom - 1, row.Right - 10, row.Bottom - 1);

        // One line per topic, ellipsised: a group caption on the same line collided with the long
        // titles, and the rail is short enough to scan without one.
        TextRenderer.DrawText(
            e.Graphics, topic.Title, Theme.Ui, new Rectangle(row.X + 14, row.Y, row.Width - 24, row.Height),
            selected ? Theme.Text : Theme.TextDim,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }
}
