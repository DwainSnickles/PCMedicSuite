namespace PC_Medic;

internal sealed partial class TweaksPage : PageBase
{
    private static readonly string[] AppearanceKeys = { "visual", "transparency", "background" };
    private static readonly string[] SystemKeys = { "gamedvr", "telemetry", "hibernate" };
    private static readonly string[] GamingKeys = { "gamemode", "netthrottle", "netlatency", "cpupriority", "hags" };

    private readonly List<(Tweak Tweak, ToggleRow Row)> _bindings = new();
    private bool _loading;

    public TweaksPage()
        : base("TWEAKS", "Reversible performance switches \u2014 each one is a registry value or a powercfg flag.")
    {
        InitializeComponent();

        ToggleRow[] appearanceRows = CreateRows(_appearance, AppearanceKeys, _appearanceNote);
        ToggleRow[] systemRows = CreateRows(_system, SystemKeys, _systemNote);
        ToggleRow[] gamingRows = CreateRows(_gaming, GamingKeys, _gamingNote);

        LayoutCard(_appearance, appearanceRows, _appearanceNote);
        _appearance.Resize += (_, _) => LayoutCard(_appearance, appearanceRows, _appearanceNote);

        LayoutCard(_system, systemRows, _systemNote);
        _system.Resize += (_, _) => LayoutCard(_system, systemRows, _systemNote);

        LayoutCard(_gaming, gamingRows, _gamingNote);
        _gaming.Resize += (_, _) => LayoutCard(_gaming, gamingRows, _gamingNote);

        void LayoutGodMode()
        {
            int buttonWidth = Math.Min(190, Math.Max(120, _godMode.ClientSize.Width - 36));
            _godModeButton.SetBounds(18, 56, buttonWidth, 34);

            int left = _godModeButton.Right + 16;
            _godModeNote.SetBounds(left, 60, Math.Max(80, _godMode.ClientSize.Width - left - 18), 40);
        }

        _godMode.Resize += (_, _) => LayoutGodMode();
        LayoutGodMode();

        _footer.Resize += (_, _) =>
        {
            _status.Location = new Point(2, 9);
            _rescan.Location = new Point(Math.Max(0, _footer.ClientSize.Width - _rescan.Width), 2);
            _revert.Location = new Point(Math.Max(0, _rescan.Left - _revert.Width - 8), 2);
            _history.Location = new Point(Math.Max(0, _revert.Left - _history.Width - 8), 2);
        };

        _rescan.Click += (_, _) => LoadStates();
        _history.Click += (_, _) => ShowHistory();
        _revert.Click += RevertAll;
    }

    public override void OnNavigated() => LoadStates();

    public override void RefreshData() => LoadStates();

    private ToggleRow[] CreateRows(Card card, IEnumerable<string> keys, Label note)
    {
        var rows = new List<ToggleRow>();

        foreach (string key in keys)
        {
            Tweak tweak = TweakService.All.Single(candidate => candidate.Key == key);

            var row = new ToggleRow
            {
                Title = tweak.Title,
                Subtitle = tweak.Subtitle,
                BadgeText = tweak.Badge,
                BadgeTone = tweak.RequiresAdmin ? Theme.Warn : Theme.Accent,
                Tone = tweak.Tone,
            };

            row.Toggled += (sender, on) => OnToggled(sender, on);
            card.Controls.Add(row);
            _bindings.Add((tweak, row));
            rows.Add(row);
        }

        card.Controls.Add(note);
        return rows.ToArray();
    }

    private static void LayoutCard(Card card, IReadOnlyList<ToggleRow> rows, Label note)
    {
        int width = Math.Max(240, card.ClientSize.Width - 36);
        int y = 58;

        foreach (ToggleRow row in rows)
        {
            row.SetBounds(18, y, width, 62);
            y += 68;
        }

        note.SetBounds(18, y + 2, width, 42);
    }

    private async void OnToggled(ToggleRow row, bool on)
    {
        if (_loading) return;

        Tweak? tweak = null;
        foreach ((Tweak Candidate, ToggleRow Bound) in _bindings)
        {
            if (Bound == row)
            {
                tweak = Candidate;
                break;
            }
        }

        if (tweak is null) return;

        if (tweak.RequiresAdmin && !Native.IsElevated)
        {
            row.Checked = !on;
            row.Invalidate();
            SetStatus($"{tweak.Title}: administrator rights required \u2014 use RESTART AS ADMIN.", Theme.Warn);
            ActivityLog.Warn($"{tweak.Title}: needs an elevated run.");
            return;
        }

        if (on && !tweak.IsApplied() && !TweakBackup.Capture(tweak))
        {
            row.Checked = false;
            row.Invalidate();
            SetStatus($"{tweak.Title}: original value could not be recorded \u2014 change cancelled.", Theme.Bad);
            ActivityLog.Error($"{tweak.Title}: no snapshot could be written.");
            return;
        }

        TweakChange change = TweakBackup.Prepare(tweak, on);

        row.Enabled = false;
        SetStatus($"{tweak.Title.ToLowerInvariant()} \u2014 working\u2026", Theme.TextDim);

        TweakOutcome outcome = await Task.Run(() => tweak.Apply(on));
        if (IsDisposed) return;

        if (outcome.Ok) TweakBackup.Record(change, outcome);

        row.Enabled = true;
        ActivityLog.Write(outcome.Ok ? LogLevel.Success : LogLevel.Error, $"{tweak.Title}: {outcome.Message}");
        SetStatus(outcome.Ok ? outcome.Message : $"{tweak.Title}: {outcome.Message}", outcome.Ok ? Theme.Good : Theme.Bad);

        LoadStates();
    }

    private async void RevertAll(object? sender, EventArgs e)
    {
        _revert.Enabled = false;
        SetStatus("Restoring captured values\u2026", Theme.TextDim);

        var result = await Task.Run(() => TweakBackup.RevertAll(TweakService.All));
        if (IsDisposed) return;

        _revert.Enabled = true;

        LogLevel level = result.Restored > 0 ? LogLevel.Success : LogLevel.Warn;
        ActivityLog.Write(level, $"Tweak revert: {result.Message}");

        foreach (string failure in result.Failures)
        {
            ActivityLog.Warn($"Tweak revert skipped \u2014 {failure}");
        }

        SetStatus(result.Message, result.Restored > 0 && result.Failures.Count == 0 ? Theme.Good : Theme.Warn);
        LoadStates();
    }

    private void ShowHistory()
    {
        using var dialog = new TweakHistoryForm();
        dialog.ShowDialog(this);
        LoadStates();
    }

    private void SetStatus(string text, Color tone)
    {
        _status.Text = text;
        _status.ForeColor = tone;
    }

    private async void LoadStates()
    {
        if (_loading) return;
        _loading = true;

        try
        {
            var snapshot = await Task.Run(() =>
            {
                var states = new List<(ToggleRow Row, bool Applied)>();

                foreach ((Tweak tweak, ToggleRow row) in _bindings)
                {
                    states.Add((row, tweak.IsApplied()));
                }

                return (States: states, Snapshots: TweakBackup.Count, Changes: TweakBackup.HistoryCount);
            });

            if (IsDisposed) return;

            int active = 0;
            foreach ((ToggleRow row, bool applied) in snapshot.States)
            {
                row.Checked = applied;
                row.Invalidate();
                if (applied) active++;
            }

            _revert.Enabled = snapshot.Snapshots > 0;

            SetStatus(
                $"{active} OF {snapshot.States.Count} TWEAKS ACTIVE  \u2022  {snapshot.Snapshots} SNAPSHOT" +
                (snapshot.Snapshots == 1 ? string.Empty : "S") +
                $"  \u2022  {snapshot.Changes} CHANGE" + (snapshot.Changes == 1 ? string.Empty : "S") + "  \u2022  " +
                (Native.IsElevated ? "ELEVATED" : "STANDARD USER"),
                active > 0 ? Theme.Good : Theme.TextDim);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Tweak state read failed: {ex.Message}");
            SetStatus("Tweak state could not be read.", Theme.Bad);
        }
        finally
        {
            _loading = false;
        }
    }
}
