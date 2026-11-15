namespace PC_Medic;

/// <summary>
/// Catalogue page: the Ninite-style "tick and install" list ported from SoftwareUpdater.
/// Rows come from the embedded/downloaded catalogue, installed flags come from the
/// uninstall registry, and installs run sequentially through the shared InstallRunner
/// so they get the same restore point, running-app policy and run log as Updates.
/// </summary>
internal sealed partial class CataloguePage : PageBase
{
    private const int MaxCategories = 10;

    private readonly List<(ToggleRow Row, CatalogueApp App)> _rows = new();
    private readonly Dictionary<string, ToggleRow> _rowByPackage = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _manualToggles = new(StringComparer.OrdinalIgnoreCase);

    private SettingsService? _settings;
    private CatalogueService? _catalogue;
    private WingetClient? _winget;
    private ActivityService? _activity;
    private InstallRunner? _runner;
    private string _category = "ALL";
    private string _filter = string.Empty;
    private bool _loaded;
    private bool _busy;
    private bool _installing;
    private bool _wingetReady;

    public CataloguePage()
        : base("CATALOGUE", "Common Windows apps, ticked and installed in one silent run.")
    {
        InitializeComponent();

        _install.Glyph = "";
        _refreshList.Glyph = "";
        _refreshCatalogue.Glyph = "";

        void LayoutToolbar()
        {
            int right = _toolbar.ClientSize.Width;
            _install.SetBounds(right - _install.Width, 5, _install.Width, 30);
            right -= _install.Width + 8;
            if (_cancel.Visible)
            {
                _cancel.SetBounds(right - _cancel.Width, 5, _cancel.Width, 30);
                right -= _cancel.Width + 8;
            }

            _refreshCatalogue.SetBounds(right - _refreshCatalogue.Width, 5, _refreshCatalogue.Width, 30);
            right -= _refreshCatalogue.Width + 8;
            _refreshList.SetBounds(right - _refreshList.Width, 5, _refreshList.Width, 30);
            right -= _refreshList.Width + 10;

            _searchHost.SetBounds(Math.Max(170, right - 190), 7, 190, 26);
        }

        _toolbar.Resize += (_, _) => LayoutToolbar();
        LayoutToolbar();

        _refreshList.Click += (_, _) => Reload(announce: true);
        _refreshCatalogue.Click += (_, _) => RefreshCatalogue();
        _install.Click += (_, _) => InstallSelected();
        _cancel.Click += (_, _) => _runner?.Cancel();

        _search.GotFocus += (_, _) =>
        {
            if (_search.Text == "SEARCH")
            {
                _search.Text = string.Empty;
                _search.ForeColor = Theme.Text;
            }
        };

        _search.LostFocus += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_search.Text))
            {
                _search.Text = "SEARCH";
                _search.ForeColor = Theme.TextFaint;
            }
        };

        _search.TextChanged += (_, _) =>
        {
            _filter = _search.Text == "SEARCH" ? string.Empty : _search.Text.Trim();
            RebuildRows();
        };

        _list.Resize += (_, _) =>
        {
            foreach (Control control in _list.Controls)
            {
                if (control is ToggleRow row) row.Width = RowWidth();
            }
        };

        _sweep.Tick += (_, _) =>
        {
            foreach ((ToggleRow row, _) in _rows)
            {
                if (row.Working) row.Sweep();
            }
        };

        _status.Text = "NOT LOADED";
        _footer.Text = "SOURCE  \u2014  \u2022  SYNCED  \u2014  \u2022  0 APPS";
    }

    public override void OnNavigated()
    {
        if (_loaded)
        {
            RefreshData();
            return;
        }

        _loaded = true;
        _ = LoadOnceAsync();
    }

    public override void RefreshData() => Reload(announce: false);

    private int RowWidth() => Math.Max(260, _list.ClientSize.Width - 24);

    private async Task LoadOnceAsync()
    {
        try
        {
            _settings = new SettingsService();
            _settings.Load();
            _catalogue = new CatalogueService();
            _catalogue.Load();
            _activity = new ActivityService();
            _activity.Load();
            _winget = new WingetClient();
            EnsureRunner();

            // WingetClient probes by spawning winget --version; keep that off the UI thread.
            _wingetReady = await Task.Run(() => _winget.IsAvailable);
            if (!_wingetReady)
            {
                ActivityLog.Warn("winget was not found \u2014 catalogue installs need 'App Installer' from the Microsoft Store.");
            }

            RebuildChips();
            Reload(announce: false);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Catalogue failed to load: {ex.Message}");
            UpdateStatus($"LOAD FAILED \u2014 {Theme.Truncate(ex.Message.ToUpperInvariant(), 60)}", Theme.Bad);
        }
    }

    private void EnsureRunner()
    {
        if (_runner is not null || _winget is null || _settings is null || _activity is null) return;

        _runner = new InstallRunner(_winget, _settings, _activity);
        _runner.ItemChanged += OnRunnerItemChanged;
        _runner.LogLine += line => ActivityLog.Info(line);
        _runner.Completed += summary =>
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<RunSummary>(s => FinishRun(s)), summary); }
                catch (InvalidOperationException) { }
                return;
            }

            FinishRun(summary);
        };
    }

    private async void Reload(bool announce, string? note = null)
    {
        if (_busy || _catalogue is null) return;

        _busy = true;
        _status.Text = "SCANNING INSTALLED APPS\u2026";
        using IDisposable busy = Working();
        _status.ForeColor = Theme.TextDim;
        SetToolbarEnabled(false);

        try
        {
            List<InstalledApp> installed = await new AppRegistryInventory().ScanAsync();
            _catalogue.MarkInstalled(installed);
            RebuildChips();
            RebuildRows();

            if (announce)
            {
                ActivityLog.Info(
                    $"Catalogue: {_catalogue.Apps.Count} apps listed, " +
                    $"{_catalogue.Apps.Count(app => app.IsInstalled)} already installed.");
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Catalogue reload failed: {ex.Message}");
            note = ex.Message;
        }
        finally
        {
            _busy = false;
            SetToolbarEnabled(true);
            UpdateStatus(note);
        }
    }

    private async void RefreshCatalogue()
    {
        if (_busy || _catalogue is null || _settings is null) return;

        _busy = true;
        _status.Text = "DOWNLOADING CATALOGUE\u2026";
        using IDisposable busy = Working();
        _status.ForeColor = Theme.TextDim;
        SetToolbarEnabled(false);
        string? note = null;

        try
        {
            bool ok = await _catalogue.RefreshAsync(_settings.Current.CatalogueUrl);

            if (ok)
            {
                ActivityLog.Success($"Catalogue refreshed: {_catalogue.Apps.Count} apps.");
                RebuildChips();
            }
            else
            {
                ActivityLog.Warn("Catalogue refresh failed \u2014 the current list stays in use.");
                note = "REFRESH FAILED \u2014 CURRENT LIST KEPT";
            }

            // Reload owns _busy from here; hand it the note so the status line reports the outcome.
            _busy = false;
            Reload(announce: false, note: note);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Catalogue refresh failed: {ex.Message}");
            _busy = false;
            SetToolbarEnabled(true);
            UpdateStatus(ex.Message, Theme.Bad);
        }
    }

    private async void InstallSelected()
    {
        if (_busy || _installing || _runner is null || _catalogue is null) return;

        List<CatalogueApp> apps = _rows
            .Where(pair => pair.Row.Checked && IsSelectable(pair.App))
            .Select(pair => pair.App)
            .ToList();

        if (apps.Count == 0)
        {
            UpdateStatus("TICK APPS TO INSTALL FIRST", Theme.Warn);
            return;
        }

        if (!_wingetReady)
        {
            UpdateStatus("WINGET NOT FOUND \u2014 INSTALL APP INSTALLER FIRST", Theme.Bad);
            return;
        }

        if (!Elevation.IsElevated)
        {
            ActivityLog.Warn("Catalogue install running without administrator rights \u2014 some packages will fail.");
            UpdateStatus("STANDARD USER \u2014 SOME INSTALLS MAY FAIL", Theme.Warn);
        }

        var items = apps.Select(app => new RunItem
        {
            PackageId = app.PreferredPackageId,
            Name = app.Name,
            SourceName = app.PreferredSource,
            Action = RunAction.Install,
            ToVersion = "latest",
            IconText = app.IconText,
            IconColour = app.IconColour,
            EstimatedBytes = (long)app.ApproxSizeMb * 1024 * 1024,
        }).ToList();

        _installing = true;
        _busy = true;
        _sweep.Start();
        _cancel.Visible = true;
        LayoutToolbarSafe();
        SetToolbarEnabled(false);
        _cancel.Enabled = true;
        _install.Enabled = false;

        foreach (ToggleRow row in _rows.Select(pair => pair.Row)) row.Enabled = false;

        _status.Text = $"INSTALLING {apps.Count} APP{(apps.Count == 1 ? string.Empty : "S")}\u2026";
        _status.ForeColor = Theme.Accent;
        using IDisposable busy = Working();

        try
        {
            await _runner.RunAsync(items);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Catalogue install failed: {ex.Message}");
            FinishRun(new RunSummary(0, items.Count, 0, false, TimeSpan.Zero, string.Empty,
                                     new RebootPlan(RebootAction.None, TimeSpan.Zero, string.Empty)));
        }
    }

    private void FinishRun(RunSummary summary)
    {
        _installing = false;
        _busy = false;
        _sweep.Stop();
        _cancel.Visible = false;
        LayoutToolbarSafe();

        foreach (ToggleRow row in _rows.Select(pair => pair.Row)) row.Enabled = true;

        string counts =
            $"{summary.Succeeded} INSTALLED  \u2022  {summary.Failed} FAILED  \u2022  " +
            $"{summary.Skipped} SKIPPED{(summary.Cancelled ? "  \u2022  CANCELLED" : string.Empty)}";

        if (summary.WasClean)
        {
            ActivityLog.Success($"Catalogue install: {counts} in {Format.Duration(summary.Elapsed)}.");
            UpdateStatus(counts, Theme.Good);
        }
        else if (summary.Cancelled)
        {
            ActivityLog.Warn($"Catalogue install stopped: {counts}.");
            UpdateStatus(counts, Theme.Warn);
        }
        else
        {
            ActivityLog.Error($"Catalogue install finished with failures: {counts}.");
            UpdateStatus(counts, Theme.Bad);
        }

        // Installed flags changed under us; rescan so INSTALLED badges and locked switches catch up.
        _ = RefreshInstalledAsync();
    }

    private async Task RefreshInstalledAsync()
    {
        try
        {
            List<InstalledApp> installed = await new AppRegistryInventory().ScanAsync();
            if (_catalogue is not null) _catalogue.MarkInstalled(installed);
            RebuildRows();
            UpdateFooter();
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"Installed-app rescan failed: {ex.Message}");
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

        if (_rowByPackage.TryGetValue(item.PackageId, out ToggleRow? row) && !row.IsDisposed)
        {
            row.Working = item.State is RunItemState.Queued or RunItemState.Running;
            row.Progress = item.Percent;
            (row.BadgeText, row.BadgeTone) = item.State switch
            {
                RunItemState.Succeeded => ("DONE", Theme.Good),
                RunItemState.Failed => ("FAILED", Theme.Bad),
                RunItemState.Skipped => ("SKIPPED", Theme.TextDim),
                RunItemState.Cancelled => ("CANCELLED", Theme.Warn),
                RunItemState.Running => ("INSTALLING", Theme.Accent),
                _ => ("QUEUED", Theme.TextDim),
            };
        }

        UpdateStatus($"{Theme.Truncate(item.Name.ToUpperInvariant(), 22)}  {item.StatusText}",
                     item.State == RunItemState.Failed ? Theme.Bad : Theme.Accent);
    }

    private void LayoutToolbarSafe() => LayoutToolbar();

    private void LayoutToolbar()
    {
        int right = _toolbar.ClientSize.Width;
        _install.SetBounds(right - _install.Width, 5, _install.Width, 30);
        right -= _install.Width + 8;
        if (_cancel.Visible)
        {
            _cancel.SetBounds(right - _cancel.Width, 5, _cancel.Width, 30);
            right -= _cancel.Width + 8;
        }

        _refreshCatalogue.SetBounds(right - _refreshCatalogue.Width, 5, _refreshCatalogue.Width, 30);
        right -= _refreshCatalogue.Width + 8;
        _refreshList.SetBounds(right - _refreshList.Width, 5, _refreshList.Width, 30);
        right -= _refreshList.Width + 10;

        _searchHost.SetBounds(Math.Max(170, right - 190), 7, 190, 26);
    }

    private static bool HasPackage(CatalogueApp app) => !string.IsNullOrWhiteSpace(app.PreferredPackageId);

    private static bool IsSelectable(CatalogueApp app) => !app.IsInstalled && HasPackage(app);

    private IEnumerable<CatalogueApp> VisibleApps()
    {
        if (_catalogue is null) yield break;

        foreach (CatalogueApp app in _catalogue.Apps)
        {
            if (!string.Equals(_category, "ALL", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(app.Category, _category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (_filter.Length > 0 &&
                app.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                (app.Publisher?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) ?? -1) < 0 &&
                app.Category.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            yield return app;
        }
    }

    private void RebuildChips()
    {
        _chips.SuspendLayout();
        foreach (Control control in _chips.Controls.Cast<Control>().ToArray()) control.Dispose();
        _chips.Controls.Clear();

        var categories = new List<string> { "ALL" };
        if (_catalogue is not null)
        {
            categories.AddRange(_catalogue.Categories.Take(MaxCategories));
        }

        _categories = categories;
        if (!_categories.Any(c => string.Equals(c, _category, StringComparison.OrdinalIgnoreCase)))
        {
            _category = "ALL";
        }

        foreach (string category in categories)
        {
            bool active = string.Equals(category, _category, StringComparison.OrdinalIgnoreCase);
            string label = category.ToUpperInvariant();
            var chip = new NeonButton
            {
                Text = label,
                Primary = active,
                Tone = active ? Theme.Accent : Theme.TextDim,
                Font = Theme.Kicker,
                Height = 24,
                Width = Math.Max(46, TextRenderer.MeasureText(label, Theme.Kicker).Width + 22),
                Margin = new Padding(4, 6, 4, 6),
                Tag = category,
            };
            chip.Click += (_, _) =>
            {
                if (string.Equals(_category, category, StringComparison.OrdinalIgnoreCase)) return;
                _category = category;
                RebuildChips();
                RebuildRows();
            };
            _chips.Controls.Add(chip);
        }

        _chips.ResumeLayout();
    }

    private void RebuildRows()
    {
        if (_catalogue is null || _installing) return;

        _list.SuspendLayout();
        foreach (Control control in _list.Controls.Cast<Control>().ToArray()) control.Dispose();
        _list.Controls.Clear();
        _rows.Clear();
        _rowByPackage.Clear();

        int y = 6;
        int shown = 0;

        foreach (CatalogueApp app in VisibleApps())
        {
            ToggleRow row = BuildRow(app);
            row.SetBounds(4, y, RowWidth(), 62);
            _list.Controls.Add(row);
            _rows.Add((row, app));
            if (!string.IsNullOrWhiteSpace(app.PreferredPackageId))
            {
                _rowByPackage.TryAdd(app.PreferredPackageId, row);
            }

            y += 68;
            shown++;
        }

        if (shown == 0)
        {
            var empty = new Label
            {
                Text = _catalogue.Apps.Count == 0
                    ? "The catalogue is empty. Use REFRESH CATALOGUE to pull a fresh list."
                    : "No apps match the current filter.",
                Font = Theme.MonoSmall,
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(8, 12),
            };
            _list.Controls.Add(empty);
            y += 40;
        }

        _list.AutoScrollMinSize = new Size(0, y + 6);
        _list.ResumeLayout();
        UpdateFooter();
        SyncButtons();
        UpdateStatus();
    }

    private ToggleRow BuildRow(CatalogueApp app)
    {
        bool selectable = IsSelectable(app);
        bool manual = _manualToggles.Contains(app.Id);

        (string badge, Color badgeTone) = BadgeFor(app);

        var row = new ToggleRow
        {
            Title = Theme.Truncate(app.Name, 60),
            Subtitle = Theme.Truncate(SubtitleFor(app), 90),
            BadgeText = badge,
            BadgeTone = badgeTone,
            Tone = app.IsInstalled ? Theme.Good : Theme.Accent,
            Checked = selectable && (app.IsSelected || !manual),
            Locked = !selectable,
            Action2Glyph = string.IsNullOrWhiteSpace(app.Homepage) ? string.Empty : "\uE8A7",
            Action2Tone = Theme.Violet,
            Tag = app,
        };

        row.Toggled += (_, _) =>
        {
            app.IsSelected = row.Checked;
            _manualToggles.Add(app.Id);
            SyncButtons();
            UpdateFooter();
        };

        if (!string.IsNullOrWhiteSpace(app.Homepage))
        {
            string url = app.Homepage;
            row.Action2Clicked += _ => AppInfo.OpenUrl(url);
            _tips.SetToolTip(row, $"{app.Name}\r\n{app.Publisher}\r\n{url}");
        }
        else
        {
            _tips.SetToolTip(row, $"{app.Name}\r\n{app.Publisher}\r\n{app.Description}");
        }

        return row;
    }

    private static (string Text, Color Tone) BadgeFor(CatalogueApp app)
    {
        if (app.IsInstalled) return ("INSTALLED", Theme.Good);
        if (app.Essential) return ("ESSENTIAL", Theme.Violet);
        if (app.Popularity >= 50) return ("POPULAR", Theme.Accent);
        return (string.Empty, Theme.TextDim);
    }

    private static string SubtitleFor(CatalogueApp app)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(app.Publisher)) parts.Add(app.Publisher.Trim());
        if (!string.IsNullOrWhiteSpace(app.Category)) parts.Add(app.Category.Trim().ToUpperInvariant());
        if (app.ApproxSizeMb > 0) parts.Add($"~{app.ApproxSizeMb} MB");

        return parts.Count == 0 ? "COMMON WINDOWS APP" : string.Join("  \u2022  ", parts);
    }

    private void SetToolbarEnabled(bool enabled)
    {
        _refreshList.Enabled = enabled && !_installing;
        _refreshCatalogue.Enabled = enabled && !_installing;
        SyncButtons();
    }

    private void SyncButtons()
    {
        int selected = _rows.Count(pair => pair.Row.Checked && IsSelectable(pair.App));
        _install.Text = selected == 0 ? "INSTALL SELECTED" : $"INSTALL SELECTED ({selected})";
        _install.Enabled = !_busy && !_installing && selected > 0 && _wingetReady;
        _install.Invalidate();
    }

    private void UpdateFooter()
    {
        if (_catalogue is null) return;

        int total = _catalogue.Apps.Count;
        int installed = _catalogue.Apps.Count(app => app.IsInstalled);
        int selected = _rows.Count(pair => pair.Row.Checked && IsSelectable(pair.App));
        string synced = _catalogue.LastSynced?.ToString("yyyy-MM-dd HH:mm") ?? "\u2014";

        _footer.Text =
            $"SOURCE  {Theme.Truncate(_catalogue.SourceDescription.ToUpperInvariant(), 40)}  \u2022  " +
            $"SYNCED  {synced}  \u2022  {total} APPS  \u2022  {installed} INSTALLED  \u2022  {selected} SELECTED";
    }

    private void UpdateStatus(string? note = null, Color? tone = null)
    {
        if (_catalogue is null)
        {
            _status.Text = "NOT LOADED";
            return;
        }

        int total = _catalogue.Apps.Count;
        int installed = _catalogue.Apps.Count(app => app.IsInstalled);

        _status.Text =
            $"{total} APPS  \u2022  {installed} INSTALLED  \u2022  " +
            (Elevation.IsElevated ? "ELEVATED" : "STANDARD USER") +
            (string.IsNullOrEmpty(note) ? string.Empty : $"  \u2022  {Theme.Truncate(note.ToUpperInvariant(), 80)}");

        _status.ForeColor = tone ?? Theme.TextDim;
    }

    private List<string> _categories = new();
}
