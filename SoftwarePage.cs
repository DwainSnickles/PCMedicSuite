using System.Drawing.Drawing2D;

namespace PC_Medic;

/// <summary>
/// Software page: the SoftwareUpdater feature set ported into PC Medic — the full
/// installed-software inventory, update candidates with security flags, version pins
/// and ignore rules, sequential install/update/uninstall runs through InstallRunner,
/// and the updater settings (scan schedule, sources, install behaviour, exclusions,
/// diagnostics export). Registry inventory, winget upgrade checks and the catalogue
/// all feed the same two lists.
/// </summary>
internal sealed partial class SoftwarePage : PageBase
{
    private readonly List<Action> _toolbarLayouts = new();
    private Control? _settingsContent;


    private readonly List<(ToggleRow Row, UpdateCandidate Candidate)> _updateRows = new();
    private readonly List<(ToggleRow Row, InstalledApp App)> _installedRows = new();
    private readonly Dictionary<string, ToggleRow> _updateRowById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ToggleRow> _installedRowById = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(ToggleRow Row, Func<bool> Get)> _settingRows = new();

    private SettingsService? _settings;
    private ActivityService? _activity;
    private WingetClient? _winget;
    private InventoryService? _inventory;
    private WingetBootstrapper? _bootstrap;
    private InstallRunner? _runner;

    private List<UpdateCandidate> _updates = new();
    private List<InstalledApp> _installed = new();
    private List<ProgramEntry> _programEntries = new();
    private readonly Dictionary<string, ProgramEntry> _entryByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _deadNames = new(StringComparer.OrdinalIgnoreCase);
    private const string SearchHint = "SEARCH INSTALLED…";
    private string _filter = string.Empty;
    private string _installedSummary = "NOT SCANNED";
    private readonly List<ToggleRow> _findHits = new();
    private int _findCursor = -1;
    private int _installedSort;

    private static readonly string[] SortLabels = { "NAME", "NEWEST", "OLDEST", "PUBLISHER", "SIZE" };
    private bool _securityFilter;
    private bool _busyScan;
    private bool _installing;

    /// <summary>True while the failed-run offer is dispatching removals; stops removal failures re-prompting.</summary>
    private bool _removingFailed;
    private bool _initialized;
    private bool _wingetReady;
    private bool _wingetPrompted;
    private bool _firstVisit = true;
    private ToggleRow? _armedRow;

    private static readonly string[] ScheduleOptions =
    {
        "Manually only",
        "Every time PC Medic starts",
        "Daily at 03:00",
        "Weekly on Sunday",
    };

    public SoftwarePage()
        : base("SOFTWARE", "Installed software, update candidates, batch runs and updater settings.")
    {
        InitializeComponent();

        _cancelRun.Tone = Theme.Bad;
        _cancelRun.Visible = false;
        _securityOnly.Tone = Theme.TextDim;

        _settingsContent = _settingsInner;

        WireCollapse(_updatesCard, 0, 44, _updatesInner, _updatesOverlay);
        WireCollapse(_installedCard, 1, 34, _installedInner, _installedOverlay);
        WireCollapse(_settingsCard, 2, 22, _settingsInner, null);
        ApplyFoldStates();

        WireOverlay(_updatesCard, _updatesList, _updatesOverlay);
        WireOverlay(_installedCard, _installedList, _installedOverlay);

        // Borderless search plate: the host panel carries the surface colour the TextBox cannot and
        // paints its own edge, so the field still reads as a field on a toolbar full of buttons.
        _searchHost.BackColor = Theme.Surface2;
        _searchHost.Height = 26;
        _searchHost.Padding = new Padding(0);

        _search.BorderStyle = BorderStyle.None;
        _search.BackColor = Theme.Surface2;
        _search.ForeColor = Theme.Text;
        _search.Font = Theme.Ui;
        _search.Text = SearchHint;
        _search.ForeColor = Theme.Text;
        _searchHost.Controls.Add(_search);

        void LayoutSearch()
        {
            int height = _search.PreferredHeight;
            _search.SetBounds(30, Math.Max(2, (_searchHost.Height - height) / 2),
                              Math.Max(40, _searchHost.Width - 38), height);
        }

        _searchHost.Resize += (_, _) => LayoutSearch();
        LayoutSearch();

        _searchHost.Paint += (_, e) =>
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var plate = new RectangleF(0.5f, 0.5f, _searchHost.Width - 1f, _searchHost.Height - 1f);
            using GraphicsPath path = Theme.Round(plate, 6f);
            using var pen = new Pen(_search.Focused ? Theme.Alpha(Theme.Accent, 200) : Theme.Edge, 1f);
            g.DrawPath(pen, path);
        };

        WireToolbar(_updatesToolbar,
            new[] { _securityOnly, _selectAll, _scan, _updateSelected, _cancelRun, _bulkExes });
        // NEXT is placed last, so it lands leftmost: right beside the search plate it belongs to.
        WireToolbar(_installedToolbar,
            new Control[] { _sort, _cleanDead, _exportCsv, _exportHtml, _rescan, _findNext });
        WireToolbar(_settingsToolbar, new Control[] { _exportDiag, _refreshCatalogue });

        _scan.Click += (_, _) => _ = ScanAllAsync(announce: true);
        _rescan.Click += (_, _) => _ = ScanAllAsync(announce: true);
        _selectAll.Click += (_, _) => ToggleSelectAll();
        _securityOnly.Click += (_, _) =>
        {
            _securityFilter = !_securityFilter;
            _securityOnly.Tone = _securityFilter ? Theme.Warn : Theme.TextDim;
            _securityOnly.Invalidate();
            RebuildUpdates();
        };
        _updateSelected.Click += (_, _) => _ = UpdateSelectedAsync();
        _cancelRun.Click += (_, _) => _runner?.Cancel();
        _exportCsv.Click += (_, _) => ExportReport(csv: true);
        _exportHtml.Click += (_, _) => ExportReport(csv: false);
        _sortMenu.ShowImageMargin = false;
        _sortMenu.Font = Theme.MonoSmall;

        for (int i = 0; i < SortLabels.Length; i++)
        {
            int mode = i;
            var item = new ToolStripMenuItem($"SORT: {SortLabels[i]}")
            {
                ForeColor = Theme.Text,
                BackColor = Theme.Surface,
            };
            item.Click += (_, _) => SetSortMode(mode);
            _sortMenu.Items.Add(item);
        }

        _sort.Click += (_, _) => _sortMenu.Show(_sort, new Point(0, _sort.Height));
        SetSortMode(0);
        _exportDiag.Click += (_, _) => _ = ExportDiagnosticsAsync();
        _refreshCatalogue.Click += (_, _) => _ = RefreshCatalogueAsync();
        _cleanDead.Tone = Theme.Bad;
        _cleanDead.Click += (_, _) => CleanDeadEntries();
        _bulkExes.Click += (_, _) => OpenBulkInstaller();
        _tips.SetToolTip(
            _bulkExes,
            "Run a folder of installers (.exe/.msi/.msu) one at a time, with restarts held back until the last one finishes.");

        _search.GotFocus += (_, _) =>
        {
            if (_search.Text == SearchHint)
            {
                _search.Text = string.Empty;
                _search.ForeColor = Theme.Text;
            }

            _searchHost.Invalidate();
        };

        _search.LostFocus += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_search.Text))
            {
                _search.Text = SearchHint;
                _search.ForeColor = Theme.Text;
            }

            _searchHost.Invalidate();
        };

        // FIND rather than filter: the list keeps every row, lights the matches and jumps to the
        // first one. Enter (or NEXT) walks the rest and wraps at the end.
        _findNext.Tone = Theme.TextDim;
        _findNext.Enabled = false;
        _findNext.Click += (_, _) => StepFind();

        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_findHits.Count == 0) ApplyFind();
                else StepFind();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _search.Clear();
            }
        };

        _search.TextChanged += (_, _) =>
        {
            _filter = _search.Text == SearchHint ? string.Empty : _search.Text.Trim();
            ApplyFind();
        };

        _updatesList.Resize += (_, _) => ResizeRows(_updatesList);
        _installedList.Resize += (_, _) => ResizeRows(_installedList);
        _settingsList.Resize += (_, _) => ResizeRows(_settingsList);
        _exclusionList.Resize += (_, _) => ResizeRows(_exclusionList);

        _sweep.Tick += (_, _) =>
        {
            foreach ((ToggleRow row, _) in _updateRows) if (row.Working) row.Sweep();
            foreach ((ToggleRow row, _) in _installedRows) if (row.Working) row.Sweep();
        };

        _confirmTimer.Tick += (_, _) => DisarmConfirm();

        _settingsCard.CollapseGlyph = "\uE70E";
        _settingsCard.ExpandGlyph = "\uE70D";

        _updateStatus.Text = "NOT SCANNED";
        _installedStatus.Text = "NOT SCANNED";
        _settingsStatus.Text = "PREFERENCES \u2022 EXCLUSIONS";
        _exclusionHeader.Text = "EXCLUSION RULES";
    }

    public override void OnNavigated()
    {
        if (_firstVisit)
        {
            _firstVisit = false;
            _ = InitializeAsync();
            return;
        }

        RefreshData();
    }

    public override void RefreshData() => _ = ScanAllAsync(announce: false);

    /// <summary>
    /// A folder of installers already on disk — a downloaded Dell update set, MSI or MSU hotfixes —
    /// run in one pass. The window owns the queue, the silent switches and the deferred restarts.
    /// </summary>
    private void OpenBulkInstaller()
    {
        using var dialog = new BulkInstallForm();
        dialog.ShowDialog(FindForm());
    }

    /// <summary>Work started here keeps running off-page; the wait cursor must not follow the pointer.</summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) Busy.ReleaseWaitCursor();
    }

    // ------------------------------------------------------------------ layout

    private readonly record struct Foldable(Card Card, int Row, float Percent, Control? Content, ScanOverlay? Overlay);
    private readonly List<Foldable> _foldables = new();

    /// <summary>
    /// A scan overlay is a sibling of the card's inner table, sized to the list's rect so it
    /// never gets treated as a layout cell. Re-placed whenever either side moves.
    /// </summary>
    private static void WireOverlay(Card card, BufferedPanel list, ScanOverlay overlay)
    {
        void Layout()
        {
            if (card.IsDisposed || list.IsDisposed || !list.IsHandleCreated || !list.Visible) return;
            overlay.Bounds = new Rectangle(card.PointToClient(list.PointToScreen(Point.Empty)), list.Size);
        }

        list.SizeChanged += (_, _) => Layout();
        card.Layout += (_, _) => Layout();
        Layout();
        overlay.BringToFront();
    }

    /// <summary>
    /// A folded card keeps a 58px header strip; its row collapses to that height and the open
    /// cards split whatever is left, so minimizing actually removes the panel instead of
    /// leaving an empty shell behind the header. The last table row is an absorber that only
    /// takes space when every card is folded.
    /// </summary>
    private void WireCollapse(Card card, int row, float expandedPercent, Control? content, ScanOverlay? overlay)
    {
        _foldables.Add(new Foldable(card, row, expandedPercent, content, overlay));
        card.CollapsedChanged += (_, _) => ApplyFoldStates();
    }

    private void ApplyFoldStates()
    {
        bool allFolded = _foldables.Count > 0;

        foreach (Foldable fold in _foldables)
        {
            bool folded = fold.Card.Collapsed;
            allFolded &= folded;

            if (fold.Content is not null) fold.Content.Visible = !folded;
            if (folded && fold.Overlay is not null) fold.Overlay.Visible = false;

            _root.RowStyles[fold.Row] = folded
                ? new RowStyle(SizeType.Absolute, 58)
                : new RowStyle(SizeType.Percent, fold.Percent);
        }

        _root.RowStyles[3] = allFolded
            ? new RowStyle(SizeType.Percent, 100)
            : new RowStyle(SizeType.Absolute, 0);

        _root.PerformLayout();
    }

    /// <summary>Right-aligns the toolbar buttons and gives the status label the room that is left.</summary>
    private void WireToolbar(Panel toolbar, Control[] rightButtons)
    {
        void Layout()
        {
            int right = toolbar.ClientSize.Width;
            foreach (Control button in rightButtons)
            {
                if (!button.Visible) continue;
                right -= button.Width;
                button.SetBounds(right, 3, button.Width, 28);
                right -= 8;
            }

            Panel? searchHost = null;
            foreach (Control control in toolbar.Controls)
            {
                if (control is Panel host) searchHost = host;
            }

            // The search plate keeps a fixed width just left of the buttons; the status label gives
            // up exactly the room it took, so the field never hides behind a button.
            int plateWidth = searchHost is null ? 0 : Math.Min(250, Math.Max(160, right - 140));
            int plateLeft = searchHost is null ? right : Math.Max(70, right - plateWidth - 6);

            foreach (Control control in toolbar.Controls)
            {
                if (control is Label label)
                {
                    label.SetBounds(0, 8, Math.Max(60, plateLeft - 12), 18);
                }
                else if (control is Panel host)
                {
                    host.SetBounds(plateLeft, 4, plateWidth, 26);
                }
            }
        }

        toolbar.Resize += (_, _) => Layout();
        _toolbarLayouts.Add(Layout);
        Layout();
    }

    /// <summary>Re-places every toolbar's buttons — needed when CANCEL appears mid-run.</summary>
    private void RelayoutToolbars()
    {
        foreach (Action layout in _toolbarLayouts) layout();
    }

    // ------------------------------------------------------------------ services

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _settings = new SettingsService();
            _settings.Load();
            _settings.Changed += _ => OnSettingsChanged();

            _activity = new ActivityService();
            _activity.Load();
            _winget = new WingetClient();
            _inventory = new InventoryService(_winget, _settings);
            _bootstrap = new WingetBootstrapper(_winget);
            _runner = new InstallRunner(_winget, _settings, _activity);
            _runner.ItemChanged += OnRunnerItemChanged;
            _runner.LogLine += line => ActivityLog.Info(line);
            _runner.Completed += summary =>
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { BeginInvoke(new Action<RunSummary>(FinishRun), summary); }
                    catch (InvalidOperationException) { }
                    return;
                }
                FinishRun(summary);
            };

            BuildSettingsRows();
            RebuildExclusions();
            SyncSettings();

            // winget probes by spawning "winget --version"; keep that off the UI thread.
            _wingetReady = await Task.Run(() => _winget!.IsAvailable);
            if (!_wingetReady)
            {
                ActivityLog.Warn("winget was not found — update checks need 'App Installer' from the Microsoft Store.");
            }

            await ScanAllAsync(announce: false);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Software page failed to initialize: {ex}");
            _updateStatus.Text = "LOAD FAILED";
            _updateStatus.ForeColor = Theme.Bad;
        }
    }

    private void OnSettingsChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(OnSettingsChanged)); }
            catch (InvalidOperationException) { }
            return;
        }

        SyncSettings();
        RebuildExclusions();
    }

    // ------------------------------------------------------------------ scanning

    private async Task ScanAllAsync(bool announce)
    {
        if (_busyScan || _installing) return;
        if (!_initialized)
        {
            await InitializeAsync();
            return; // InitializeAsync runs the first scan itself.
        }

        if (_inventory is null || _settings is null) return;

        _busyScan = true;
        _scan.Enabled = false;
        _rescan.Enabled = false;
        SyncButtons();
        _updateStatus.Text = "SCANNING\u2026";
        _updateStatus.ForeColor = Theme.TextDim;
        _installedStatus.Text = "SCANNING\u2026";
        _installedStatus.ForeColor = Theme.TextDim;
        using IDisposable busy = Working();
        _updatesOverlay.Begin();
        _installedOverlay.Begin();

        try
        {
            await OfferWingetRepairAsync();

            var progress = new Progress<ScanProgress>(p =>
            {
                _updatesOverlay.Percent = p.Percent;
                _installedOverlay.Percent = p.Percent;
                _updateStatus.Text = $"{Theme.Truncate(p.Message.ToUpperInvariant(), 40)}  \u2022  {p.Percent}%";
            });

            (ScanResult result, List<ProgramEntry> programs) = await Task.Run(async () =>
            {
                ScanResult scanned = await _inventory!.ScanAsync(progress, CancellationToken.None);
                // The uninstall-registry pass is what proves an entry dead; it rides the same scan.
                List<ProgramEntry> entries = await Task.Run(ProgramsService.Load);
                return (scanned, entries);
            });

            _updates = result.Updates;
            _installed = result.Installed;
            _programEntries = programs;
            _entryByName.Clear();
            _deadNames.Clear();
            foreach (ProgramEntry entry in programs)
            {
                _entryByName.TryAdd(entry.DisplayName, entry);
                if (entry.Dead) _deadNames.Add(entry.DisplayName);
            }
            RebuildUpdates();
            RebuildInstalled();

            int security = _updates.Count(u => u.IsSecuritySensitive);
            _updateStatus.Text = _updates.Count == 0
                ? "EVERYTHING IS CURRENT"
                : $"{_updates.Count} UPDATES  \u2022  {security} SECURITY";
            _updateStatus.ForeColor = _updates.Count == 0 ? Theme.Good : Theme.Text;

            _installedSummary = $"{_installed.Count} PROGRAMS" +
                (_deadNames.Count > 0 ? $"  •  {_deadNames.Count} DEAD" : string.Empty);
            _installedStatus.Text = _installedSummary;
            _installedStatus.ForeColor = Theme.Text;

            if (announce)
            {
                ActivityLog.Info(
                    $"Software scan: {result.Installed.Count} installed, " +
                    $"{result.Updates.Count} update(s) available ({security} security).");
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Software scan failed: {ex}");
            _updateStatus.Text = "SCAN FAILED";
            _updateStatus.ForeColor = Theme.Bad;
            _installedSummary = "SCAN FAILED";
            _installedStatus.Text = _installedSummary;
            _installedStatus.ForeColor = Theme.Bad;
        }
        finally
        {
            _updatesOverlay.End();
            _installedOverlay.End();
            _busyScan = false;
            _scan.Enabled = true;
            _rescan.Enabled = true;
            SyncButtons();
        }
    }

    /// <summary>One repair offer per visit: re-register, then the PowerShell bootstrap, then reprobe.</summary>
    private async Task OfferWingetRepairAsync()
    {
        if (_settings is null || _winget is null || _bootstrap is null) return;
        if (!_settings.Current.UseWinget || _wingetReady || _wingetPrompted) return;

        _wingetPrompted = true;
        WingetCheck check = _bootstrap.Check();
        if (check.IsReady) { _wingetReady = true; return; }

        DialogResult answer = MessageBox.Show(
            this,
            check.Headline + "\n\n" + check.Detail +
            "\n\nTry to repair it now? (Re-register first, then the official bootstrap.)",
            "Windows Package Manager",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button1);

        if (answer != DialogResult.Yes) return;

        _updateStatus.Text = "REPAIRING WINGET\u2026";
        using IDisposable busy = Working();

        bool ok = await Task.Run(() => _bootstrap.TryReRegisterAsync());
        if (!ok) ok = await Task.Run(() => _bootstrap.TryRepairViaPowerShellAsync());

        _wingetReady = await Task.Run(() => _winget.IsAvailable);
        ActivityLog.Info(_wingetReady
            ? "winget repaired — resuming the scan."
            : "winget repair did not succeed — the registry inventory still loads without it.");
    }

    // ------------------------------------------------------------------ update rows

    private void RebuildUpdates()
    {
        if (_installing || _settings is null) return;

        _updatesList.SuspendLayout();
        foreach (Control control in _updatesList.Controls.Cast<Control>().ToArray()) control.Dispose();
        _updatesList.Controls.Clear();
        _updateRows.Clear();
        _updateRowById.Clear();

        int y = 6;
        int shown = 0;

        foreach (UpdateCandidate candidate in _updates)
        {
            if (_securityFilter && !candidate.IsSecuritySensitive) continue;
            if (_settings.IsIgnored("id:" + candidate.PackageId.ToLowerInvariant())) continue;

            ToggleRow row = BuildUpdateRow(candidate);
            row.SetBounds(8, y, RowWidth(_updatesList), 58);
            _updatesList.Controls.Add(row);
            _updateRows.Add((row, candidate));
            _updateRowById.TryAdd(candidate.PackageId, row);
            y += 64;
            shown++;
        }

        if (shown == 0)
        {
            _updatesList.Controls.Add(new Label
            {
                Text = _updates.Count == 0
                    ? "No updates available — everything winget tracks is current."
                    : "No updates match the current filter.",
                Font = Theme.MonoSmall,
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(12, 12),
            });
            y += 40;
        }

        _updatesList.AutoScrollMinSize = new Size(0, y + 10);
        _updatesList.ResumeLayout();
        SyncButtons();
    }

    private ToggleRow BuildUpdateRow(UpdateCandidate candidate)
    {
        (string badge, Color badgeTone) = candidate switch
        {
            { IsSecuritySensitive: true } => ("SECURITY", Theme.Warn),
            { IsPinned: true } => ("PINNED", Theme.Violet),
            { IsRunning: true } => ("RUNNING", Theme.Warn),
            _ => (string.Empty, Theme.TextDim),
        };

        var row = new ToggleRow
        {
            Title = Theme.Truncate(candidate.Name, 56),
            Subtitle = Theme.Truncate(UpdateSubtitle(candidate), 105),
            BadgeText = badge,
            BadgeTone = badgeTone,
            Tone = candidate.IsSecuritySensitive ? Theme.Warn : Theme.Accent,
            Checked = candidate.IsSelected,
            Clickable = true,
            ActionText = candidate.IsPinned ? "UNPIN" : "PIN",
            ActionTone = Theme.Violet,
            Tag = candidate,
        };

        row.Toggled += (_, _) =>
        {
            candidate.IsSelected = row.Checked;
            ClearFind();
            SyncButtons();
        };

        row.RowClicked += _ =>
        {
            row.Checked = !row.Checked;
            candidate.IsSelected = row.Checked;
            ClearFind();
            SyncButtons();
        };

        row.ActionClicked += _ => TogglePin(candidate);

        ContextMenuStrip menu = BuildCandidateMenu(candidate);
        row.ContextMenuStrip = menu;

        _tips.SetToolTip(row,
            $"{candidate.Name}\r\n{candidate.CurrentVersion} \u2192 {candidate.AvailableVersion}\r\n" +
            $"{candidate.Publisher ?? "\u2014"}  \u2022  {candidate.SourceName}\r\n" +
            $"{candidate.PackageId}\r\nRight-click for pin and ignore rules.");

        return row;
    }

    private static string UpdateSubtitle(UpdateCandidate c)
    {
        var parts = new List<string>
        {
            $"{c.CurrentVersion} \u2192 {c.AvailableVersion}",
            Theme.Truncate(c.Publisher ?? "\u2014", 32),
            c.SourceName,
            c.SizeDisplay,
        };

        if (c.IsMajorVersionChange) parts.Add("MAJOR");
        if (c.IsPinned) parts.Add($"PINNED {c.PinnedVersion}");
        if (c.IsRunning) parts.Add($"{Theme.Truncate(c.RunningProcessName ?? "APP", 18)} RUNNING");

        return string.Join("  \u2022  ", parts);
    }

    private ContextMenuStrip BuildCandidateMenu(UpdateCandidate candidate)
    {
        var menu = new ContextMenuStrip { BackColor = Theme.Surface, ForeColor = Theme.Text };
        menu.ShowImageMargin = false;
        menu.Font = Theme.MonoSmall;

        var pinItem = new ToolStripMenuItem { ForeColor = Theme.Text, BackColor = Theme.Surface };
        var ignoreItem = new ToolStripMenuItem { ForeColor = Theme.Text, BackColor = Theme.Surface };
        var updateOne = new ToolStripMenuItem("UPDATE THIS ONE") { ForeColor = Theme.Text, BackColor = Theme.Surface };

        menu.Items.Add(pinItem);
        menu.Items.Add(ignoreItem);
        menu.Items.Add(new ToolStripSeparator { ForeColor = Theme.Border, BackColor = Theme.Surface });
        menu.Items.Add(updateOne);

        menu.Opening += (_, _) =>
        {
            string key = "id:" + candidate.PackageId.ToLowerInvariant();
            bool pinned = _settings?.FindPin(key) is not null;
            bool ignored = _settings?.IsIgnored(key) ?? false;

            pinItem.Text = pinned ? "UNPIN \u2014 ALLOW UPDATES AGAIN" : "PIN TO CURRENT VERSION";
            pinItem.Enabled = !_installing;
            ignoreItem.Text = ignored ? "STOP IGNORING" : "IGNORE \u2014 HIDE FROM SCANS";
            ignoreItem.Enabled = !_installing;
            updateOne.Enabled = !_installing && !_busyScan && candidate.IsSelected;
        };

        pinItem.Click += (_, _) => TogglePin(candidate);
        ignoreItem.Click += (_, _) => ToggleIgnore(candidate);
        updateOne.Click += (_, _) => _ = UpdateSingleAsync(candidate);

        return menu;
    }

    private void TogglePin(UpdateCandidate candidate)
    {
        if (_settings is null) return;

        string key = "id:" + candidate.PackageId.ToLowerInvariant();
        ExclusionRule? pin = _settings.FindPin(key);

        if (pin is not null)
        {
            _settings.RemoveExclusion(pin);
            candidate.IsPinned = false;
            candidate.PinnedVersion = null;
            ActivityLog.Info($"{candidate.Name} unpinned — updates are offered again.");
        }
        else
        {
            _settings.AddExclusion(new ExclusionRule
            {
                Key = key,
                DisplayName = candidate.Name,
                Kind = ExclusionKind.Pin,
                PinnedVersion = candidate.CurrentVersion,
                Note = "Pinned from the Software page",
            });
            candidate.IsPinned = true;
            candidate.PinnedVersion = candidate.CurrentVersion;
            ActivityLog.Info($"{candidate.Name} pinned to {candidate.CurrentVersion}.");
        }

        RebuildUpdates();
    }

    private void ToggleIgnore(UpdateCandidate candidate)
    {
        if (_settings is null) return;

        string key = "id:" + candidate.PackageId.ToLowerInvariant();
        ExclusionRule? rule = _settings.Current.Exclusions.FirstOrDefault(e =>
            e.Kind == ExclusionKind.Ignore &&
            string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));

        if (rule is not null)
        {
            _settings.RemoveExclusion(rule);
            ActivityLog.Info($"{candidate.Name} is no longer ignored — rescan to see it again.");
        }
        else
        {
            _settings.AddExclusion(new ExclusionRule
            {
                Key = key,
                DisplayName = candidate.Name,
                Kind = ExclusionKind.Ignore,
                Note = "Ignored from the Software page",
            });
            _updates.RemoveAll(u =>
                string.Equals(u.PackageId, candidate.PackageId, StringComparison.OrdinalIgnoreCase));
            ActivityLog.Info($"{candidate.Name} ignored — hidden from scans and reports.");
        }

        RebuildUpdates();
    }

    // ------------------------------------------------------------------ installed rows

    private void RebuildInstalled()
    {
        if (_installing || _settings is null) return;

        _installedList.SuspendLayout();
        foreach (Control control in _installedList.Controls.Cast<Control>().ToArray()) control.Dispose();
        _installedList.Controls.Clear();
        _installedRows.Clear();
        _installedRowById.Clear();
        DisarmConfirm();

        int y = 6;
        int shown = 0;

        foreach (InstalledApp app in OrderedInstalled())
        {
            if (_settings.IsIgnored(app.Key)) continue;

            ToggleRow row = BuildInstalledRow(app);
            row.SetBounds(8, y, RowWidth(_installedList), 52);
            _installedList.Controls.Add(row);
            _installedRows.Add((row, app));
            if (!string.IsNullOrWhiteSpace(app.PackageId))
            {
                _installedRowById.TryAdd(app.PackageId, row);
            }
            y += 56;
            shown++;
        }

        if (shown == 0)
        {
            _installedList.Controls.Add(new Label
            {
                Text = "No installed programs were found.",
                Font = Theme.MonoSmall,
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(12, 12),
            });
            y += 40;
        }

        _installedList.AutoScrollMinSize = new Size(0, y + 10);
        _installedList.ResumeLayout();

        // Rows were rebuilt under an active search: relight its matches and put the count back.
        if (_filter.Length > 0) ApplyFind();
        else RestoreInstalledStatus();
    }

    // ------------------------------------------------------------------ find

    /// <summary>
    /// FIND on the installed list: every row stays in place, the matches are lit and the list jumps
    /// to the first one. Enter or NEXT walks the rest and wraps back to the top.
    /// </summary>
    private void ApplyFind()
    {
        _findHits.Clear();
        _findCursor = -1;

        if (_filter.Length > 0)
        {
            foreach ((ToggleRow row, InstalledApp app) in _installedRows)
            {
                if (MatchesFind(app, _filter)) _findHits.Add(row);
            }
        }

        foreach ((ToggleRow row, _) in _installedRows)
        {
            row.FindHit = false;
            row.FindCurrent = false;
        }

        foreach (ToggleRow row in _findHits) row.FindHit = true;

        _findNext.Enabled = _findHits.Count > 0;

        if (_findHits.Count == 0)
        {
            if (_filter.Length > 0)
            {
                _installedStatus.Text = $"NO MATCH  \u2022  {Theme.Truncate(_filter.ToUpperInvariant(), 26)}";
                _installedStatus.ForeColor = Theme.Warn;
            }
            else
            {
                RestoreInstalledStatus();
            }

            return;
        }

        StepFind();
    }

    /// <summary>Moves the find cursor on by one, wrapping, and centres the row it lands on.</summary>
    private void StepFind()
    {
        if (_findHits.Count == 0) return;

        _findCursor = (_findCursor + 1) % _findHits.Count;

        for (int i = 0; i < _findHits.Count; i++)
        {
            _findHits[i].FindCurrent = i == _findCursor;
        }

        ToggleRow row = _findHits[_findCursor];
        RevealRow(row);

        _installedStatus.Text =
            $"FIND {_findCursor + 1}/{_findHits.Count}  \u2022  {row.Title.ToUpperInvariant()}";
        _installedStatus.ForeColor = Theme.Accent;
    }

    private static bool MatchesFind(InstalledApp app, string term) =>
        app.DisplayName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
        (app.Publisher?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
        (app.PackageId?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
        (app.DisplayVersion?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
        (app.InstallLocation?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;

    /// <summary>Centres a row in the installed list, the way the queue window follows its live job.</summary>
    private void RevealRow(ToggleRow row)
    {
        if (row.IsDisposed || !_installedList.Visible || !_installedList.IsHandleCreated) return;

        int room = _installedList.ClientSize.Height;
        if (room <= 0) return;

        _installedList.ScrollControlIntoView(row);

        int current = -_installedList.AutoScrollPosition.Y;
        int max = Math.Max(0, _installedList.AutoScrollMinSize.Height - room);

        // AutoScroll shifts a child's bounds as the list scrolls, so Top already carries the offset.
        int logicalTop = row.Top + current;
        int target = Math.Clamp(logicalTop - ((room - row.Height) / 2), 0, max);

        if (current == target) return;

        _installedList.AutoScrollPosition = new Point(0, target);
    }

    private void RestoreInstalledStatus()
    {
        _installedStatus.Text = _installedSummary;
        _installedStatus.ForeColor = Theme.Text;
    }

    /// <summary>
    /// Empties the find box once the user acts on the list — uninstalling, running updates or
    /// ticking one — so a stale query does not leave rows lit and the status out of step.
    /// </summary>
    private void ClearFind()
    {
        if (_filter.Length == 0 && _findHits.Count == 0) return;

        if (_search.Text.Length > 0 && _search.Text != SearchHint)
        {
            _search.Text = string.Empty; // TextChanged re-runs the find and restores the count.
            return;
        }

        _filter = string.Empty;
        ApplyFind();
    }

    private void SetSortMode(int mode)
    {
        _installedSort = mode;
        _sort.Text = $"SORT: {SortLabels[mode]}";

        for (int i = 0; i < _sortMenu.Items.Count; i++)
        {
            if (_sortMenu.Items[i] is ToolStripMenuItem item) item.Checked = i == mode;
        }

        RebuildInstalled();
    }

    /// <summary>The installed list in the order the SORT button selected; name is the scan default.</summary>
    private IEnumerable<InstalledApp> OrderedInstalled() => _installedSort switch
    {
        // Apps with no recorded date sink to the bottom in both date orders.
        1 => _installed.OrderByDescending(a => a.InstallDate ?? DateTime.MinValue)
                       .ThenBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase),
        2 => _installed.OrderBy(a => a.InstallDate ?? DateTime.MaxValue)
                       .ThenBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase),
        3 => _installed.OrderBy(a => a.Publisher ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                       .ThenBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase),
        4 => _installed.OrderByDescending(a => a.EstimatedSizeBytes)
                       .ThenBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase),
        _ => _installed.OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase),
    };

    private ToggleRow BuildInstalledRow(InstalledApp app)
    {
        (string badge, Color badgeTone) = app.Scope switch
        {
            InstallScope.User => ("USER", Theme.Accent),
            InstallScope.Store => ("STORE", Theme.Violet),
            _ => (string.Empty, Theme.TextDim),
        };

        ProgramEntry? entry = EntryFor(app);
        bool tracked = app.IsTracked;
        bool dead = _deadNames.Contains(app.DisplayName);
        bool canUninstall = tracked || entry?.HasUninstaller == true;
        // winget refuses user-scope uninstalls while elevated (0x8A15007D), and this app
        // normally runs as administrator — per-user apps go through their own uninstaller.
        bool useRegistryUninstaller = app.Scope == InstallScope.User && entry?.HasUninstaller == true;

        if (dead) (badge, badgeTone) = ("DEAD", Theme.Bad);

        var row = new ToggleRow
        {
            Title = Theme.Truncate(app.DisplayName, 56),
            // No pre-truncation: the row renderer ellipsises at the real width, and a fixed cap
            // left the right half of wide rows blank.
            Subtitle = InstalledSubtitle(app),
            BadgeText = badge,
            BadgeTone = badgeTone,
            Tone = dead ? Theme.Bad : Theme.Accent,
            Checked = false,
            Locked = true, // inspection only; removal goes through the bin action
            ShowSwitch = false, // a locked switch that ignores clicks just looks broken
            ActionGlyph = canUninstall ? "\uE74D" : string.Empty,
            ActionTone = Theme.Bad,
            Tag = app,
        };

        if (canUninstall)
        {
            row.ActionClicked += _ => OnAppAction(row, app);
        }

        string tip = $"{app.DisplayName}\r\n{app.DisplayVersion ?? "\u2014"}\r\n" +
                     $"{app.Publisher ?? "\u2014"}  \u2022  {app.ScopeDisplay}\r\n";
        tip += dead
            ? "Dead entry — its uninstaller is gone. Use CLEAN DEAD to remove the leftover registry key."
            : useRegistryUninstaller
                ? "Press the bin twice to run its registered uninstaller (per-user apps can't go through elevated winget)."
                : tracked
                    ? "Press the bin twice to uninstall through winget."
                    : entry?.HasUninstaller == true
                        ? "Press the bin twice to run its registered uninstaller."
                        : "Not tracked by winget and no uninstaller is registered.";
        _tips.SetToolTip(row, tip);

        ContextMenuStrip menu = BuildAppMenu(app);
        row.ContextMenuStrip = menu;

        return row;
    }

    private static string InstalledSubtitle(InstalledApp a)
    {
        var parts = new List<string>
        {
            a.DisplayVersion ?? "\u2014",
            Theme.Truncate(a.Publisher ?? "\u2014", 32),
            a.ScopeDisplay,
            a.SizeDisplay,
        };
        if (a.InstallDate is { } installed) parts.Add($"installed {installed:yyyy-MM-dd}");
        return string.Join("  \u2022  ", parts);
    }

    private ContextMenuStrip BuildAppMenu(InstalledApp app)
    {
        var menu = new ContextMenuStrip { BackColor = Theme.Surface, ForeColor = Theme.Text };
        menu.ShowImageMargin = false;
        menu.Font = Theme.MonoSmall;

        var ignoreItem = new ToolStripMenuItem { ForeColor = Theme.Text, BackColor = Theme.Surface };
        menu.Items.Add(ignoreItem);

        menu.Opening += (_, _) =>
        {
            bool ignored = _settings?.IsIgnored(app.Key) ?? false;
            ignoreItem.Text = ignored ? "STOP IGNORING" : "IGNORE \u2014 HIDE FROM SCANS";
            ignoreItem.Enabled = !_installing;
        };

        ignoreItem.Click += (_, _) => ToggleAppIgnore(app);

        return menu;
    }

    private void ToggleAppIgnore(InstalledApp app)
    {
        if (_settings is null) return;

        ExclusionRule? rule = _settings.Current.Exclusions.FirstOrDefault(e =>
            e.Kind == ExclusionKind.Ignore &&
            string.Equals(e.Key, app.Key, StringComparison.OrdinalIgnoreCase));

        if (rule is not null)
        {
            _settings.RemoveExclusion(rule);
            ActivityLog.Info($"{app.DisplayName} is no longer ignored — rescan to see it again.");
        }
        else
        {
            _settings.AddExclusion(new ExclusionRule
            {
                Key = app.Key,
                DisplayName = app.DisplayName,
                Kind = ExclusionKind.Ignore,
                Note = "Ignored from the Software page",
            });
            ActivityLog.Info($"{app.DisplayName} ignored — hidden from scans and reports.");
        }

        RebuildInstalled();
    }

    /// <summary>The uninstall-registry entry matching this app by display name, when one was scanned.</summary>
    private ProgramEntry? EntryFor(InstalledApp app) =>
        _entryByName.TryGetValue(app.DisplayName, out ProgramEntry? entry) ? entry : null;

    /// <summary>Two-press confirm on the row's bin, the same pattern the Updates page uses.</summary>
    private void OnAppAction(ToggleRow row, InstalledApp app)
    {
        ProgramEntry? entry = EntryFor(app);
        bool canUninstall = app.IsTracked || entry?.HasUninstaller == true;

        if (_installing || _busyScan || !canUninstall) return;

        if (_armedRow == row)
        {
            DisarmConfirm();
            _ = DispatchUninstallAsync(app, entry);
            return;
        }

        DisarmConfirm();
        _armedRow = row;
        row.ActionText = "SURE?";
        row.ActionGlyph = string.Empty;
        row.ActionTone = Theme.Bad;
        row.Invalidate();
        _confirmTimer.Start();
    }

    private void DisarmConfirm()
    {
        _confirmTimer.Stop();
        if (_armedRow is not null && !_armedRow.IsDisposed)
        {
            _armedRow.ActionText = string.Empty;
            _armedRow.ActionGlyph = "\uE74D";
            _armedRow.ActionTone = Theme.Bad;
            _armedRow.Invalidate();
        }
        _armedRow = null;
    }

    /// <summary>
    /// Picks the uninstall route for an app: user-scope apps can't be uninstalled by an elevated
    /// winget (0x8A15007D), so they take their registered uninstaller; everything else winget first.
    /// Awaited by batch callers so removals run one at a time.
    /// </summary>
    private async Task DispatchUninstallAsync(InstalledApp app, ProgramEntry? entry)
    {
        bool useRegistryUninstaller =
            app.Scope == InstallScope.User && entry?.HasUninstaller == true;

        if (!useRegistryUninstaller && app.IsTracked && !string.IsNullOrWhiteSpace(app.PackageId))
        {
            await RunUninstallAsync(app);
        }
        else if (entry is not null)
        {
            UninstallViaRegistry(app, entry);
        }
    }

    private async Task RunUninstallAsync(InstalledApp app)
    {
        if (_runner is null || string.IsNullOrWhiteSpace(app.PackageId)) return;

        var item = new RunItem
        {
            PackageId = app.PackageId,
            Name = app.DisplayName,
            SourceName = app.SourceName ?? "winget",
            Action = RunAction.Uninstall,
            IconText = app.IconText,
            IconColour = app.IconColour,
            EstimatedBytes = app.EstimatedSizeBytes,
            InstallLocation = app.InstallLocation,
        };

        await StartRunAsync(new List<RunItem> { item }, $"UNINSTALLING {app.DisplayName.ToUpperInvariant()}");
    }

    /// <summary>Untracked apps go through their own registered uninstaller, the way Programs and Features does.</summary>
    private void UninstallViaRegistry(InstalledApp app, ProgramEntry entry)
    {
        (bool ok, string message) = ProgramsService.Uninstall(entry);

        if (ok)
        {
            ClearFind();
            ActivityLog.Success(message);
            _installedStatus.Text = Theme.Truncate(
                "UNINSTALLER RUNNING \u2014 LIST UPDATES WHEN IT FINISHES", 60);
            _installedStatus.ForeColor = Theme.Warn;
            _ = PollUntilGoneAsync(
                () => ProgramsService.UninstallKeyExists(entry), entry.DisplayName);
            return;
        }

        ActivityLog.Error($"Uninstall: {message}");
        _installedStatus.Text = Theme.Truncate(message.ToUpperInvariant(), 60);
        _installedStatus.ForeColor = Theme.Bad;
    }

    /// <summary>
    /// Uninstallers are separate processes the user drives, and winget can return while the
    /// real removal is still running detached. Poll until the registration is actually gone,
    /// then refresh — a row disappears because the app left, not because a launcher returned.
    /// </summary>
    private async Task PollUntilGoneAsync(Func<bool> stillRegistered, string what)
    {
        bool gone = false;
        try
        {
            // ~15 minutes at 10s intervals: long enough for a user-driven uninstaller,
            // short enough that a stuck one does not pin the page open forever.
            for (int i = 0; i < 90 && !IsDisposed; i++)
            {
                await Task.Delay(10000);
                if (IsDisposed) return;
                if (!stillRegistered()) { gone = true; break; }
            }
        }
        catch { return; }

        if (IsDisposed) return;
        if (gone)
            ActivityLog.Info($"{what} is no longer registered — refreshing the list.");
        else
            ActivityLog.Warn($"{what} is still registered 15 minutes after uninstall — " +
                             "it may need a restart or the uninstaller failed silently.");
        _ = ScanAllAsync(announce: false);
    }

    /// <summary>Winget-path check: the app's name still appears in the uninstall registry.</summary>
    private static bool NameStillRegistered(string name) =>
        ProgramsService.Load().Any(e => string.Equals(e.DisplayName, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Dead entries are uninstall keys whose uninstaller is provably gone; clearing them removes the
    /// leftover registry rows through RegistryService, which writes a .reg backup first.
    /// </summary>
    private void CleanDeadEntries()
    {
        if (_busyScan || _installing) return;

        List<ProgramEntry> dead = _programEntries.Where(entry => entry.Dead).ToList();
        if (dead.Count == 0)
        {
            _installedStatus.Text = "NO DEAD ENTRIES FOUND";
            _installedStatus.ForeColor = Theme.Good;
            return;
        }

        string names = string.Join("\r\n", dead.Take(8).Select(entry => $"• {entry.DisplayName}"));
        if (dead.Count > 8) names += $"\r\n• and {dead.Count - 8} more";

        DialogResult answer = MessageBox.Show(
            this,
            $"{dead.Count} dead program entr{(dead.Count == 1 ? "y" : "ies")} will be removed from the uninstall registry:\r\n\r\n{names}\r\n\r\n" +
            "A .reg backup is written first. Continue?",
            "Remove dead entries",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes) return;

        (bool ok, string message) = RegistryService.Remove(ProgramsService.ToIssues(dead));

        if (!ok)
        {
            ActivityLog.Error($"Dead entry clean: {message}");
            _installedStatus.Text = Theme.Truncate(message.ToUpperInvariant(), 60);
            _installedStatus.ForeColor = Theme.Bad;
            return;
        }

        ActivityLog.Success($"Dead entry clean: {message}");
        _ = ScanAllAsync(announce: false);
    }

    // ------------------------------------------------------------------ runs

    private async Task UpdateSelectedAsync()
    {
        if (_settings is null || _winget is null) return;

        var candidates = _updateRows
            .Where(pair => pair.Row.Checked && !pair.Candidate.IsPinned)
            .Select(pair => pair.Candidate)
            .ToList();

        int pinned = _updateRows.Count(pair => pair.Row.Checked && pair.Candidate.IsPinned);
        if (pinned > 0)
        {
            ActivityLog.Warn($"{pinned} pinned app(s) were left out of the run — unpin them first.");
        }

        if (candidates.Count == 0)
        {
            _updateStatus.Text = "TICK UPDATES TO RUN FIRST";
            _updateStatus.ForeColor = Theme.Warn;
            return;
        }

        if (!_wingetReady)
        {
            _updateStatus.Text = "WINGET NOT FOUND \u2014 REPAIR IT FIRST";
            _updateStatus.ForeColor = Theme.Bad;
            return;
        }

        // RunningAppPolicy.Ask is answered here, once, before the queue starts; the runner
        // treats Ask as Proceed because anything still queued was already consented to.
        if (_settings.Current.RunningAppPolicy == RunningAppPolicy.Ask)
        {
            var running = candidates.Where(c => c.IsRunning).Select(c => c.Name).ToList();
            if (running.Count > 0)
            {
                string names = string.Join(", ", running.Take(6).Select(n => Theme.Truncate(n, 30)));
                if (running.Count > 6) names += $" and {running.Count - 6} more";

                DialogResult answer = MessageBox.Show(
                    this,
                    $"These apps are running and their files may be in use:\n\n{names}\n\n" +
                    "winget can fail or skip them while they are open. Continue anyway?",
                    "Apps are running",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (answer != DialogResult.Yes) return;
            }
        }

        var items = candidates.Select(c => new RunItem
        {
            PackageId = c.PackageId,
            Name = c.Name,
            SourceName = c.SourceName,
            Action = RunAction.Update,
            FromVersion = c.CurrentVersion,
            ToVersion = c.AvailableVersion,
            EstimatedBytes = c.EstimatedDownloadBytes,
            IconText = c.IconText,
            IconColour = c.IconColour,
            IsSecuritySensitive = c.IsSecuritySensitive,
            Scope = c.InstalledScope,
            InstallLocation = c.InstallLocation,
        }).ToList();

        await StartRunAsync(items, $"UPDATING {candidates.Count} APP{(candidates.Count == 1 ? string.Empty : "S")}");
    }

    private async Task UpdateSingleAsync(UpdateCandidate candidate)
    {
        if (_runner is null || !_wingetReady || string.IsNullOrWhiteSpace(candidate.PackageId)) return;

        var item = new RunItem
        {
            PackageId = candidate.PackageId,
            Name = candidate.Name,
            SourceName = candidate.SourceName,
            Action = RunAction.Update,
            FromVersion = candidate.CurrentVersion,
            ToVersion = candidate.AvailableVersion,
            EstimatedBytes = candidate.EstimatedDownloadBytes,
            IconText = candidate.IconText,
            IconColour = candidate.IconColour,
            IsSecuritySensitive = candidate.IsSecuritySensitive,
            Scope = candidate.InstalledScope,
            InstallLocation = candidate.InstallLocation,
        };

        await StartRunAsync(new List<RunItem> { item }, $"UPDATING {candidate.Name.ToUpperInvariant()}");
    }

    private async Task StartRunAsync(List<RunItem> items, string caption)
    {
        if (_installing || _runner is null || items.Count == 0) return;

        // The find box goes back to its hint as soon as a run starts: the rows it was pointing at
        // are about to be reported on and then rescanned.
        ClearFind();

        _installing = true;
        _sweep.Start();
        _cancelRun.Visible = true;
        _cancelRun.Enabled = true;
        SyncButtons();
        SetCardsEnabled(false);

        _updateStatus.Text = caption;
        _updateStatus.ForeColor = Theme.Accent;
        using IDisposable busy = Working();

        try
        {
            // Completed fires before RunAsync returns; FinishRun does the reporting.
            await _runner.RunAsync(items);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Software run failed: {ex}");
            FinishRun(new RunSummary(0, items.Count, 0, false, TimeSpan.Zero, string.Empty,
                                     new RebootPlan(RebootAction.None, TimeSpan.Zero, string.Empty)));
        }
    }

    private void FinishRun(RunSummary summary)
    {
        _installing = false;
        _sweep.Stop();
        _cancelRun.Visible = false;
        SetCardsEnabled(true);
        SyncButtons();

        string counts =
            $"{summary.Succeeded} DONE  \u2022  {summary.Failed} FAILED  \u2022  " +
            $"{summary.Skipped} SKIPPED{(summary.Cancelled ? "  \u2022  CANCELLED" : string.Empty)}";

        Color tone = summary.WasClean ? Theme.Good : summary.Cancelled ? Theme.Warn : Theme.Bad;
        _updateStatus.Text = counts;
        _updateStatus.ForeColor = tone;

        if (summary.WasClean)
            ActivityLog.Success($"Software run: {counts} in {Format.Duration(summary.Elapsed)}.");
        else if (summary.Cancelled)
            ActivityLog.Warn($"Software run stopped: {counts}.");
        else
            ActivityLog.Error($"Software run finished with failures: {counts}.");

        if (!string.IsNullOrWhiteSpace(summary.LogPath))
            ActivityLog.Info($"Run log: {summary.LogPath}");

        HandleReboot(summary.Reboot);

        // Uninstallers detach and finish after winget exits; watch the registry names so the
        // list drops the row when removal really lands instead of looking like it failed.
        if (_runner is not null)
        {
            foreach (string name in _runner.Items
                .Where(i => i.Action == RunAction.Uninstall && i.State == RunItemState.Succeeded)
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _ = PollUntilGoneAsync(() => NameStillRegistered(name), name);
            }
        }

        // Versions and installed flags changed under us; rescan so badges and switches catch up.
        // Suspect items ride the same rescan: failures still installed, and updates that claimed
        // success but are still offered afterwards, both get the remove-or-hide choice.
        List<RunItem> suspects = _runner?.Items
            .Where(i => i.State == RunItemState.Failed ||
                        (i.Action == RunAction.Update && i.State == RunItemState.Succeeded))
            .ToList() ?? new List<RunItem>();

        if (suspects.Count > 0 && !_removingFailed)
            _ = OfferFailedRemovalsAsync(suspects);
        else
            _ = ScanAllAsync(announce: false);
    }

    /// <summary>
    /// After a run with failures, the rescan is the verdict: an app still installed gets the
    /// same remove-or-hide choice the Updates page offers, rather than returning broken forever.
    /// </summary>
    private async Task OfferFailedRemovalsAsync(List<RunItem> suspects)
    {
        await ScanAllAsync(announce: false);
        if (IsDisposed) return;

        // The rescan is the verdict: a failed item only matters while the app is still
        // installed, and a "successful" update only matters while the scan still offers it.
        List<RunItem> stillSuspect = suspects
            .Where(item => item.State == RunItemState.Failed ||
                           _updates.Any(u => string.Equals(
                               u.PackageId, item.PackageId, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        List<InstalledApp> stillThere = stillSuspect
            .Select(item => _installed.FirstOrDefault(app =>
                (item.PackageId.Length > 0 &&
                 string.Equals(app.PackageId, item.PackageId, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(app.DisplayName, item.Name, StringComparison.OrdinalIgnoreCase)))
            .Where(app => app is not null)
            .Select(app => app!)
            .GroupBy(app => app.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (stillThere.Count == 0) return;

        string names = string.Join("\r\n", stillThere.Take(6).Select(app => $"• {app.DisplayName}"));
        if (stillThere.Count > 6) names += $"\r\n• and {stillThere.Count - 6} more";

        int choice = OptionPromptForm.Ask(
            this,
            "Run finished with failures",
            $"{stillThere.Count} app{(stillThere.Count == 1 ? string.Empty : "s")} failed or are still offered an update:\r\n\r\n{names}\r\n\r\n" +
            "Remove them, hide them from scans, or keep them?",
            new[] { "REMOVE", "HIDE", "KEEP" },
            defaultIndex: 2);

        if (choice == 0)
        {
            _removingFailed = true;
            try
            {
                foreach (InstalledApp app in stillThere)
                {
                    if (IsDisposed) return;
                    await DispatchUninstallAsync(app, EntryFor(app));
                }
            }
            finally
            {
                _removingFailed = false;
            }
            return; // each removal rescans through its own run or registry watcher
        }

        if (choice != 1 || _settings is null) return;

        int hidden = 0;
        foreach (InstalledApp app in stillThere)
        {
            _settings.AddExclusion(new ExclusionRule
            {
                Key = app.Key,
                DisplayName = app.DisplayName,
                Kind = ExclusionKind.Ignore,
                Note = "Hidden after a failed run",
            });
            hidden++;
        }
        _settings.Save();
        ActivityLog.Warn($"{hidden} app{(hidden == 1 ? string.Empty : "s")} hidden from scans after failing.");
        await ScanAllAsync(announce: false);
    }

    /// <summary>The runner schedules Automatic restarts itself; Prompt is the UI's to answer.</summary>
    private void HandleReboot(RebootPlan plan)
    {
        if (plan.Action != RebootAction.Prompt) return;

        switch (RebootPromptForm.Ask(this, plan.Reason))
        {
            case RebootChoice.RestartNow:
                RebootProbe.Restart(plan.Delay, plan.Reason);
                break;

            // "Never" is the same switch as Settings > REBOOT: NEVER — one place, one meaning.
            case RebootChoice.Never when _settings is not null:
                _settings.Current.RebootPolicy = RebootPolicy.Never;
                _settings.Save();
                ActivityLog.Info("Restart prompts turned off (Settings > REBOOT: NEVER).");
                break;
        }
    }

    private void OnRunnerItemChanged(RunItem item)
    {
        if (IsDisposed) return;

        if (InvokeRequired)
        {
            try { BeginInvoke(new Action<RunItem>(OnRunnerItemChanged), item); }
            catch (InvalidOperationException) { }
            return;
        }

        bool found = false;

        if (_updateRowById.TryGetValue(item.PackageId, out ToggleRow? updateRow) && !updateRow.IsDisposed)
        {
            found = true;
            PaintRunState(updateRow, item);
        }

        if (_installedRowById.TryGetValue(item.PackageId, out ToggleRow? installedRow) && !installedRow.IsDisposed)
        {
            found = true;
            PaintRunState(installedRow, item);
        }

        if (found)
        {
            _updateStatus.Text = $"{Theme.Truncate(item.Name.ToUpperInvariant(), 22)}  {item.StatusText}";
            _updateStatus.ForeColor = item.State == RunItemState.Failed ? Theme.Bad : Theme.Accent;
        }
    }

    private static void PaintRunState(ToggleRow row, RunItem item)
    {
        row.Working = item.State is RunItemState.Queued or RunItemState.Running;
        row.Progress = item.Percent;
        row.BadgeText = item.State switch
        {
            RunItemState.Succeeded => "DONE",
            RunItemState.Failed => "FAILED",
            RunItemState.Skipped => "SKIPPED",
            RunItemState.Cancelled => "CANCELLED",
            RunItemState.Running => "WORKING",
            _ => "QUEUED",
        };
        row.BadgeTone = item.State switch
        {
            RunItemState.Succeeded => Theme.Good,
            RunItemState.Failed => Theme.Bad,
            RunItemState.Cancelled => Theme.Warn,
            _ => Theme.Accent,
        };

        if (!string.IsNullOrWhiteSpace(item.Detail))
        {
            row.Subtitle = Theme.Truncate(item.Detail, 105);
        }

        row.Invalidate();
    }

    private void SetCardsEnabled(bool enabled)
    {
        foreach (Control control in _updatesList.Controls)
            if (control is ToggleRow row) row.Enabled = enabled || row.Working;
        foreach (Control control in _installedList.Controls)
            if (control is ToggleRow row) row.Enabled = enabled || row.Working;
    }

    private void ToggleSelectAll()
    {
        if (_installing) return;

        var eligible = _updateRows
            .Where(pair => !pair.Candidate.IsPinned)
            .Select(pair => pair.Row)
            .ToList();

        if (eligible.Count == 0) return;

        bool check = eligible.Any(row => !row.Checked);
        foreach (ToggleRow row in eligible)
        {
            row.Checked = check;
            if (row.Tag is UpdateCandidate candidate) candidate.IsSelected = check;
        }

        SyncButtons();
    }

    private void SyncButtons()
    {
        int selected = _updateRows.Count(pair => pair.Row.Checked && !pair.Candidate.IsPinned);
        _updateSelected.Text = selected == 0 ? "UPDATE SELECTED" : $"UPDATE SELECTED ({selected})";
        _updateSelected.Enabled = !_busyScan && !_installing && selected > 0 && _wingetReady;
        _selectAll.Enabled = !_installing;
        _securityOnly.Enabled = !_installing;
        _scan.Enabled = !_busyScan && !_installing;
        _rescan.Enabled = !_busyScan && !_installing;
        _cleanDead.Enabled = !_busyScan && !_installing;
        _cancelRun.Enabled = _installing && _runner?.IsRunning == true;
        _updateSelected.Invalidate();
        _selectAll.Invalidate();
        RelayoutToolbars();
    }

    // ------------------------------------------------------------------ settings rows

    private void BuildSettingsRows()
    {
        _settingsList.SuspendLayout();
        foreach (Control control in _settingsList.Controls.Cast<Control>().ToArray()) control.Dispose();
        _settingsList.Controls.Clear();
        _settingRows.Clear();

        int y = 6;

        void Header(string text)
        {
            _settingsList.Controls.Add(new Label
            {
                Text = text,
                Font = Theme.Kicker,
                ForeColor = Theme.TextFaint,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(4, y),
            });
            y += 24;
        }

        void Toggle(string title, string subtitle, Func<bool> get, Action<bool> apply, bool reschedule = false)
        {
            var row = new ToggleRow
            {
                Title = title,
                Subtitle = subtitle,
                Height = 46,
                Checked = get(),
                Tone = Theme.Accent,
            };
            row.Toggled += (_, c) =>
            {
                apply(c);
                _settings?.Save();
                if (reschedule) ScheduleSoon();
                SyncSettings();
            };
            row.SetBounds(4, y, RowWidth(_settingsList), 46);
            _settingsList.Controls.Add(row);
            _settingRows.Add((row, get));
            y += 50;
        }

        void Cycle(string title, string subtitle, Func<string> caption, Action onClick)
        {
            var row = new ToggleRow
            {
                Title = title,
                Subtitle = subtitle,
                Height = 46,
                ShowSwitch = false,
                Locked = true,
            };

            var button = new NeonButton
            {
                Text = caption(),
                Width = 168,
                Height = 28,
                Font = Theme.Kicker,
            };
            button.Click += (_, _) =>
            {
                onClick();
                _settings?.Save();
                if (title.StartsWith("Scan schedule", StringComparison.OrdinalIgnoreCase))
                    ScheduleSoon();
                SyncSettings();
            };
            row.Controls.Add(button);
            row.Resize += (_, _) =>
                button.SetBounds(row.Width - button.Width - 14, (row.Height - button.Height) / 2,
                                button.Width, button.Height);
            button.SetBounds(row.Width - button.Width - 14, 9, button.Width, 28);

            row.SetBounds(4, y, RowWidth(_settingsList), 46);
            _settingsList.Controls.Add(row);
            y += 50;
        }

        if (_settings is null) return;
        var s = _settings.Current;

        Header("SOURCES");
        Toggle("Use winget", "Package manager for update checks, installs and removals",
            () => s.UseWinget, v => s.UseWinget = v);
        Toggle("Use Microsoft Store", "Include Store apps in the inventory",
            () => s.UseMicrosoftStore, v => s.UseMicrosoftStore = v);
        Toggle("Use catalogue", "Allow the browsable app catalogue",
            () => s.UseCatalogue, v => s.UseCatalogue = v);

        Header("SCANNING");
        Toggle("Auto scan", "Scan on the schedule chosen below",
            () => s.AutoScan, v => s.AutoScan = v, reschedule: true);
        Toggle("Notify on updates", "Balloon tip when a scan finds updates",
            () => s.NotifyOnUpdates, v => s.NotifyOnUpdates = v);
        Toggle("Include Store apps", "Merge Store packages into the scan",
            () => s.IncludeStoreApps, v => s.IncludeStoreApps = v);
        Toggle("Include unknown versions", "Track apps whose version winget cannot read",
            () => s.IncludeUnknownVersions, v => s.IncludeUnknownVersions = v);

        Header("INSTALLS");
        Toggle("Create restore point", "System restore point before each run",
            () => s.CreateRestorePoint, v => s.CreateRestorePoint = v);
        Toggle("Keep installers", "Leave downloaded installers on disk",
            () => s.KeepInstallers, v => s.KeepInstallers = v);

        Header("BEHAVIOUR");
        Cycle("When an app is running",
            "What a run does with apps that have live processes",
            () => $"RUNNING APPS: {RunningLabel(s.RunningAppPolicy)}",
            () => s.RunningAppPolicy = Next(s.RunningAppPolicy));
        Cycle("After the run",
            "Whether a pending restart is offered, automatic or never",
            () => $"REBOOT: {RebootLabel(s.RebootPolicy)}",
            () => s.RebootPolicy = Next(s.RebootPolicy));
        Cycle("Scan schedule",
            "When PC Medic scans for software updates by itself",
            () => $"SCAN: {s.ScanSchedule.ToUpperInvariant()}",
            () =>
            {
                int index = Array.FindIndex(ScheduleOptions,
                    o => string.Equals(o, s.ScanSchedule, StringComparison.OrdinalIgnoreCase));
                s.ScanSchedule = ScheduleOptions[(index + 1) % ScheduleOptions.Length];
            });

        _settingsList.AutoScrollMinSize = new Size(0, y + 35);
        _settingsList.ResumeLayout();
    }

    private static string RunningLabel(RunningAppPolicy policy) => policy switch
    {
        RunningAppPolicy.AlwaysClose => "ALWAYS CLOSE",
        RunningAppPolicy.AlwaysSkip => "ALWAYS SKIP",
        _ => "ASK",
    };

    private static RunningAppPolicy Next(RunningAppPolicy policy) => policy switch
    {
        RunningAppPolicy.Ask => RunningAppPolicy.AlwaysClose,
        RunningAppPolicy.AlwaysClose => RunningAppPolicy.AlwaysSkip,
        _ => RunningAppPolicy.Ask,
    };

    private static string RebootLabel(RebootPolicy policy) => policy switch
    {
        RebootPolicy.Never => "NEVER",
        RebootPolicy.Automatic => "AUTOMATIC",
        _ => "PROMPT AT END",
    };

    private static RebootPolicy Next(RebootPolicy policy) => policy switch
    {
        RebootPolicy.PromptAtEnd => RebootPolicy.Never,
        RebootPolicy.Never => RebootPolicy.Automatic,
        _ => RebootPolicy.PromptAtEnd,
    };

    private void SyncSettings()
    {
        if (_settings is null) return;

        // Checked is set programmatically here; Toggled only fires on real clicks.
        foreach ((ToggleRow row, Func<bool> get) in _settingRows)
        {
            bool value = get();
            if (row.Checked != value) row.Checked = value;
        }

        RebuildExclusions();
    }

    /// <summary>Fire-and-forget wrapper so settings lambdas can schedule without a discard clash.</summary>
    private void ScheduleSoon() => _ = ApplyScheduleAsync();

    private async Task ApplyScheduleAsync()
    {
        if (_settings is null) return;

        try
        {
            ScheduledScanService.Outcome outcome =
                await Task.Run(() => ScheduledScanService.ApplyAsync(_settings.Current));

            _settingsStatus.Text = Theme.Truncate(outcome.Message.ToUpperInvariant(), 70);
            _settingsStatus.ForeColor = outcome.Ok ? Theme.Good : Theme.Warn;

            if (outcome.Ok) ActivityLog.Info("Scan schedule: " + outcome.Message);
            else ActivityLog.Warn("Scan schedule: " + outcome.Message);
        }
        catch (Exception ex)
        {
            ActivityLog.Error("Scan schedule update failed: " + ex.Message);
        }
    }

    private void RebuildExclusions()
    {
        if (_settings is null || _installing) return;

        _exclusionList.SuspendLayout();
        foreach (Control control in _exclusionList.Controls.Cast<Control>().ToArray()) control.Dispose();
        _exclusionList.Controls.Clear();

        var rules = _settings.Current.Exclusions;
        _exclusionHeader.Text = rules.Count == 0
            ? "EXCLUSION RULES  \u2022  NONE"
            : $"EXCLUSION RULES  \u2022  {rules.Count}";

        int y = 4;
        foreach (ExclusionRule rule in rules)
        {
            string kind = rule.Kind == ExclusionKind.Pin ? "PIN" : "IGNORE";
            string sub = $"{kind}  \u2022  {rule.Key}" +
                         (rule.Kind == ExclusionKind.Pin ? $"  \u2022  PINNED {rule.PinnedVersion}" : string.Empty);

            var row = new ToggleRow
            {
                Title = Theme.Truncate(rule.DisplayName, 40),
                Subtitle = Theme.Truncate(sub, 90),
                Height = 44,
                ShowSwitch = false,
                Locked = true,
                ActionText = "REMOVE",
                ActionTone = Theme.Bad,
                Tag = rule,
            };
            row.ActionClicked += _ => RemoveExclusion(rule);
            _tips.SetToolTip(row, $"{rule.DisplayName}\r\n{rule.Summary}\r\n{rule.Key}");
            row.SetBounds(4, y, RowWidth(_exclusionList), 44);
            _exclusionList.Controls.Add(row);
            y += 48;
        }

        if (rules.Count == 0)
        {
            _exclusionList.Controls.Add(new Label
            {
                Text = "No pins or ignore rules. Right-click an update or installed row to add one.",
                Font = Theme.MonoSmall,
                ForeColor = Theme.TextFaint,
                BackColor = Color.Transparent,
                AutoSize = true,
                MaximumSize = new Size(Math.Max(120, _exclusionList.ClientSize.Width - 30), 0),
                Location = new Point(8, 10),
            });
            y += 60;
        }

        _exclusionList.AutoScrollMinSize = new Size(0, y + 33);
        _exclusionList.ResumeLayout();
    }

    private void RemoveExclusion(ExclusionRule rule)
    {
        if (_settings is null) return;
        _settings.RemoveExclusion(rule); // Save fires Changed, which rebuilds the list.
        ActivityLog.Info($"Exclusion removed: {rule.DisplayName}");
        _ = ScanAllAsync(announce: false); // ignored apps reappear on the next pass
    }

    // ------------------------------------------------------------------ exports

    private void ExportReport(bool csv)
    {
        if (_installed.Count == 0 && _updates.Count == 0)
        {
            _installedStatus.Text = "SCAN FIRST \u2014 NOTHING TO EXPORT";
            _installedStatus.ForeColor = Theme.Warn;
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = csv ? "Export inventory as CSV" : "Export inventory as HTML",
            Filter = csv ? "CSV file (*.csv)|*.csv" : "HTML file (*.html)|*.html",
            FileName = $"PCMedic-inventory-{DateTime.Now:yyyyMMdd-HHmm}.{(csv ? "csv" : "html")}",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string path = dialog.FileName;
        _installedStatus.Text = "WRITING REPORT\u2026";
        _installedStatus.ForeColor = Theme.TextDim;

        _ = Task.Run(() =>
            {
                if (csv) ReportExporter.ExportCsv(path, _installed, _updates);
                else ReportExporter.ExportHtml(path, _installed, _updates);
            })
            .ContinueWith(t =>
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    try { BeginInvoke(new Action(() => ReportExported(t, path))); }
                    catch (InvalidOperationException) { }
                    return;
                }
                ReportExported(t, path);
            }, TaskScheduler.Default);
    }

    private void ReportExported(Task task, string path)
    {
        if (task.IsFaulted)
        {
            ActivityLog.Error($"Report export failed: {task.Exception?.GetBaseException().Message}");
            _installedStatus.Text = "EXPORT FAILED";
            _installedStatus.ForeColor = Theme.Bad;
            return;
        }

        ActivityLog.Success($"Report written to {path}");
        _installedStatus.Text = $"EXPORTED  \u2022  {Path.GetFileName(path).ToUpperInvariant()}";
        _installedStatus.ForeColor = Theme.Good;
    }

    private async Task ExportDiagnosticsAsync()
    {
        if (_settings is null || _winget is null) return;

        using var dialog = new SaveFileDialog
        {
            Title = "Export diagnostics",
            Filter = "Zip archive (*.zip)|*.zip",
            FileName = $"PCMedic-diagnostics-{DateTime.Now:yyyyMMdd-HHmm}.zip",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string path = dialog.FileName;
        _settingsStatus.Text = "BUILDING DIAGNOSTICS\u2026";
        _settingsStatus.ForeColor = Theme.TextDim;

        try
        {
            string written = await DiagnosticsExporter.ExportAsync(path, _winget, _settings);
            ActivityLog.Success($"Diagnostics exported to {written}");
            _settingsStatus.Text = "DIAGNOSTICS EXPORTED";
            _settingsStatus.ForeColor = Theme.Good;
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Diagnostics export failed: {ex}");
            _settingsStatus.Text = "EXPORT FAILED";
            _settingsStatus.ForeColor = Theme.Bad;
        }
    }

    private async Task RefreshCatalogueAsync()
    {
        if (_settings is null || _busyScan || _installing) return;

        _settingsStatus.Text = "DOWNLOADING CATALOGUE\u2026";
        _settingsStatus.ForeColor = Theme.TextDim;

        try
        {
            var catalogue = new CatalogueService();
            bool ok = await catalogue.RefreshAsync(_settings.Current.CatalogueUrl);

            if (ok)
            {
                ActivityLog.Success($"Catalogue refreshed: {catalogue.Apps.Count} apps.");
                _settingsStatus.Text = $"CATALOGUE \u2022 {catalogue.Apps.Count} APPS";
                _settingsStatus.ForeColor = Theme.Good;
            }
            else
            {
                ActivityLog.Warn("Catalogue refresh failed — the current list stays in use.");
                _settingsStatus.Text = "REFRESH FAILED \u2022 CURRENT LIST KEPT";
                _settingsStatus.ForeColor = Theme.Warn;
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Catalogue refresh failed: {ex}");
            _settingsStatus.Text = "REFRESH FAILED";
            _settingsStatus.ForeColor = Theme.Bad;
        }
    }

    // ------------------------------------------------------------------ misc

    private static int RowWidth(Panel list) => Math.Max(280, list.ClientSize.Width - 20);

    private static void ResizeRows(Panel list)
    {
        // Rows are positioned absolutely; only their width tracks the panel.
        foreach (Control control in list.Controls)
        {
            if (control is ToggleRow row) row.Width = RowWidth(list);
        }
    }
}
