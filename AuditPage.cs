namespace PC_Medic;

/// <summary>
/// The two audits on one page: provably orphaned registry keys on the left, dead shortcut targets on
/// the right. Each half keeps its own scan, selection, backup and plate; nothing is shared but the page.
/// </summary>
internal sealed partial class AuditPage : PageBase
{
    private readonly List<(ToggleRow Row, RegistryIssue Issue)> _registryRows = new();
    private readonly List<(ToggleRow Row, ShortcutEntry Entry)> _shortcutRows = new();

    private List<RegistryIssue> _issues = new();
    private ShortcutScan _scan = new(Array.Empty<ShortcutEntry>(), 0, 0, 0);
    private bool _busyRegistry;
    private bool _busyShortcuts;
    private bool _shortcutsScanned;

    public AuditPage()
        : base(
            "REGISTRY AUDIT",
            "Orphaned registry keys and dead shortcuts, removed per item with a backup first.")
    {
        InitializeComponent();

        _footer.Text =
            $"BACKUPS  {Theme.Truncate(RegistryService.BackupRoot, 76)}" +
            "  •  RECYCLED SHORTCUTS RESTORE FROM THE BIN";
        _tips.SetToolTip(
            _footer,
            $"{RegistryService.BackupRoot}\r\nRecycled shortcuts restore from the Recycle Bin; kept shortcuts are remembered.");

        void LayoutRegistryToolbar()
        {
            NeonButton[] buttons = { _registryScan, _registryRestore, _registryRemove, _registryAll };
            int right = _registryToolbar.ClientSize.Width;
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                right -= buttons[i].Width;
                buttons[i].SetBounds(right, 26, buttons[i].Width, 28);
                right -= 8;
            }
            _registryStatus.SetBounds(2, 4, Math.Max(90, _registryToolbar.ClientSize.Width - 6), 18);
        }

        _registryToolbar.Resize += (_, _) => LayoutRegistryToolbar();
        LayoutRegistryToolbar();

        void LayoutShortcutToolbar()
        {
            NeonButton[] buttons = { _shortcutScan, _shortcutRecycle, _shortcutKeep, _shortcutAll };
            int right = _shortcutToolbar.ClientSize.Width;
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                right -= buttons[i].Width;
                buttons[i].SetBounds(right, 26, buttons[i].Width, 28);
                right -= 8;
            }
            _shortcutStatus.SetBounds(2, 4, Math.Max(90, _shortcutToolbar.ClientSize.Width - 6), 18);
        }

        _shortcutToolbar.Resize += (_, _) => LayoutShortcutToolbar();
        LayoutShortcutToolbar();

        void LayoutRegistryLoading()
        {
            if (_registryCard.IsDisposed || _registryList.IsDisposed || !_registryList.IsHandleCreated) return;
            _registryLoading.Bounds = new Rectangle(
                _registryCard.PointToClient(_registryList.PointToScreen(Point.Empty)), _registryList.Size);
        }

        _registryList.SizeChanged += (_, _) => LayoutRegistryLoading();
        _registryCard.Layout += (_, _) => LayoutRegistryLoading();
        LayoutRegistryLoading();
        _registryLoading.BringToFront();

        void LayoutShortcutLoading()
        {
            if (_shortcutCard.IsDisposed || _shortcutList.IsDisposed || !_shortcutList.IsHandleCreated) return;
            _shortcutLoading.Bounds = new Rectangle(
                _shortcutCard.PointToClient(_shortcutList.PointToScreen(Point.Empty)), _shortcutList.Size);
        }

        _shortcutList.SizeChanged += (_, _) => LayoutShortcutLoading();
        _shortcutCard.Layout += (_, _) => LayoutShortcutLoading();
        LayoutShortcutLoading();
        _shortcutLoading.BringToFront();

        _registryScan.Click += (_, _) => ReloadRegistry(announce: true);
        _registryAll.Click += (_, _) => ToggleAllRegistry();
        _registryRemove.Click += (_, _) => RemoveRegistrySelection();
        _registryRestore.Click += (_, _) => RestoreRegistryBackup();

        _shortcutScan.Click += (_, _) => ReloadShortcuts(announce: true);
        _shortcutAll.Click += (_, _) => ToggleAllShortcuts();
        _shortcutKeep.Click += (_, _) => KeepShortcuts();
        _shortcutRecycle.Click += (_, _) => RecycleShortcuts();

        _registryList.Resize += (_, _) => ResizeRows(_registryList);
        _shortcutList.Resize += (_, _) => ResizeRows(_shortcutList);

        SyncRegistryButtons();
        SyncShortcutButtons();
    }

    /// <summary>The registry half rescans on every visit; the shortcut walk is reused once it has run.</summary>
    public override void OnNavigated()
    {
        ReloadRegistry(announce: false);

        if (_shortcutsScanned) return;
        _shortcutsScanned = true;
        ReloadShortcuts(announce: false);
    }

    public override void RefreshData() => OnNavigated();

    private static void ResizeRows(Panel list)
    {
        foreach (Control control in list.Controls)
        {
            if (control is ToggleRow row) row.Width = Math.Max(200, list.ClientSize.Width - 24);
        }
    }

    private static int RowWidth(Panel list) => Math.Max(200, list.ClientSize.Width - 24);

    // ---------------------------------------------------------------- registry half

    private async void ReloadRegistry(bool announce, string? note = null)
    {
        if (_busyRegistry || IsDisposed) return;

        _busyRegistry = true;
        SetRegistryButtons(false);
        _registryStatus.Text = "SCANNING HKLM + HKCU\u2026";
        _registryStatus.ForeColor = Theme.TextDim;
        using IDisposable busy = Working();

        var progress = new Progress<int>(percent =>
        {
            if (!IsDisposed) _registryLoading.Percent = percent;
        });

        _registryLoading.Percent = 0;
        _registryLoading.Visible = true;
        _registryLoading.BringToFront();

        try
        {
            List<RegistryIssue> issues = await Task.Run(() => RegistryService.Scan(progress));
            _issues = issues;
            RebuildRegistry();

            if (announce)
            {
                ActivityLog.Info(
                    $"Registry audit: {issues.Count} findings, {issues.Count(issue => !issue.ReviewOnly)} orphaned.");
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Registry audit failed: {ex.Message}");
            note = ex.Message;
        }
        finally
        {
            _registryLoading.Visible = false;
            _busyRegistry = false;
            SetRegistryButtons(true);
            UpdateRegistryStatus(note);
        }
    }

    private void RebuildRegistry()
    {
        _registryList.SuspendLayout();
        foreach (Control control in _registryList.Controls.Cast<Control>().ToArray()) control.Dispose();
        _registryList.Controls.Clear();
        _registryRows.Clear();

        int y = 6;

        if (_issues.Count == 0)
        {
            y = AddEmpty(_registryList, "No orphaned keys found \u2014 nothing here needs touching.", y);
        }

        foreach (IGrouping<RegistryCategory, RegistryIssue> group in _issues.GroupBy(issue => issue.Category))
        {
            AddGroupHeader(_registryList, $"{group.First().CategoryLabel}  \u2022  {group.Count()}", ref y);

            foreach (RegistryIssue issue in group)
            {
                ToggleRow row = BuildRegistryRow(issue);
                row.SetBounds(4, y, RowWidth(_registryList), 62);
                _registryList.Controls.Add(row);
                _registryRows.Add((row, issue));
                y += 68;
            }

            y += 6;
        }

        _registryList.AutoScrollMinSize = new Size(0, y + 6);
        _registryList.ResumeLayout();
        SyncRegistryButtons();
    }

    private ToggleRow BuildRegistryRow(RegistryIssue issue)
    {
        string badge = issue.Category switch
        {
            RegistryCategory.Uninstall => "KEY",
            RegistryCategory.AppPath => "PATH",
            RegistryCategory.SharedDll => "DLL",
            _ => "RUN",
        };

        var row = new ToggleRow
        {
            Title = Theme.Truncate(issue.Display, 48),
            Subtitle = $"MISSING  {Theme.Truncate(issue.MissingPath, 80)}",
            BadgeText = issue.ReviewOnly ? "REVIEW" : badge,
            BadgeTone = issue.ReviewOnly ? Theme.TextFaint : issue.CategoryTone,
            Tone = issue.CategoryTone,
            // Verified dead: the file, folder or key target is gone from a mounted fixed drive, so the
            // switch starts on and REMOVE is armed. Review-only rows stay off and untickable.
            Checked = !issue.ReviewOnly,
            Enabled = !issue.ReviewOnly,
            Tag = issue,
        };

        row.Toggled += (_, _) => SyncRegistryButtons();

        _tips.SetToolTip(
            row,
            $"{issue.Display}\r\n{issue.RegistryPath}\r\nMissing: {issue.MissingPath}" +
            (issue.ReviewOnly
                ? "\r\nReported only \u2014 manage this on the Startup Apps page."
                : "\r\nVerified missing on a mounted fixed drive, so it starts ticked for removal."));

        return row;
    }

    private void ToggleAllRegistry()
    {
        List<ToggleRow> rows = _registryRows
            .Where(pair => !pair.Issue.ReviewOnly)
            .Select(pair => pair.Row)
            .ToList();

        if (rows.Count == 0) return;

        bool select = rows.Any(row => !row.Checked);
        foreach (ToggleRow row in rows) row.Checked = select;
        SyncRegistryButtons();
    }

    private void RemoveRegistrySelection()
    {
        List<RegistryIssue> selected = _registryRows
            .Where(pair => pair.Row.Checked)
            .Select(pair => pair.Issue)
            .ToList();

        if (selected.Count == 0)
        {
            UpdateRegistryStatus("tick the entries to remove first", Theme.Warn);
            return;
        }

        (bool ok, string message) = RegistryService.Remove(selected);

        if (!ok)
        {
            ActivityLog.Error($"Registry clean: {message}");
            UpdateRegistryStatus(message, Theme.Bad);
            return;
        }

        ActivityLog.Success($"Registry clean: {message}");
        ReloadRegistry(announce: false, note: message);
    }

    private void RestoreRegistryBackup()
    {
        (bool ok, string message) = RegistryService.RestoreLatest();

        if (ok) ActivityLog.Success($"Registry restore: {message}");
        else ActivityLog.Warn($"Registry restore: {message}");

        ReloadRegistry(announce: false, note: message);

        if (!ok) UpdateRegistryStatus(message, Theme.Warn);
    }

    private void SetRegistryButtons(bool enabled)
    {
        _registryScan.Enabled = enabled;
        _registryAll.Enabled = enabled;
        _registryRestore.Enabled = enabled && RegistryService.LatestBackupFolder() is not null;
        SyncRegistryButtons();
    }

    private void SyncRegistryButtons()
    {
        int selected = _registryRows.Count(pair => pair.Row.Checked);
        _registryRemove.Text = selected == 0 ? "REMOVE" : $"REMOVE ({selected})";
        _registryRemove.Enabled = !_busyRegistry && selected > 0;
        _registryRemove.Invalidate();
        _tips.SetToolTip(_registryRemove, "Writes a .reg backup, then removes the ticked keys.");
    }

    private void UpdateRegistryStatus(string? note = null, Color? tone = null)
    {
        int orphaned = _issues.Count(issue => !issue.ReviewOnly);
        int review = _issues.Count - orphaned;

        _registryStatus.Text =
            $"{_issues.Count} FINDINGS  \u2022  {orphaned} ORPHANED  \u2022  {review} REVIEW" +
            (string.IsNullOrEmpty(note) ? string.Empty : $"  \u2022  {Theme.Truncate(note.ToUpperInvariant(), 26)}");

        _registryStatus.ForeColor = tone ?? Theme.TextDim;
        _tips.SetToolTip(
            _registryStatus,
            $"{_issues.Count} findings, {orphaned} provably orphaned, {review} review only\r\n" +
            (Native.IsElevated ? "ELEVATED SESSION" : "STANDARD USER") +
            (string.IsNullOrEmpty(note) ? string.Empty : $"\r\n{note}"));
    }

    // ---------------------------------------------------------------- shortcut half

    private async void ReloadShortcuts(bool announce, string? note = null)
    {
        if (_busyShortcuts || IsDisposed) return;

        _busyShortcuts = true;
        SetShortcutButtons(false);
        _shortcutStatus.Text = "RESOLVING .LNK TARGETS\u2026";
        _shortcutStatus.ForeColor = Theme.TextDim;
        using IDisposable busy = Working();

        _shortcutLoading.Percent = 0;
        _shortcutLoading.Visible = true;
        _shortcutLoading.BringToFront();

        var progress = new Progress<int>(percent =>
        {
            if (!IsDisposed) _shortcutLoading.Percent = percent;
        });

        try
        {
            ShortcutScan scan = await ShortcutService.ScanAsync(progress);
            _scan = scan;
            RebuildShortcuts();

            if (announce)
            {
                int dead = scan.Entries.Count(entry => entry.Status == ShortcutStatus.Broken);
                ActivityLog.Info($"Shortcut audit: {scan.Scanned} resolved, {dead} dead, {scan.Healthy} healthy.");
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Shortcut audit failed: {ex.Message}");
            note = ex.Message;
        }
        finally
        {
            _shortcutLoading.Visible = false;
            _busyShortcuts = false;
            SetShortcutButtons(true);
            UpdateShortcutStatus(note);
        }
    }

    private void RebuildShortcuts()
    {
        _shortcutList.SuspendLayout();
        foreach (Control control in _shortcutList.Controls.Cast<Control>().ToArray()) control.Dispose();
        _shortcutList.Controls.Clear();
        _shortcutRows.Clear();

        int y = 6;

        if (_scan.Entries.Count == 0)
        {
            y = AddEmpty(_shortcutList, "No dead shortcuts found \u2014 every .lnk still resolves.", y);
        }

        foreach (IGrouping<string, ShortcutEntry> group in _scan.Entries.GroupBy(entry => entry.Area))
        {
            AddGroupHeader(_shortcutList, $"{group.Key}  \u2022  {group.Count()}", ref y);

            foreach (ShortcutEntry entry in group)
            {
                ToggleRow row = BuildShortcutRow(entry);
                row.SetBounds(4, y, RowWidth(_shortcutList), 62);
                _shortcutList.Controls.Add(row);
                _shortcutRows.Add((row, entry));
                y += 68;
            }

            y += 6;
        }

        _shortcutList.AutoScrollMinSize = new Size(0, y + 6);
        _shortcutList.ResumeLayout();
        SyncShortcutButtons();
    }

    private ToggleRow BuildShortcutRow(ShortcutEntry entry)
    {
        bool dead = entry.Status == ShortcutStatus.Broken;

        var row = new ToggleRow
        {
            Title = Theme.Truncate(entry.Name, 48),
            Subtitle = dead
                ? $"MISSING  {Theme.Truncate(entry.Target, 80)}"
                : $"REVIEW  {Theme.Truncate(entry.Detail, 80)}",
            BadgeText = dead ? "DEAD" : "REVIEW",
            BadgeTone = dead ? Theme.Bad : Theme.TextDim,
            // A ticked review row is the operator's judgement rather than a proven finding, so it lights
            // amber instead of red and stays distinguishable from a dead row at a glance.
            Tone = dead ? Theme.Bad : Theme.Warn,
            Checked = false,
            // Every row can be ticked: a review row is the operator's call to make, so the switch has
            // to move once they have looked at the folder.
            Locked = false,
            Clickable = true,
            Tag = entry,
        };

        row.Toggled += (_, _) => SyncShortcutButtons();
        row.RowClicked += _ => OpenFolder(entry);

        // The subtitle ellipsises the reason, so the tooltip carries all of it: a review row says what
        // stopped it resolving, not just that it did not.
        string reason = string.IsNullOrWhiteSpace(entry.Detail) ? "the target could not be resolved" : entry.Detail;
        string details = dead
            ? $"{entry.FilePath}\r\nTarget: {entry.Target}" +
              "\r\nClick the row to open its folder; tick it to recycle it or mark it not dead."
            : $"{entry.FilePath}\r\nREVIEW: {reason}" +
              "\r\nNo target was resolved: click the row to open its folder, then tick it to recycle it or mark it not dead.";

        _tips.SetToolTip(row, details);

        return row;
    }

    private void ToggleAllShortcuts()
    {
        // Review rows are included: seeing them is the review, and the operator decides whether to
        // recycle an unresolved link or mark it as still wanted.
        List<ToggleRow> rows = _shortcutRows.Select(pair => pair.Row).ToList();

        if (rows.Count == 0) return;

        bool select = rows.Any(row => !row.Checked);
        foreach (ToggleRow row in rows) row.Checked = select;
        SyncShortcutButtons();
    }

    /// <summary>Clicking a row opens its folder, so the entry can be checked by eye before a decision.</summary>
    private static void OpenFolder(ShortcutEntry entry)
    {
        string? folder = Path.GetDirectoryName(entry.FilePath);

        if (folder is null || !Native.OpenShell(folder))
        {
            ActivityLog.Warn($"The folder for {entry.Name} could not be opened.");
            return;
        }

        ActivityLog.Info($"Opened {folder} for {entry.Name}.");
    }

    /// <summary>Marks ticked entries as fine and drops them from the list without rescanning.</summary>
    private void KeepShortcuts()
    {
        List<ShortcutEntry> selected = _shortcutRows.Where(pair => pair.Row.Checked).Select(pair => pair.Entry).ToList();

        if (selected.Count == 0)
        {
            UpdateShortcutStatus("tick the shortcuts that still work first", Theme.Warn);
            return;
        }

        (bool ok, string message) = ShortcutService.Keep(selected);
        ActivityLog.Write(ok ? LogLevel.Success : LogLevel.Warn, message);

        if (!ok)
        {
            UpdateShortcutStatus(message, Theme.Bad);
            return;
        }

        _scan = new ShortcutScan(
            _scan.Entries.Where(entry => !selected.Contains(entry)).ToList(),
            _scan.Scanned,
            _scan.Healthy,
            _scan.Suppressed + selected.Count);

        RebuildShortcuts();
        UpdateShortcutStatus(message, Theme.Good);
    }

    private void RecycleShortcuts()
    {
        List<ShortcutEntry> selected = _shortcutRows.Where(pair => pair.Row.Checked).Select(pair => pair.Entry).ToList();

        if (selected.Count == 0)
        {
            UpdateShortcutStatus("tick the shortcuts to recycle first", Theme.Warn);
            return;
        }

        (bool ok, string message) = ShortcutService.Recycle(selected);

        if (!ok)
        {
            ActivityLog.Error($"Shortcut clean: {message}");
            UpdateShortcutStatus(message, Theme.Bad);
            return;
        }

        ActivityLog.Success($"Shortcut clean: {message}");
        ReloadShortcuts(announce: false, note: message);
    }

    private void SetShortcutButtons(bool enabled)
    {
        _shortcutScan.Enabled = enabled;
        _shortcutAll.Enabled = enabled;
        SyncShortcutButtons();
    }

    private void SyncShortcutButtons()
    {
        int selected = _shortcutRows.Count(pair => pair.Row.Checked);
        _shortcutRecycle.Text = selected == 0 ? "RECYCLE" : $"RECYCLE ({selected})";
        _shortcutKeep.Text = selected == 0 ? "NOT DEAD" : $"NOT DEAD ({selected})";
        _shortcutRecycle.Enabled = !_busyShortcuts && selected > 0;
        _shortcutKeep.Enabled = !_busyShortcuts && selected > 0;
        _shortcutRecycle.Invalidate();
        _shortcutKeep.Invalidate();
        _tips.SetToolTip(_shortcutRecycle, "Sends the ticked shortcuts to the Recycle Bin.");
        _tips.SetToolTip(_shortcutKeep, "Remembers the ticked shortcuts as working and hides them from later scans.");
    }

    private void UpdateShortcutStatus(string? note = null, Color? tone = null)
    {
        int dead = _scan.Entries.Count(entry => entry.Status == ShortcutStatus.Broken);
        int review = _scan.Entries.Count - dead;

        _shortcutStatus.Text =
            $"{dead} DEAD  \u2022  {review} REVIEW  \u2022  {_scan.Suppressed} KEPT" +
            (string.IsNullOrEmpty(note) ? string.Empty : $"  \u2022  {Theme.Truncate(note.ToUpperInvariant(), 22)}");

        _shortcutStatus.ForeColor = tone ?? Theme.TextDim;
        _tips.SetToolTip(
            _shortcutStatus,
            $"{_scan.Scanned} shortcuts resolved, {_scan.Healthy} healthy\r\n{_shortcutStatus.Text}");
    }

    // ---------------------------------------------------------------- shared list furniture

    private static int AddEmpty(Panel list, string text, int y)
    {
        var label = new Label
        {
            Text = text,
            Font = Theme.MonoSmall,
            ForeColor = Theme.TextDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(8, y + 8),
        };

        list.Controls.Add(label);
        return y + 40;
    }

    private static void AddGroupHeader(Panel list, string text, ref int y)
    {
        var header = new Label
        {
            Text = text,
            Font = Theme.Kicker,
            ForeColor = Theme.TextDim,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(8, y + 4),
        };

        list.Controls.Add(header);
        y += 26;
    }
}
