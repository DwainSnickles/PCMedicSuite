using System.Drawing.Drawing2D;

namespace PC_Medic;

/// <summary>
/// The junk finder: scans the cleanable categories on open, shows what each one weighs, and removes
/// only what is ticked. Cleaning runs on a background thread so the dialog stays responsive, and
/// every target goes through JunkService, which sends files to the Recycle Bin where it can.
///
/// Both passes report into a ring: the scan knows how many categories it has finished and the clean
/// knows how many it has left, so the ring carries a real percentage rather than a spinner pretending
/// to be one. A finished clean hands its numbers to <see cref="JunkResultForm"/> instead of squeezing
/// them into the one status line.
/// </summary>
internal sealed partial class JunkCleanupForm : Form
{
    /// <summary>One list row: the category the scan measured, plus whether the user has ticked it.</summary>
    private sealed class Row
    {
        public Row(JunkTarget target) => Target = target;

        public JunkTarget Target { get; }

        public bool Ticked { get; set; }
    }

    private readonly List<Row> _rows = new();
    private bool _scanning;

    public JunkCleanupForm()
    {
        Text = "Disk cleanup";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        ClientSize = new Size(640, 430);

        InitializeComponent();

        // The list is painted by hand. A stock check box is drawn in system colours, so on this
        // surface it reads as a light-theme control dropped into a dark dialog and a ticked row is
        // hard to tell from an unticked one; the tick here is the accent, on an accent-outlined box.
        _list.DrawItem += DrawRow;
        _list.MouseClick += (_, e) => Toggle(_list.IndexFromPoint(e.Location));
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Space) return;
            Toggle(_list.SelectedIndex);
            e.Handled = true;
        };

        _clean.Click += async (_, _) => await CleanSelectedAsync();
        _help.Click += (_, _) => HelpForm.Open(this, "junk-cleanup");
        _close.Click += (_, _) => Close();

        _ = ScanAsync();
    }

    /// <summary>
    /// Measures every category. <paramref name="tickFound"/> ticks the categories that came back with
    /// something in them; after a clean the caller passes false, because what is left is the junk
    /// Windows would not release and re-ticking it is what made the old dialog look like it had not run.
    /// </summary>
    private async Task ScanAsync(bool tickFound = true)
    {
        if (IsDisposed) return;

        _scanning = true;
        _clean.Enabled = false;
        _list.Enabled = false;
        _rows.Clear();
        _list.Items.Clear();

        _status.Text = "SCANNING FOR JUNK\u2026";
        _status.ForeColor = Theme.Accent;

        _ring.Begin(0, "SCANNING");
        var progress = new Progress<ScanProgress>(report =>
        {
            if (IsDisposed) return;
            _ring.Percent = report.Percent;
            _status.Text = $"{report.Message} \u2014 {report.StepLabel.ToUpperInvariant()}";
        });

        List<JunkTarget> targets;
        try
        {
            targets = await Task.Run(() => JunkService.Scan(progress));
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;

            _ring.End();
            _list.Enabled = true;
            _clean.Enabled = true;
            _scanning = false;
            _status.Text = $"SCAN FAILED \u2014 {ex.Message.ToUpperInvariant()}";
            _status.ForeColor = Theme.Bad;
            return;
        }

        if (IsDisposed) return;

        _ring.Percent = 100;
        _ring.End();

        foreach (JunkTarget target in targets)
        {
            var row = new Row(target) { Ticked = tickFound && target.Bytes > 0 };
            _rows.Add(row);
            _list.Items.Add(Label(row));
        }

        ulong total = targets.Aggregate(0UL, (sum, target) => sum + target.Bytes);
        int found = targets.Count(target => target.Bytes > 0);

        _status.Text = total == 0
            ? "NOTHING FOUND \u2014 THE USUAL SPOTS ARE CLEAN"
            : $"{found} CATEGORY(IES) \u00b7 {Theme.Bytes(total)} FOUND" +
              (tickFound ? string.Empty : " \u2014 TICK TO RETRY");
        _status.ForeColor = total == 0 ? Theme.Good : Theme.Text;

        _list.Enabled = true;
        _clean.Enabled = true;
        _scanning = false;
        _list.Invalidate();
    }

    private static string Label(Row row) =>
        $"{row.Target.Name} \u2014 {Theme.Bytes(row.Target.Bytes)}" +
        (row.Target.RequiresAdmin && !Native.IsElevated ? "  (NEEDS ADMIN)" : string.Empty);

    private async Task CleanSelectedAsync()
    {
        var selected = _rows.Where(row => row.Ticked).Select(row => row.Target).ToList();

        if (selected.Count == 0)
        {
            _status.Text = "TICK A CATEGORY FIRST";
            _status.ForeColor = Theme.Warn;
            return;
        }

        _clean.Enabled = false;
        _list.Enabled = false;

        _ring.Begin(0, "CLEANING");
        var results = new List<(JunkTarget Target, JunkCleanResult Result)>(selected.Count);

        int removed = 0, skipped = 0, queuedReboot = 0, failed = 0;
        ulong freed = 0;

        for (int index = 0; index < selected.Count; index++)
        {
            if (IsDisposed) return;

            JunkTarget target = selected[index];
            _ring.Percent = (int)Math.Round(index * 100.0 / selected.Count);
            _status.Text = $"CLEANING {target.Name} \u2014 {index + 1} / {selected.Count}";
            _status.ForeColor = Theme.Accent;

            JunkCleanResult result = await Task.Run(() => JunkService.Clean(target.Id));
            if (IsDisposed) return;

            results.Add((target, result));
            removed += result.Removed;
            skipped += result.Skipped;
            queuedReboot += result.QueuedReboot;
            failed += result.Failed;
            freed += result.Freed;

            ActivityLog.Write(
                result.Error is null ? LogLevel.Success : LogLevel.Warn,
                $"Junk cleanup '{target.Name}': {result.Removed} removed, {Theme.Bytes(result.Freed)} freed" +
                (result.QueuedReboot > 0 ? $", {result.QueuedReboot} queued for reboot" : string.Empty) +
                (result.Skipped > 0 ? $", {result.Skipped} skipped (in use)" : string.Empty) +
                (result.Error is null ? string.Empty : $" \u2014 {result.Error}"));
        }

        _ring.Percent = 100;

        ActivityLog.Info(
            $"Junk cleanup finished: {Theme.Bytes(freed)} freed, {removed} removed, " +
            $"{skipped} skipped (in use), {queuedReboot} queued for reboot, {failed} failed.");

        // Sizes changed, so the scan runs again rather than showing stale numbers, and nothing is
        // ticked afterwards: a tick has to mean "clean this", not "this is what you just cleaned".
        await ScanAsync(tickFound: false);
        if (IsDisposed) return;

        _ring.End();

        JunkResultForm.Show(this, results);
    }

    private void Toggle(int index)
    {
        if (!_list.Enabled || index < 0 || index >= _rows.Count) return;

        _rows[index].Ticked = !_rows[index].Ticked;
        _list.Invalidate();
    }

    /// <summary>
    /// One row: an accent-outlined tick box, the category, and its size. Rows the user has not ticked
    /// stay dim, so the difference between "will be cleaned" and "will be left alone" does not rest on
    /// a 15-pixel glyph alone.
    /// </summary>
    private void DrawRow(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _rows.Count) return;

        Row row = _rows[e.Index];
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(0, e.Bounds.Y, _list.ClientSize.Width, e.Bounds.Height);
        bool selected = (e.State & DrawItemState.Selected) != 0;

        using (var background = new SolidBrush(selected ? Theme.Alpha(Theme.Accent, 26) : Theme.Surface))
        {
            g.FillRectangle(background, bounds);
        }

        const int Box = 15;
        var box = new Rectangle(10, bounds.Y + ((bounds.Height - Box) / 2), Box, Box);

        using (GraphicsPath path = Theme.Round(box, 3.5f))
        {
            using var fill = new SolidBrush(row.Ticked ? Theme.Alpha(Theme.Accent, 40) : Theme.Surface2);
            using var edge = new Pen(row.Ticked ? Theme.Accent : Theme.BorderSoft, 1.4f);
            g.FillPath(fill, path);
            g.DrawPath(edge, path);
        }

        if (row.Ticked)
        {
            // Drawn rather than a glyph: the tick has to sit inside the box at every DPI.
            using var tick = new Pen(Theme.Accent, 1.8f);
            g.DrawLines(tick, new[]
            {
                new PointF(box.X + 3.4f, box.Y + 7.6f),
                new PointF(box.X + 6.2f, box.Y + 10.6f),
                new PointF(box.X + 11.4f, box.Y + 4.4f),
            });
        }

        Color text = !_list.Enabled ? Theme.TextFaint : row.Ticked ? Theme.Text : Theme.TextDim;

        TextRenderer.DrawText(
            g,
            _list.Items[e.Index]?.ToString() ?? string.Empty,
            Theme.MonoSmall,
            new Rectangle(box.Right + 10, bounds.Y, Math.Max(10, bounds.Width - box.Right - 18), bounds.Height),
            text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape && !_scanning) Close();
    }
}
