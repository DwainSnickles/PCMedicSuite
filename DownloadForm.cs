using System.Drawing.Drawing2D;

namespace PC_Medic;

/// <summary>One package the download panel moves through, with the work that does it.</summary>
internal sealed record DownloadJob(
    string Name,
    string Detail,
    Func<IProgress<int>, CancellationToken, Task<(bool Ok, string Message)>> Run);

/// <summary>What became of one job, in the order the queue ran.</summary>
internal sealed record DownloadResult(string Name, bool Ok, bool Stopped, string Message);

/// <summary>
/// Progress surface for a download pass: a row per package with its own bar, an overall bar and count, and
/// one CANCEL that stops the package in flight. Hosted either in <see cref="DownloadForm"/> or laid over the
/// updates column, so a single install reports in the same place as a full pass.
/// </summary>
internal sealed partial class DownloadPanel : SkinnedControl
{
    private const int Pad = 24;

    private readonly List<DownloadJob> _jobs;
    private readonly List<ToggleRow> _rows = new();
    private readonly List<DownloadResult> _results = new();
    private readonly List<int> _failures = new();
    private int _failCursor = -1;
    private readonly CancellationTokenSource _token = new();
    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _running;

    public DownloadPanel(string title, string kicker, IReadOnlyList<DownloadJob> jobs)
    {
        InitializeComponent();

        _jobs = jobs.ToList();

        BackColor = Theme.Surface;
        DoubleBuffered = true;

        _kicker.Text = kicker;
        _kicker.Font = Theme.Kicker;
        _kicker.ForeColor = Theme.Alpha(Theme.Accent, 215);
        _kicker.BackColor = Color.Transparent;
        _kicker.AutoSize = true;

        _title.Text = title;
        _title.Font = new Font(Theme.H2.FontFamily, 15f, FontStyle.Bold);
        _title.ForeColor = Theme.Text;
        _title.BackColor = Color.Transparent;
        _title.AutoSize = true;

        _count.Text = $"0 / {_jobs.Count}";
        _count.Font = Theme.Numeric;
        _count.ForeColor = Theme.TextDim;
        _count.BackColor = Color.Transparent;
        _count.AutoSize = false;
        _count.TextAlign = ContentAlignment.MiddleRight;

        _bar.Visible = true;
        _bar.Percent = 0;

        _list.BackColor = Theme.Surface;
        _list.AutoScroll = true;
        _list.Resize += (_, _) => SizeRows();

        _status.Text = "Starting\u2026";
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.BackColor = Color.Transparent;
        _status.AutoSize = false;
        _status.AutoEllipsis = true;
        _status.TextAlign = ContentAlignment.MiddleLeft;

        BuildRows();

        _help.Click += (_, _) => HelpForm.Open(this, "downloads");

        _cancel.Click += (_, _) =>
        {
            if (_running) RequestCancel();
            else CloseRequested?.Invoke();
        };

        _failJump.Click += (_, _) => ShowNextFailure();

        _keep.Click += (_, _) => Answer(remove: false);
        _remove.Click += (_, _) => Answer(remove: true);

        // The corner close is the guaranteed way out: it stops the package and shuts the window, and the
        // host allows that even mid-pass, so a stubborn installer can never hold the window hostage.
        _shut.Click += (_, _) =>
        {
            RequestCancel();
            CloseRequested?.Invoke();
        };

        _sweep.Tick += (_, _) =>
        {
            foreach (ToggleRow row in _rows)
            {
                if (row.Working) row.Sweep();
            }
        };

        // A single install that worked leaves nothing to decide, so the window steps aside by itself;
        // the page verifies the upgrade as soon as it is gone.
        _dismiss.Tick += (_, _) =>
        {
            _dismiss.Stop();
            CloseRequested?.Invoke();
        };

        Resize += (_, _) => LayoutContent();
        LayoutContent();
    }

    /// <summary>Raised when the panel is finished and its button is asked to close the host.</summary>
    public event Action? CloseRequested;

    /// <summary>What each job reported, in queue order.</summary>
    public IReadOnlyList<DownloadResult> Results => _results;

    /// <summary>True once the keep-or-remove offer was answered with REMOVE.</summary>
    public bool RemoveRequested { get; private set; }

    /// <summary>Completes when the pass has stopped, by finishing or by cancelling.</summary>
    public Task WhenFinished => _finished.Task;

    public bool Running => _running;

    /// <summary>Begins the pass; call once the panel has a parent and a size.</summary>
    public void Start()
    {
        if (_running || _finished.Task.IsCompleted) return;

        _ = RunAsync();
    }

    /// <summary>Cancels the package in flight; the panel stays until it stops.</summary>
    public void RequestCancel()
    {
        if (!_running) return;

        _cancel.Enabled = false;
        _cancel.Text = "CANCELLING";
        _token.Cancel();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var fill = new SolidBrush(Theme.Surface);
        e.Graphics.FillRectangle(fill, ClientRectangle);

        using var edge = new Pen(Theme.Edge);
        e.Graphics.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sweep.Dispose();
            _dismiss.Dispose();
            _token.Cancel();
            _token.Dispose();
            _finished.TrySetResult();
        }

        base.Dispose(disposing);
    }

    private void LayoutContent()
    {
        if (Width < 240 || Height < 220) return;

        _kicker.Location = new Point(Pad, 14);
        _title.Location = new Point(Pad, 32);
        _shut.SetBounds(Width - Pad - _shut.Width, 12, _shut.Width, 28);
        _help.SetBounds(_shut.Left - 6 - _help.Width, 12, _help.Width, 28);
        _count.SetBounds(Width - Pad - 36 - 100, 34, 100, 22);
        _bar.SetBounds(Pad, 74, Width - (Pad * 2), 10);

        int listTop = 96;
        int listBottom = Height - 62;
        _list.SetBounds(Pad - 4, listTop, Width - (Pad * 2) + 8, Math.Max(60, listBottom - listTop));

        _status.SetBounds(Pad, Height - 50, Math.Max(120, Width - (Pad * 2) - 126), 30);
        _cancel.SetBounds(Width - Pad - _cancel.Width, Height - 48, _cancel.Width, 32);

        _remove.SetBounds(Width - Pad - _remove.Width, Height - 48, _remove.Width, 32);
        _keep.SetBounds(_remove.Left - 8 - _keep.Width, Height - 48, _keep.Width, 32);
        _failJump.SetBounds(_cancel.Left - 8 - _failJump.Width, Height - 48, _failJump.Width, 32);

        // The offer puts two buttons where the question sits, so it takes what room they leave.
        if (_keep.Visible)
        {
            _status.SetBounds(Pad, Height - 50, Math.Max(120, _keep.Left - Pad - 8), 30);
        }
    }

    private void BuildRows()
    {
        _list.SuspendLayout();

        int y = 6;

        foreach (DownloadJob job in _jobs)
        {
            var row = new ToggleRow
            {
                Title = Theme.Truncate(job.Name, 56),
                Subtitle = Theme.Truncate(job.Detail, 72),
                BadgeText = "WAITING",
                BadgeTone = Theme.TextDim,
                ShowSwitch = false,
                Size = new Size(RowWidth(), 56),
                Location = new Point(6, y),
            };

            _list.Controls.Add(row);
            _rows.Add(row);
            y += 62;
        }

        _list.AutoScrollMinSize = new Size(0, y + 6);
        _list.ResumeLayout();
    }

    private int RowWidth() => Math.Max(220, _list.ClientSize.Width - 24);

    private void SizeRows()
    {
        foreach (ToggleRow row in _rows) row.Width = RowWidth();
    }

    private async Task RunAsync()
    {
        _running = true;
        _sweep.Start();

        int completed = 0;

        for (int index = 0; index < _jobs.Count; index++)
        {
            DownloadJob job = _jobs[index];
            ToggleRow row = _rows[index];

            if (_token.IsCancellationRequested)
            {
                End(row, false, true);
                _results.Add(new DownloadResult(job.Name, false, true, "Cancelled."));
                continue;
            }

            Follow(row);
            Begin(row);
            SetStatus($"{Theme.Truncate(job.Name.ToUpperInvariant(), 40)}\u2026", Theme.Text);
            SetOverall(index, 0);

            bool ok;
            string message;
            bool stopped = false;

            try
            {
                var progress = new Progress<int>(percent =>
                {
                    if (IsDisposed) return;

                    Report(row, percent);
                    SetOverall(index, percent);
                });

                (ok, message) = await job.Run(progress, _token.Token);
                stopped = _token.IsCancellationRequested;
            }
            catch (OperationCanceledException)
            {
                ok = false;
                message = "Cancelled.";
                stopped = true;
            }
            catch (Exception ex)
            {
                ok = false;
                message = ex.Message;
            }

            if (IsDisposed) return;

            End(row, ok, stopped, message);
            _results.Add(new DownloadResult(job.Name, ok, stopped, message));

            if (ok && !stopped) completed++;
        }

        if (IsDisposed) return;

        Finish(completed);
    }

    private void Finish(int completed)
    {
        _sweep.Stop();
        _running = false;
        _bar.Percent = 100;
        _count.Text = $"{_jobs.Count} / {_jobs.Count}";
        _cancel.Text = "CLOSE";
        _cancel.Enabled = true;

        bool cancelled = _results.Any(result => result.Stopped);
        int failed = _results.Count(result => !result.Ok && !result.Stopped);

        if (cancelled)
        {
            SetStatus($"{completed} OF {_jobs.Count} COMPLETED \u2014 CANCELLED", Theme.Warn);
        }
        else
        {
            SetStatus(
                failed == 0 ? $"ALL {completed} COMPLETED" : $"{completed} OF {_jobs.Count} COMPLETED \u2014 {failed} FAILED",
                failed == 0 ? Theme.Good : Theme.Warn);

            // A single package that failed is a decision, not a summary: the window stays up and asks.
            if (failed > 0 && _jobs.Count == 1 && _results.Count == 1)
            {
                OfferKeepOrRemove();
            }
            else if (_jobs.Count == 1 && _results.Count == 1)
            {
                _title.Text = "UPGRADE COMPLETE";
                _dismiss.Start();
            }

            // A batch that failed somewhere ends on that row rather than at the top of the queue, and the
            // footer keeps a way back to every failure once the list has been scrolled away.
            if (failed > 0 && _jobs.Count > 1)
            {
                _failures.Clear();

                for (int index = 0; index < _results.Count; index++)
                {
                    if (!_results[index].Ok && !_results[index].Stopped) _failures.Add(index);
                }

                _failCursor = -1;
                _failJump.Text = $"{_failures.Count} FAILED";
                _failJump.Visible = true;
                ShowNextFailure();
            }
        }

        _finished.TrySetResult();
    }

    /// <summary>
    /// Turns the window into the choice a failure deserves: the reason lands on the failed row, and the
    /// footer offers keeping the app or removing it instead of a summary nobody can act on.
    /// </summary>
    private void OfferKeepOrRemove()
    {
        ToggleRow row = _rows[0];

        row.Subtitle = Theme.Truncate(_results[0].Message.ToUpperInvariant(), 68);
        row.BadgeText = "FAILED";
        row.BadgeTone = Theme.Bad;
        row.Invalidate();

        _title.Text = "UPGRADE FAILED";
        _count.Text = string.Empty;

        _cancel.Visible = false;
        _failJump.Visible = false;
        _keep.Enabled = true;
        _remove.Enabled = true;
        _keep.Visible = true;
        _remove.Visible = true;

        SetStatus("KEEP IT OR REMOVE IT?", Theme.Warn);
        LayoutContent();
    }

    /// <summary>Records the answer to the offer and lets the host close on it.</summary>
    private void Answer(bool remove)
    {
        RemoveRequested = remove;
        _keep.Enabled = false;
        _remove.Enabled = false;
        CloseRequested?.Invoke();
    }

    /// <summary>
    /// Brings the package being worked on into view. A queue longer than the window would otherwise keep
    /// showing the rows that already finished, with the live one below the fold.
    /// </summary>
    private void Follow(ToggleRow row)
    {
        int room = _list.ClientSize.Height;
        if (room <= 0) return;

        // ScrollControlIntoView guarantees the row is on screen; the pass below centres it.
        _list.ScrollControlIntoView(row);

        int current = -_list.AutoScrollPosition.Y;
        int max = Math.Max(0, _list.AutoScrollMinSize.Height - room);

        // AutoScroll shifts a child's bounds as the list scrolls, so its Top is already offset by the
        // current position. The target has to be measured from where the row really sits.
        int logicalTop = row.Top + current;
        int target = Math.Clamp(logicalTop - ((room - row.Height) / 2), 0, max);

        // Small moves are left alone, so a queue of short jobs does not twitch the list on every row.
        if (current == target) return;

        _list.AutoScrollPosition = new Point(0, target);
    }

    /// <summary>
    /// Scrolls to the next failed package, wrapping around. The first call lands on the first failure, and
    /// the footer button keeps every failure one press away after the list has been scrolled elsewhere.
    /// </summary>
    private void ShowNextFailure()
    {
        if (_failures.Count == 0) return;

        _failCursor = (_failCursor + 1) % _failures.Count;

        int index = _failures[_failCursor];
        if (index < _rows.Count) Follow(_rows[index]);
    }

    private void SetOverall(int index, int percent)
    {
        int total = Math.Max(1, _jobs.Count);
        double value = (index + (percent / 100.0)) * 100.0 / total;

        _bar.Percent = Math.Clamp((int)Math.Round(value), 0, 100);
        _count.Text = $"{Math.Min(index + 1, _jobs.Count)} / {_jobs.Count}";
    }

    private void SetStatus(string text, Color tone)
    {
        _status.Text = text;
        _status.ForeColor = tone;
        _status.Invalidate();
    }

    private static void Begin(ToggleRow row)
    {
        row.Working = true;
        row.Progress = 0;
        row.BadgeText = "0%";
        row.BadgeTone = Theme.Accent;
        row.Invalidate();
    }

    private static void Report(ToggleRow row, int percent)
    {
        if (percent <= 0) return;

        row.Progress = percent;
        row.BadgeText = $"{percent}%";
    }

    private static void End(ToggleRow row, bool ok, bool stopped, string message = "")
    {
        row.Working = false;
        row.Progress = 0;
        row.BadgeText = stopped ? "STOPPED" : ok ? "DONE" : "FAILED";
        row.BadgeTone = stopped ? Theme.Warn : ok ? Theme.Good : Theme.Bad;

        // A failed row carries its reason, so the queue explains itself instead of just saying FAILED.
        if (!ok && !stopped && message.Length > 0)
        {
            row.Subtitle = Theme.Truncate(message.ToUpperInvariant(), 70);
        }

        row.Invalidate();
    }
}

/// <summary>Modal host for <see cref="DownloadPanel"/>, used when the updates column is not visible.</summary>
internal sealed partial class DownloadForm : Form
{
    private const int WmNcHitTest = 0x0084;
    private const int HtCaption = 2;
    private const int HtClient = 1;

    private readonly DownloadPanel _panel;

    public DownloadForm(string title, string kicker, IReadOnlyList<DownloadJob> jobs)
    {
        _panel = new DownloadPanel(title, kicker, jobs);

        InitializeComponent();

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 500);
        BackColor = Theme.Bg;
        Font = Theme.Ui;
        ForeColor = Theme.Text;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        KeyPreview = true;

        MinimumSize = new Size(360, 320);
        Padding = new Padding(2);

        Controls.Add(_panel);
        _panel.Dock = DockStyle.Fill;

        _panel.CloseRequested += Close;
    }

    /// <summary>What each job reported, in queue order.</summary>
    public IReadOnlyList<DownloadResult> Results => _panel.Results;

    /// <summary>True once the failure offer was answered with REMOVE.</summary>
    public bool RemoveRequested => _panel.RemoveRequested;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _panel.Start();
    }

    /// <summary>
    /// Closing is never refused: a pass still running is cancelled on the way out, so the window can always
    /// be left whether the installer cooperates or not.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_panel.Running) _panel.RequestCancel();

        base.OnFormClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Escape) Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var frame = new RectangleF(1f, 1f, Width - 2f, Height - 2f);
        using GraphicsPath outline = RoundPath(frame, 12f);
        using var edge = new Pen(Theme.Alpha(Theme.Accent, 215), 2f);
        g.DrawPath(edge, outline);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WmNcHitTest && (int)m.Result == HtClient) m.Result = HtCaption;
    }

    /// <summary>Rounded rectangle, so the frame follows the window's own outline.</summary>
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
}
