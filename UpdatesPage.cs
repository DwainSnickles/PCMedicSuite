namespace PC_Medic;

/// <summary>
/// Updates page: application upgrades through winget on the left, driver updates from the
/// Windows Update agent on the right. Both are opt-in per row; drivers require elevation.
/// </summary>
internal sealed partial class UpdatesPage : PageBase
{
    private Control? _appsContent;
    private Control? _driversContent;
    private readonly List<(ToggleRow Row, AppUpgrade App)> _appRows = new();
    private readonly List<(ToggleRow Row, DriverUpdate Driver)> _driverRows = new();
    private readonly string? _winget = UpdatesService.WingetPath;
    private List<AppUpgrade> _appItems = new();
    private List<DriverUpdate> _driverItems = new();
    private List<DriverDevice> _driverDevices = new();
    private string? _driverError;
    private bool _devicesScanned;
    private bool _devicesScanning;
    private bool _busyApps;
    private bool _busyDrivers;
    private IDisposable? _appsBusy;
    private IDisposable? _driverBusy;
    private bool _firstVisit = true;
    private ToggleRow? _confirmRow;

    public UpdatesPage()
        : base("UPDATES", "Application upgrades through winget, driver updates through Windows Update.")
    {
        InitializeComponent();

        _appsContent = _appsInner;
        _driversContent = _driversInner;

        _appsCard.CollapseGlyph = "";
        _appsCard.ExpandGlyph = "";
        _driversCard.CollapseGlyph = "";
        _driversCard.ExpandGlyph = "";

        _appsCard.CollapsedChanged += (_, _) =>
        {
            ApplyCollapse();
            SyncPanelBusy(_appsCard, _busyApps, ref _appsBusy);
            SyncPanelBusy(_driversCard, _busyDrivers || _devicesScanning, ref _driverBusy);
        };

        _driversCard.CollapsedChanged += (_, _) =>
        {
            ApplyCollapse();
            SyncPanelBusy(_appsCard, _busyApps, ref _appsBusy);
            SyncPanelBusy(_driversCard, _busyDrivers || _devicesScanning, ref _driverBusy);
            if (!_driversCard.Collapsed) EnsureDeviceScan();
        };

        _note.Text = _winget is null
            ? "WINGET NOT FOUND — THE UPGRADE BUTTON OPENS THE APP INSTALLER PAGE"
            : $"DATABASE  {Theme.Truncate(_winget, 70)}";
        _tips.SetToolTip(_note, _winget ?? UpdatesService.WingetDownloadPage);
        _root.SetColumnSpan(_note, 3);

        _appStatus.Text = _winget is null ? "WINGET NOT FOUND" : "NOT SCANNED";
        _driverStatus.Text = "NOT SCANNED";

        // The status labels are transparent and are added before the buttons, so they sat in front and
        // painted the toolbar's own surface across the left of each button — it ate the start of the
        // caption. Both now stop short of the first button, and the buttons sit in front regardless.
        foreach (Control button in new Control[] { _downloadApps, _scanApps, _downloadDrivers, _offlineDrivers, _scanDrivers })
            button.BringToFront();

        _scanApps.Click += (_, _) => _ = ReloadApps(announce: true);
        _scanDrivers.Click += (_, _) => ReloadDrivers(announce: true);

        _downloadApps.Click += (_, _) =>
        {
            if (_winget is null)
            {
                Tools.OpenUrl(UpdatesService.WingetDownloadPage);
                return;
            }

            DownloadAllApps();
        };

        _downloadDrivers.Click += (_, _) => DownloadAllDrivers();

        // One button, two folder actions: install packages from a stick or backup, or write this
        // machine's drivers out so a rebuild does not need the network at all.
        _offlineDrivers.Click += (_, _) =>
            _offlineMenu.Show(_offlineDrivers, new Point(0, _offlineDrivers.Height));

        void ManageBlacklist()
        {
            using var dialog = new HiddenUpdatesForm();
            dialog.OpenFileRequested += OpenBlacklistFile;
            dialog.ShowDialog(FindForm());

            // Unhidden packages are offered again, so the list and its badge catch up now.
            if (dialog.Changed && !IsDisposed) _ = ReloadApps(announce: false);
        }

        void OpenBlacklistFile()
        {
            string path = UpdatesService.IgnoreFilePath;
            string? folder = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            if (!File.Exists(path)) File.WriteAllText(path, "[]");

            (bool ok, string message) = Tools.Launch(path);
            if (ok)
            {
                ActivityLog.Info($"Hidden updates file opened: {path}");
            }
            else
            {
                SetDriverStatus("COULD NOT OPEN FILE", Theme.Bad, message);
                ActivityLog.Error($"Could not open hidden updates file: {message}");
            }
        }

        _offlineMenu.BackColor = Theme.Surface;
        _offlineMenu.ForeColor = Theme.Text;
        _offlineMenu.ShowImageMargin = false;
        _offlineMenu.Font = Theme.MonoSmall;

        var installFromFolder = new ToolStripMenuItem("INSTALL FROM FOLDER\u2026")
        {
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
        };
        installFromFolder.Click += (_, _) => InstallFromFolder();

        var saveBackup = new ToolStripMenuItem("SAVE DRIVER BACKUP\u2026")
        {
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
        };
        saveBackup.Click += (_, _) => SaveDriverBackup();

        var openBlacklist = new ToolStripMenuItem("OPEN HIDDEN UPDATES…")
        {
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
        };
        openBlacklist.Click += (_, _) => OpenBlacklistFile();

        var manageBlacklist = new ToolStripMenuItem("MANAGE HIDDEN UPDATES…")
        {
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
        };
        manageBlacklist.Click += (_, _) => ManageBlacklist();

        _offlineMenu.Items.AddRange(new ToolStripItem[] { installFromFolder, saveBackup, openBlacklist, manageBlacklist });

        _tips.SetToolTip(
            _offlineDrivers,
            "Driver packages from a folder - a USB stick or a saved backup - or write this machine's drivers to one.");

        _apps.Resize += (_, _) => ResizeRows(_apps);
        _drivers.Resize += (_, _) => ResizeRows(_drivers);
        _confirmTimer.Tick += (_, _) => ResetConfirm();
        _rowSweep.Tick += (_, _) =>
        {
            foreach ((ToggleRow row, AppUpgrade _) in _appRows) row.Sweep();
        };

        SyncButtons();
    }

    public override void OnNavigated()
    {
        if (!_firstVisit) return;
        _firstVisit = false;

        // The driver search is the slow half; fold its column on arrival so applications stay usable
        // while Windows Update is still being asked.
        if (_driversCard is not null) _driversCard.Collapsed = true;

        // Both halves refresh on arrival: the driver search is the slower one, so it starts with the
        // winget query rather than waiting for someone to press its own SCAN button.
        _ = ReloadApps(announce: false);
        ReloadDrivers(announce: false);
    }

    /// <summary>
    /// Work started here keeps running when the panel is left, but the wait cursor must not follow the
    /// pointer around the rest of the console: anything still outstanding is dropped on the way out.
    /// </summary>
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (!Visible) Busy.ReleaseWaitCursor();
    }

    /// <summary>
    /// A folded panel keeps a narrow strip on its own side; an open one takes the width that is left,
    /// split evenly between the two when both are open, and both can be folded at once.
    /// </summary>
    private void ApplyCollapse()
    {
        if (_appsCard is null || _driversCard is null || _appsContent is null || _driversContent is null) return;

        bool appsFolded = _appsCard.Collapsed;
        bool driversFolded = _driversCard.Collapsed;

        _appsContent.Visible = !appsFolded;
        _driversContent.Visible = !driversFolded;

        if (appsFolded) _appsLoading.Visible = false;
        if (driversFolded) _driversLoading.Visible = false;

        const int strip = 52;
        bool bothFolded = appsFolded && driversFolded;

        _root.ColumnStyles[0] = appsFolded
            ? new ColumnStyle(SizeType.Absolute, strip)
            : new ColumnStyle(SizeType.Percent, driversFolded ? 100 : 50);

        _root.ColumnStyles[1] = driversFolded
            ? new ColumnStyle(SizeType.Absolute, strip)
            : new ColumnStyle(SizeType.Percent, appsFolded ? 100 : 50);

        // With no percentage column left TableLayoutPanel stretches the last absolute one to fill
        // the row, so a both-folded page parks the spare width in this empty third column instead.
        _root.ColumnStyles[2] = bothFolded
            ? new ColumnStyle(SizeType.Percent, 100)
            : new ColumnStyle(SizeType.Absolute, 0);

        _root.PerformLayout();
    }

    /// <summary>
    /// The marquee and wait cursor only reflect work the user can see: a scan whose panel is folded
    /// keeps running in the background without holding a Busy scope, and folding or unfolding the
    /// panel mid-scan releases or restores that scope through <see cref="SyncPanelBusy"/>.
    /// </summary>
    private void AcquirePanelBusy(ref IDisposable? slot, Card? card)
    {
        if (slot is null && card?.Collapsed == false) slot = Working();
    }

    private void SyncPanelBusy(Card? card, bool busy, ref IDisposable? slot)
    {
        if (busy && card?.Collapsed == false)
        {
            if (slot is null) slot = Working();
        }
        else if (slot is not null)
        {
            slot.Dispose();
            slot = null;
        }
    }

    private static void ReleasePanelBusy(ref IDisposable? slot)
    {
        slot?.Dispose();
        slot = null;
    }

    private static int RowWidth(Panel list) => Math.Max(180, list.ClientSize.Width - 20);

    private static void ResizeRows(Panel list)
    {
        foreach (Control control in list.Controls)
        {
            if (control is ToggleRow row) row.Width = RowWidth(list);
        }
    }

    private async Task ReloadApps(bool announce)
    {
        if (_busyApps) return;

        _busyApps = true;
        _scanApps.Enabled = false;
        SyncButtons();
        SetAppStatus(_winget is null ? "WINGET NOT FOUND" : "QUERYING WINGET\u2026", Theme.TextDim);
        AcquirePanelBusy(ref _appsBusy, _appsCard);
        _appsLoading.Begin();

        try
        {
            (List<AppUpgrade> apps, string? error) = await UpdatesService.ScanAppsAsync();
            _appItems = apps;
            _busyApps = false;
            RebuildApps();
            UpdatesState.Publish(apps.Count, UpdatesState.Drivers);

            if (error is not null)
            {
                SetAppStatus(Theme.Truncate(error.ToUpperInvariant(), 46), Theme.Warn, error.ToUpperInvariant());
            }
            else
            {
                SetAppStatus(
                    apps.Count == 0 ? "EVERYTHING IS CURRENT" : $"{apps.Count} UPDATES AVAILABLE",
                    apps.Count == 0 ? Theme.Good : Theme.Text);
            }

            if (announce)
            {
                ActivityLog.Info($"Update scan: {apps.Count} application upgrades available.");
            }
        }
        catch (Exception ex)
        {
            SetAppStatus(Theme.Truncate(ex.Message.ToUpperInvariant(), 46), Theme.Bad, ex.Message.ToUpperInvariant());
            ActivityLog.Error($"Update scan: {ex.Message}");
        }
        finally
        {
            ReleasePanelBusy(ref _appsBusy);
            _appsLoading.End();
            _busyApps = false;
            _scanApps.Enabled = true;
            SyncButtons();
        }
    }

    private async void ReloadDrivers(bool announce)
    {
        if (_busyDrivers) return;

        _busyDrivers = true;
        _scanDrivers.Enabled = false;
        SyncButtons();
        SetDriverStatus("ASKING WINDOWS UPDATE\u2026", Theme.TextDim);
        AcquirePanelBusy(ref _driverBusy, _driversCard);
        _driversLoading.Begin();

        string? devicesError = null;

        try
        {
            // Windows Update only offers what it decided to publish. A device with no driver at all is
            // not necessarily in that list, so the device tree is read too: it is the only source that
            // can say "this machine has bare hardware" rather than "nothing was offered to you".
            // The enumeration walks every present PnP device and is the slow half of the page, so it
            // only runs while the driver panel is open; a folded panel loads on the query alone.
            bool scanDevices = _driversCard?.Collapsed == false && !_devicesScanned;
            Task<(List<DriverUpdate>, string?)> updatesTask = Task.Run(() => UpdatesService.ScanDrivers());
            Task<(List<DriverDevice>, string?)>? devicesTask = scanDevices
                ? Task.Run(() => DriverDevices.Scan())
                : null;

            (List<DriverDevice> earlyDevices, string? earlyDevicesError) = devicesTask is null
                ? (_driverDevices, null)
                : await devicesTask.ConfigureAwait(false);

            devicesError = earlyDevicesError;

            if (devicesTask is not null)
            {
                _driverDevices = earlyDevices;
                _devicesScanned = true;
            }

            int earlyBare = DriverDevices.CountNeedingAttention(_driverDevices);
            SetDriverStatus(
                $"CHECKING DEVICES • {earlyBare} NEED ATTENTION • ASKING WINDOWS UPDATE…",
                earlyBare > 0 ? Theme.Warn : Theme.TextDim,
                earlyBare > 0 ? DeviceTooltip() : null);

            (List<DriverUpdate> drivers, string? error) = await updatesTask.ConfigureAwait(false);

            _driverItems = drivers;
            _driverError = error;
            _busyDrivers = false;
            RebuildDrivers();
            UpdatesState.Publish(UpdatesState.Apps, drivers.Count);
            RefreshDriverStatus();

            int bare = DriverDevices.CountNeedingAttention(_driverDevices);

            if (announce)
            {
                ActivityLog.Info($"Driver scan: {drivers.Count} driver updates offered by Windows Update, " +
                                 $"{bare} of {earlyDevices.Count} devices without a usable driver.");

                if (devicesError is not null) ActivityLog.Warn($"Device scan: {devicesError}");
            }
        }
        catch (Exception ex)
        {
            SetDriverStatus(Theme.Truncate(ex.Message.ToUpperInvariant(), 46), Theme.Bad, ex.Message.ToUpperInvariant());
            ActivityLog.Error($"Driver scan: {ex.Message}");
        }
        finally
        {
            _driversLoading.End();
            _busyDrivers = false;
            _scanDrivers.Enabled = true;
            SyncButtons();
            SyncPanelBusy(_driversCard, _devicesScanning, ref _driverBusy);
        }
    }

    /// <summary>
    /// The status line for the driver column, shared by the Windows Update scan and the lazy device
    /// scan so whichever finishes last reports the same combined picture.
    /// </summary>
    private void RefreshDriverStatus()
    {
        int bare = DriverDevices.CountNeedingAttention(_driverDevices);

        if (_driverError is not null)
        {
            SetDriverStatus(Theme.Truncate(_driverError.ToUpperInvariant(), 46), Theme.Warn, _driverError.ToUpperInvariant());
            return;
        }

        // No package from Windows Update plus a bare device is not "up to date": it is a machine
        // with hardware Windows cannot drive, and the status line says so instead of staying green.
        string text = _driverItems.Count > 0
            ? $"{_driverItems.Count} DRIVER UPDATES"
            : bare > 0
                ? $"{bare} DEVICE{(bare == 1 ? string.Empty : "S")} NEED A DRIVER"
                : "NO DRIVERS NEED UPDATING";

        Color tone = _driverItems.Count > 0 ? Theme.Text : bare > 0 ? Theme.Warn : Theme.Good;
        SetDriverStatus(text, tone, bare > 0 ? DeviceTooltip() : null);
    }

    /// <summary>
    /// Runs the device-tree enumeration once the driver panel is open for the first time. The scan is
    /// local but walks every present PnP device, so it stays out of the way while the panel is folded
    /// and only holds the marquee while its panel is visible.
    /// </summary>
    private async void EnsureDeviceScan()
    {
        if (_devicesScanning || _devicesScanned || _driversCard?.Collapsed != false || IsDisposed) return;

        _devicesScanning = true;
        SyncPanelBusy(_driversCard, true, ref _driverBusy);

        try
        {
            (List<DriverDevice> devices, string? error) = await Task.Run(() => DriverDevices.Scan());
            if (IsDisposed) return;

            _driverDevices = devices;
            _devicesScanned = true;

            if (error is not null) ActivityLog.Warn($"Device scan: {error}");

            RebuildDrivers();
            if (!_busyDrivers) RefreshDriverStatus();
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Device scan: {ex.Message}");
        }
        finally
        {
            _devicesScanning = false;
            SyncPanelBusy(_driversCard, _busyDrivers, ref _driverBusy);
        }
    }

    /// <summary>
    /// One app upgraded on its own, in the download window placed over this column: the same rows, bar and
    /// cancel as a full pass. A failure asks whether to remove the app instead of leaving a dead row.
    /// </summary>
    private async void UpgradeOne(ToggleRow row, AppUpgrade app)
    {
        if (_busyApps) return;

        if (_winget is null)
        {
            Tools.OpenUrl(UpdatesService.WingetDownloadPage);
            return;
        }

        _busyApps = true;
        _scanApps.Enabled = false;
        SyncButtons();

        (List<DownloadResult> results, bool remove) = RunPassOver(
            _appsContent,
            "UPGRADING",
            app.Id.ToUpperInvariant(),
            new List<DownloadJob>
            {
                new(
                    app.Name,
                    $"{app.VersionLine}  \u2022  {app.ScopeLine}",
                    (progress, token) => UpdatesService.UpgradeAsync(app, progress, token)),
            });

        DownloadResult result = results.Count > 0
            ? results[0]
            : new DownloadResult(app.Name, false, true, "Cancelled.");

        _busyApps = false;
        _scanApps.Enabled = true;
        SyncButtons();

        if (result.Stopped)
        {
            SetAppStatus("CANCELLED", Theme.Warn);
            ActivityLog.Warn($"{app.Name}: upgrade cancelled.");
            return;
        }

        if (!result.Ok)
        {
            string reason = StripAppName(app, result.Message);

            row.Subtitle = Theme.Truncate(reason.ToUpperInvariant(), 78);
            _tips.SetToolTip(row, $"{app.Name}\r\n{reason}");
            SetAppStatus($"FAILED \u2014 {Theme.Truncate(reason.ToUpperInvariant(), 46)}", Theme.Bad, result.Message);
            ActivityLog.Error(result.Message);

            // The window asked whether to keep the app; REMOVE is the answer that needs acting on.
            if (remove)
            {
                ActivityLog.Warn($"{app.Name}: removing it after the failed upgrade.");
                _ = UninstallAsync(app, row);
            }

            return;
        }

        bool verified = await VerifyUpgraded(app);
        if (IsDisposed) return;

        if (verified)
        {
            ActivityLog.Success($"{app.Name} upgraded to {app.Available} \u2014 verified.");
            SetAppStatus($"{Theme.Truncate(app.Name.ToUpperInvariant(), 24)} UPGRADED", Theme.Good, app.Name);
            RemoveAppRow(app);
            return;
        }

        row.Subtitle = $"STILL OFFERED  {app.Current} \u2192 {app.Available}";
        SetAppStatus($"NOT VERIFIED \u2014 {Theme.Truncate(app.Name.ToUpperInvariant(), 24)}", Theme.Warn, app.Name);
        ActivityLog.Warn($"{app.Name}: could not confirm {app.Available}; the row stays.");
    }

    private static string StripAppName(AppUpgrade app, string message) =>
        message.StartsWith($"{app.Name}: ", StringComparison.OrdinalIgnoreCase)
            ? message[(app.Name.Length + 2)..]
            : message;

    /// <summary>
    /// Runs the pass in the download window placed over the applications column, so a single install covers
    /// the list it came from rather than appearing in the middle of the screen.
    /// </summary>
    private (List<DownloadResult> Results, bool Remove) RunPassOver(
        Control? anchor,
        string title,
        string kicker,
        List<DownloadJob> jobs)
    {
        using var form = new DownloadForm(title, kicker, jobs);

        Rectangle area = anchor is null
            ? Screen.FromPoint(MousePosition).WorkingArea
            : anchor.RectangleToScreen(anchor.ClientRectangle);

        form.StartPosition = FormStartPosition.Manual;
        form.Location = area.Location;
        form.ClientSize = area.Size;
        form.ShowDialog(FindForm());

        return (form.Results.ToList(), form.RemoveRequested);
    }

    /// <summary>
    /// Confirms an upgrade actually took. winget is asked again first, but a package whose installed version
    /// it cannot read (it prints "Unknown") stays listed forever, so Add or Remove Programs is the second
    /// opinion: it records the version the installer left behind.
    /// </summary>
    private static async Task<bool> VerifyUpgraded(AppUpgrade app)
    {
        try
        {
            (List<AppUpgrade> apps, string? error) = await UpdatesService.ScanAppsAsync();

            if (error is not null)
            {
                ActivityLog.Warn($"Upgrade verification scan failed: {error}");
            }
            else
            {
                AppUpgrade? offered = apps.FirstOrDefault(candidate =>
                    string.Equals(candidate.Id, app.Id, StringComparison.OrdinalIgnoreCase));

                // No longer offered: nothing left for winget to install.
                if (offered is null) return true;

                // A readable installed version at or past the target settles it.
                if ((CompareVersions(offered.Current, app.Available) ?? -1) >= 0) return true;
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"Upgrade verification scan failed: {ex.Message}");
        }

        try
        {
            return await Task.Run(() => RegistryReachedVersion(app));
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"Upgrade verification failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// True when the uninstall registry records this app at the target version or newer, which is how a
    /// package winget lists with a blank installed version gets confirmed.
    /// </summary>
    private static bool RegistryReachedVersion(AppUpgrade app)
    {
        foreach (ProgramEntry entry in ProgramsService.Load())
        {
            if (entry.Dead || entry.Version.Length == 0) continue;

            bool sameApp =
                string.Equals(entry.DisplayName, app.Name, StringComparison.OrdinalIgnoreCase) ||
                entry.DisplayName.StartsWith(app.Name, StringComparison.OrdinalIgnoreCase) ||
                app.Name.StartsWith(entry.DisplayName, StringComparison.OrdinalIgnoreCase);

            if (sameApp && (CompareVersions(entry.Version, app.Available) ?? -1) >= 0) return true;
        }

        return false;
    }

    /// <summary>
    /// Dotted version comparison, or null when either side carries no version at all ("Unknown", "?").
    /// Null must never count as reached: that is exactly what winget prints for packages it cannot read.
    /// </summary>
    private static int? CompareVersions(string left, string right)
    {
        if (!TryParseVersion(left, out int[] a) || !TryParseVersion(right, out int[] b)) return null;

        int length = Math.Max(a.Length, b.Length);

        for (int index = 0; index < length; index++)
        {
            int x = index < a.Length ? a[index] : 0;
            int y = index < b.Length ? b[index] : 0;
            if (x != y) return x.CompareTo(y);
        }

        return 0;
    }

    private static bool TryParseVersion(string text, out int[] parts)
    {
        var numbers = new List<int>(4);

        foreach (string chunk in text.Split(
                     new[] { '.', '-', '_', ',', ' ', '\t' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(chunk, out int number))
            {
                // Trailing noise such as "6.7.81.0 (x64)" is ignored; leading noise means no version.
                if (numbers.Count > 0) break;

                parts = Array.Empty<int>();
                return false;
            }

            numbers.Add(number);
        }

        parts = numbers.ToArray();
        return parts.Length > 0;
    }

    /// <summary>
    /// Drops a verified app out of the column and out of the list DOWNLOAD ALL would work through, closing
    /// the gap it leaves so no blank slot is left behind.
    /// </summary>
    private void RemoveAppRow(AppUpgrade app)
    {
        ToggleRow? row = RowFor(app);
        int top = row?.Top ?? 0;
        int gap = row is null ? 0 : row.Height + 6;

        _appRows.RemoveAll(pair => ReferenceEquals(pair.App, app));
        _appItems = _appItems.Where(candidate => !ReferenceEquals(candidate, app)).ToList();

        if (row is not null)
        {
            row.Dispose();

            foreach ((ToggleRow other, AppUpgrade _) in _appRows)
            {
                if (other.Top > top) other.Top -= gap;
            }
        }

        int bottom = 4;

        foreach ((ToggleRow other, AppUpgrade _) in _appRows)
        {
            bottom = Math.Max(bottom, other.Bottom);
        }

        if (_appRows.Count == 0)
        {
            bottom = AddEmpty(_apps, "Everything is current.", bottom);
        }

        _apps.AutoScrollMinSize = new Size(0, bottom + 31);
        UpdatesState.Publish(_appItems.Count, UpdatesState.Drivers);
        SyncButtons();
    }

    /// <summary>First press arms the row, a second press within a few seconds removes the app.</summary>
    private void RequestUninstall(ToggleRow row, AppUpgrade app)
    {
        if (_busyApps) return;

        if (!ReferenceEquals(_confirmRow, row))
        {
            ResetConfirm();
            _confirmRow = row;

            // A swapped icon proved too easy to miss: the pad itself says CONFIRM, in a wider pill, and
            // the badge changes too, so the armed state is obvious without reading the status line.
            row.ActionText = "CONFIRM";
            row.ActionGlyph = string.Empty;
            row.ActionTone = Theme.Warn;
            row.BadgeText = "SURE?";
            row.BadgeTone = Theme.Warn;
            _confirmTimer.Start();
            SetAppStatus($"PRESS CONFIRM TO REMOVE {Theme.Truncate(app.Name.ToUpperInvariant(), 22)}", Theme.Warn, app.Name);
            return;
        }

        ResetConfirm();
        _ = UninstallAsync(app, row);
    }

    private void ResetConfirm()
    {
        _confirmTimer.Stop();

        if (_confirmRow is { IsDisposed: false } row)
        {
            row.ActionText = string.Empty;
            row.ActionGlyph = "\uE74D";
            row.ActionTone = Theme.Bad;
            row.BadgeText = "APP";
            row.BadgeTone = Theme.Accent;
        }

        _confirmRow = null;
    }

    /// <summary>winget first; if it refuses the package, the app's own registered uninstaller is started.</summary>
    private async Task UninstallAsync(AppUpgrade app, ToggleRow row)
    {
        if (_busyApps) return;

        _busyApps = true;
        _scanApps.Enabled = false;
        SetRowsEnabled(_appRows.Select(pair => pair.Row), false);
        using IDisposable busy = Working();
        SyncButtons();

        // The row reports the removal itself: the confirm check is gone, the bar sweeps and the badge says
        // REMOVING, so a removal that takes a while never looks like a click that did nothing.
        _rowSweep.Start();
        row.Working = true;
        row.Progress = 0;
        row.ActionGlyph = string.Empty;
        row.BadgeText = "REMOVING";
        row.BadgeTone = Theme.Bad;
        row.Invalidate();

        SetAppStatus($"REMOVING {Theme.Truncate(app.Name.ToUpperInvariant(), 26)}\u2026", Theme.Bad, app.Name);

        bool removed = false;
        bool started = false;
        string failure = string.Empty;

        try
        {
            (bool ok, string message) = await Task.Run(() => UpdatesService.Uninstall(app));

            if (ok)
            {
                removed = true;
                ActivityLog.Success(message);
            }
            else
            {
                // Store packages and apps without a winget uninstaller only remove through their own entry.
                ProgramEntry? entry = await Task.Run(() => FindRegisteredUninstaller(app.Name));

                if (entry is null)
                {
                    failure = message;
                }
                else
                {
                    (bool launched, string launchedMessage) =
                        await Task.Run(() => ProgramsService.Uninstall(entry));

                    // Starting an uninstaller is not a removal: Docker answers its own dialog and leaves
                    // the app installed, so the outcome is only known by looking again afterwards.
                    if (launched)
                    {
                        started = true;
                        ActivityLog.Info(launchedMessage);
                    }
                    else
                    {
                        failure = launchedMessage;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            failure = ex.Message;
            ActivityLog.Error($"Uninstall failed: {ex.Message}");
        }
        finally
        {
            _rowSweep.Stop();
            _busyApps = false;
            _scanApps.Enabled = true;
            SetRowsEnabled(_appRows.Select(pair => pair.Row), true);
            SyncButtons();
        }

        // The rescan is awaited here: whether the app is still offered is the whole answer to the removal.
        await ReloadApps(announce: false);
        if (IsDisposed) return;

        if (!removed && StillOffered(app))
        {
            await OfferBlacklist(
                app,
                started
                    ? "The uninstaller was started, but the app is still listed."
                    : failure,
                failed: !started);
        }
    }

    /// <summary>
    /// A removal that could not be done still has one useful answer: stop offering the package. The id is
    /// remembered, so later scans leave it out, and the row goes now rather than after the next rescan.
    /// </summary>
    private async Task OfferBlacklist(AppUpgrade app, string message, bool failed = true)
    {
        if (failed) ActivityLog.Error(message);
        else ActivityLog.Warn(message);

        DialogResult answer = MessageBox.Show(
            this,
            $"{app.Name} could not be removed.\r\n\r\n{message}\r\n\r\nHide it from the updates list instead?",
            "Removal failed",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button1);

        if (answer != DialogResult.Yes) return;

        if (!UpdatesService.Ignore(app.Id))
        {
            SetAppStatus("COULD NOT HIDE IT", Theme.Bad, message);
            return;
        }

        RemoveAppRow(app);
        ActivityLog.Warn($"{app.Name} hidden from the updates list; it will not be offered again.");

        // Hiding a package changes the list, the count and the nav badge at once, so the scan runs again
        // here instead of leaving them disagreeing until the page is next opened.
        await ReloadApps(announce: false);
        if (IsDisposed) return;

        SetAppStatus($"{Theme.Truncate(app.Name.ToUpperInvariant(), 24)} HIDDEN", Theme.Warn, app.Name);
    }

    /// <summary>True when the scan that just ran still offers this package.</summary>
    private bool StillOffered(AppUpgrade app) => _appItems.Any(item =>
        string.Equals(item.Id, app.Id, StringComparison.OrdinalIgnoreCase));

    private static ProgramEntry? FindRegisteredUninstaller(string name)
    {
        try
        {
            return ProgramsService.Load().FirstOrDefault(entry =>
                !entry.Dead &&
                entry.HasUninstaller &&
                (string.Equals(entry.DisplayName, name, StringComparison.OrdinalIgnoreCase) ||
                 entry.DisplayName.StartsWith(name, StringComparison.OrdinalIgnoreCase) ||
                 name.StartsWith(entry.DisplayName, StringComparison.OrdinalIgnoreCase)));
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"Uninstaller lookup failed: {ex.Message}");
            return null;
        }
    }

    private void RebuildApps()
    {
        _apps.SuspendLayout();
        foreach (Control control in _apps.Controls.Cast<Control>().ToArray()) control.Dispose();
        _apps.Controls.Clear();
        _appRows.Clear();

        int y = 4;

        if (_appItems.Count == 0)
        {
            y = AddEmpty(_apps, _busyApps ? "Scanning\u2026" : "Nothing to upgrade.", y);
        }

        foreach (AppUpgrade app in _appItems)
        {
            // Security-sensitive vendors (browsers, PDF readers, runtimes, remote access)
            // get a WARN badge instead of the plain APP tag — same rule the Software page uses.
            bool security = SecurityRelevance.IsSecuritySensitive(app.Name, app.Id);

            var row = new ToggleRow
            {
                Title = Theme.Truncate(app.Name, 54),
                Subtitle = $"{app.VersionLine}  \u2022  {app.ScopeLine}",
                BadgeText = security ? "SEC" : "APP",
                BadgeTone = security ? Theme.Warn : Theme.Accent,
                Tone = Theme.Accent,
                ActionGlyph = "\uE74D",
                ActionTone = Theme.Bad,
                Action2Glyph = "\uE896",
                Action2Tone = Theme.Accent,
                ShowSwitch = false,
                Tag = app,
            };

            row.ActionClicked += clicked => RequestUninstall(clicked, app);
            row.Action2Clicked += clicked => UpgradeOne(clicked, app);
            string details = $"{app.Name}\r\n{app.VersionLine}  \u2022  {app.Id}\r\nPinned to: {app.ScopeLine}";
            row.ActionHoverChanged += (_, over) => _tips.SetToolTip(row, over ? $"UNINSTALL {app.Name}" : details);
            _tips.SetToolTip(row, $"{app.Name}\r\n{app.VersionLine}  \u2022  {app.Id}\r\nPinned to: {app.ScopeLine}");
            row.SetBounds(4, y, RowWidth(_apps), 62);
            _apps.Controls.Add(row);
            _appRows.Add((row, app));
            y += 68;
        }

        _apps.AutoScrollMinSize = new Size(0, y + 31);
        _apps.ResumeLayout();
        SyncButtons();
    }

    private void RebuildDrivers()
    {
        _drivers.SuspendLayout();
        foreach (Control control in _drivers.Controls.Cast<Control>().ToArray()) control.Dispose();
        _drivers.Controls.Clear();
        _driverRows.Clear();

        int y = 4;

        if (_driverItems.Count == 0)
        {
            y = AddEmpty(_drivers, _busyDrivers ? "Scanning\u2026" : DriverEmptyText(), y);
        }

        foreach (DriverUpdate driver in _driverItems)
        {
            var row = new ToggleRow
            {
                Title = Theme.Truncate(driver.Title, 54),
                Subtitle = driver.DetailLine,
                BadgeText = "DRV",
                BadgeTone = Theme.Violet,
                Tone = Theme.Violet,
                ShowSwitch = false,
                Tag = driver,
            };

            _tips.SetToolTip(row, $"{driver.Title}\r\n{driver.DetailLine}");
            row.SetBounds(4, y, RowWidth(_drivers), 62);
            _drivers.Controls.Add(row);
            _driverRows.Add((row, driver));
            y += 68;
        }

        _drivers.AutoScrollMinSize = new Size(0, y + 31);
        _drivers.ResumeLayout();
        SyncButtons();
    }

    /// <summary>
    /// What the driver list says when Windows Update has nothing. Naming the bare devices is the point:
    /// "No drivers need updating" next to hardware with no driver at all is the sentence that makes a
    /// user stop believing the page.
    /// </summary>
    private string DriverEmptyText()
    {
        List<DriverDevice> bare = _driverDevices.Where(device => device.NeedsAttention).ToList();
        if (bare.Count == 0) return "No drivers need updating.";

        string names = string.Join(", ", bare.Take(3).Select(device => device.Name));
        if (bare.Count > 3) names += $" and {bare.Count - 3} more";

        return $"No Windows Update driver packages. {bare.Count} device{(bare.Count == 1 ? " has" : "s have")} " +
               $"no usable driver: {names}.";
    }

    /// <summary>The devices that need attention, one line each, for the status tooltip.</summary>
    private string DeviceTooltip()
    {
        return string.Join("\r\n", _driverDevices
            .Where(device => device.NeedsAttention)
            .Take(8)
            .Select(device => $"{device.Name} ({device.DeviceClass}) - {device.StatusLine}"));
    }

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

    /// <summary>
    /// Downloads and installs every listed app in one pass. The work stays on the message loop instead
    /// of behind a wait cursor, so the panel can be left while it continues; each row keeps its own bar
    /// and CANCEL stops the pass, killing winget's transfer if one is in flight.
    /// </summary>
    /// <summary>
    /// Downloads and installs every listed app through the download window, which owns the queue, the live
    /// bars and the cancel. The page reflects what came back once the window closes.
    /// </summary>
    private async void DownloadAllApps()
    {
        if (_busyApps) return;

        List<AppUpgrade> queue = _appItems.ToList();
        if (queue.Count == 0) return;

        if (_winget is null)
        {
            Tools.OpenUrl(UpdatesService.WingetDownloadPage);
            return;
        }

        List<DownloadResult> results = RunPass(
            "INSTALLING UPDATES",
            $"WINGET \u2022 {queue.Count} PACKAGE{(queue.Count == 1 ? string.Empty : "S")}",
            queue.Select(app => new DownloadJob(
                app.Name,
                $"{app.VersionLine}  \u2022  {app.ScopeLine}",
                (progress, token) => UpdatesService.UpgradeAsync(app, progress, token))).ToList());

        ReportAppResults(results);

        // The rescan is awaited because it is the verdict: an update that reported success but is
        // still offered afterwards did not take, and it gets the same hide-or-keep choice.
        await ReloadApps(announce: false);
        if (IsDisposed) return;

        List<AppUpgrade> stuck = results
            .Where(result => result.Ok && !result.Stopped)
            .Select(result => queue.FirstOrDefault(app =>
                string.Equals(app.Name, result.Name, StringComparison.OrdinalIgnoreCase)))
            .Where(app => app is not null)
            .Select(app => _appItems.FirstOrDefault(item =>
                string.Equals(item.Id, app!.Id, StringComparison.OrdinalIgnoreCase)))
            .Where(item => item is not null)
            .Select(item => item!)
            .Distinct()
            .ToList();

        await OfferStillOfferedChoice(stuck);
    }

    /// <summary>
    /// A package that reported success but is still offered after the rescan did not really update -
    /// the installer ran and the version stayed. That usually means a broken install rather than a
    /// stubborn version, so the choice is removal first, hiding second, keeping last.
    /// </summary>
    private async Task OfferStillOfferedChoice(IReadOnlyList<AppUpgrade> apps)
    {
        if (apps.Count == 0) return;

        string names = string.Join("\r\n", apps.Take(6).Select(app => $"• {app.Name}"));
        if (apps.Count > 6) names += $"\r\n• and {apps.Count - 6} more";

        // Options sit left to right: REMOVE, HIDE, KEEP — KEEP is the safe Enter/default answer.
        int choice = OptionPromptForm.Ask(
            this,
            "Still listed after update",
            $"{apps.Count} update{(apps.Count == 1 ? "" : "s")} reported success, but a rescan still offers them:\r\n\r\n{names}\r\n\r\n" +
            "That usually means a broken install. What should happen to them?",
            new[] { "REMOVE", "HIDE", "KEEP" },
            defaultIndex: 2);

        if (choice == 0)
        {
            await RemoveStillOfferedAsync(apps);
            return;
        }

        if (choice != 1) return;

        int hidden = 0;

        foreach (AppUpgrade app in apps)
        {
            if (!UpdatesService.Ignore(app.Id)) continue;

            hidden++;
            RemoveAppRow(app);
            ActivityLog.Warn($"{app.Name} hidden from the updates list after a rescan still offered it.");
        }

        if (hidden == 0) return;

        await ReloadApps(announce: false);
        if (IsDisposed) return;

        SetAppStatus($"{hidden} HIDDEN AFTER RESCAN", Theme.Warn);
    }

    /// <summary>
    /// Removes each still-offered app through the same per-app uninstall the row bin uses, so every
    /// removal gets its row feedback, its own rescan verdict and the hide-offer if it fails.
    /// </summary>
    private async Task RemoveStillOfferedAsync(IReadOnlyList<AppUpgrade> apps)
    {
        ActivityLog.Warn($"{apps.Count} app{(apps.Count == 1 ? string.Empty : "s")} still offered after " +
                         "update — removing them.");

        foreach (AppUpgrade app in apps)
        {
            if (IsDisposed) return;

            (ToggleRow Row, AppUpgrade App) pair = _appRows.FirstOrDefault(p =>
                string.Equals(p.App.Id, app.Id, StringComparison.OrdinalIgnoreCase));

            if (pair.Row is null) continue;

            await UninstallAsync(pair.App, pair.Row);
        }

        if (IsDisposed) return;
        await ReloadApps(announce: false);
    }

    /// <summary>Shows the download window and comes back with what each job reported.</summary>
    private List<DownloadResult> RunPass(string title, string kicker, List<DownloadJob> jobs)
    {
        using var form = new DownloadForm(title, kicker, jobs);
        form.ShowDialog(FindForm());

        return form.Results.ToList();
    }

    /// <summary>Logs the pass and puts its outcome on the toolbar, with every failure reason in the tooltip.</summary>
    private void ReportAppResults(List<DownloadResult> results)
    {
        int applied = 0;

        foreach (DownloadResult result in results)
        {
            if (result.Stopped) continue;

            if (result.Ok)
            {
                applied++;
                ActivityLog.Success(result.Message);
            }
            else
            {
                ActivityLog.Error(result.Message);
            }
        }

        if (results.Any(result => result.Stopped))
        {
            SetAppStatus("CANCELLED", Theme.Warn);
            ActivityLog.Warn($"Download all stopped: {applied} of {results.Count} applied.");
            return;
        }

        List<string> failures = results.Where(result => !result.Ok).Select(result => result.Message).ToList();

        if (failures.Count == 0)
        {
            SetAppStatus($"{applied} OF {results.Count} UPGRADED", Theme.Good);
            return;
        }

        SetAppStatus(
            $"{applied} OF {results.Count} UPGRADED \u2014 {failures.Count} FAILED",
            Theme.Warn,
            string.Join("\r\n", failures));
    }
    /// <summary>The row that currently shows this app, or null once the list has been rebuilt.</summary>
    private ToggleRow? RowFor(AppUpgrade app)
    {
        foreach ((ToggleRow row, AppUpgrade candidate) in _appRows)
        {
            if (ReferenceEquals(candidate, app)) return row;
        }

        return null;
    }

    /// <summary>The row takes over as the progress surface while its package downloads and installs.</summary>
    private static void BeginRowProgress(ToggleRow row)
    {
        row.Working = true;
        row.Progress = 0;
        row.ActionText = string.Empty;
        row.ActionGlyph = string.Empty;
        row.BadgeText = "0%";
        row.BadgeTone = Theme.Accent;
        row.Invalidate();
    }

    private static void ReportRowProgress(ToggleRow row, int percent)
    {
        if (percent <= 0) return;

        row.Progress = percent;
        row.BadgeText = $"{percent}%";
    }

    private static void EndRowProgress(ToggleRow row, bool ok, bool stopped)
    {
        row.Working = false;
        row.Progress = 0;
        row.ActionText = string.Empty;
        row.ActionGlyph = stopped ? string.Empty : "\uE74D";
        row.ActionTone = Theme.Bad;
        row.BadgeText = stopped ? "STOPPED" : ok ? "DONE" : "FAILED";
        row.BadgeTone = stopped ? Theme.Warn : ok ? Theme.Good : Theme.Bad;
        row.Invalidate();
    }

    /// <summary>
    /// Every driver update, each as its own job in the download window: the agent's asynchronous calls
    /// report real bytes and install progress, and CANCEL aborts the driver already in flight.
    /// </summary>
    private void DownloadAllDrivers()
    {
        if (_busyDrivers) return;

        List<DriverUpdate> queue = _driverItems.ToList();
        if (queue.Count == 0) return;

        if (!Native.IsElevated)
        {
            SetDriverStatus("NEEDS ADMIN \u2014 RESTART AS ADMIN", Theme.Warn);
            ActivityLog.Warn("Driver install needs an elevated run.");
            return;
        }

        List<DownloadResult> results = RunPass(
            "INSTALLING DRIVERS",
            $"WINDOWS UPDATE \u2022 {queue.Count} DRIVER{(queue.Count == 1 ? string.Empty : "S")}",
            queue.Select(driver => new DownloadJob(
                driver.Title,
                driver.DetailLine,
                (progress, token) => Task.Run(() =>
                {
                    (bool ok, string message) = UpdatesService.Install(new[] { driver }, token, progress);
                    return (ok, message);
                }, token))).ToList());

        ReportDriverResults(results);
        ReloadDrivers(announce: false);
    }

    /// <summary>
    /// Offline install: a folder of .inf packages - a vendor's USB stick, a pnputil export or a backup
    /// made here - is matched against this machine's devices, and only the matches are offered. This is
    /// the only path that still works when the device with no driver is the network adapter.
    /// </summary>
    private async void InstallFromFolder()
    {
        if (_busyDrivers) return;

        if (!Native.IsElevated)
        {
            SetDriverStatus("NEEDS ADMIN \u2014 RESTART AS ADMIN", Theme.Warn);
            ActivityLog.Warn("Installing offline driver packages needs an elevated run.");
            return;
        }

        string? folder = ChooseFolder(
            "Pick the folder that holds the driver packages (.inf): a USB stick, a pnputil export or a saved backup.",
            newFolder: false);

        if (folder is null) return;

        _busyDrivers = true;
        _scanDrivers.Enabled = false;
        SyncButtons();
        SetDriverStatus("READING THE FOLDER\u2026", Theme.TextDim);
        using IDisposable busy = Working();
        _driversLoading.Begin();

        List<OfflineDriverPackage> packages;
        List<DriverDevice> devices;
        int infCount;
        string? error;

        try
        {
            (packages, infCount, error, devices) = await Task.Run(() =>
            {
                List<DriverDevice> scanned = _driverDevices;
                if (scanned.Count == 0) (scanned, _) = DriverDevices.Scan();

                (List<OfflineDriverPackage> matched, int count, string? failure) =
                    DriverRepository.ScanFolder(folder, scanned);

                return (matched, count, failure, scanned);
            });
        }
        catch (Exception ex)
        {
            SetDriverStatus(Theme.Truncate(ex.Message.ToUpperInvariant(), 46), Theme.Bad, ex.Message);
            ActivityLog.Error($"Offline driver scan failed: {ex.Message}");
            return;
        }
        finally
        {
            _driversLoading.End();
            _busyDrivers = false;
            _scanDrivers.Enabled = true;
            SyncButtons();
        }

        if (error is not null)
        {
            SetDriverStatus(Theme.Truncate(error.ToUpperInvariant(), 46), Theme.Bad, error);
            ActivityLog.Error($"Offline drivers: {error}");
            return;
        }

        _driverDevices = devices;

        if (packages.Count == 0)
        {
            SetDriverStatus(
                "NO PACKAGES FOR THIS PC",
                Theme.Warn,
                $"{infCount} .inf file(s) under {folder}; none match a device on this machine.");

            ActivityLog.Warn($"Offline folder: {infCount} driver packages, none for this machine.");
            return;
        }

        ActivityLog.Info($"Offline folder: {packages.Count} of {infCount} packages match this machine.");

        List<DownloadResult> results = RunPass(
            "INSTALLING DRIVERS",
            $"OFFLINE \u2022 {packages.Count} PACKAGE{(packages.Count == 1 ? string.Empty : "S")}",
            packages.Select(package => new DownloadJob(
                package.Title,
                package.DetailLine,
                (progress, token) => Task.Run(
                    () => DriverRepository.Install(package, progress, token), token))).ToList());

        ReportDriverResults(results);
        ReloadDrivers(announce: false);
    }

    /// <summary>The other half of the offline story: export the driver store so a rebuild needs no network.</summary>
    private void SaveDriverBackup()
    {
        if (_busyDrivers) return;

        if (!Native.IsElevated)
        {
            SetDriverStatus("NEEDS ADMIN \u2014 RESTART AS ADMIN", Theme.Warn);
            ActivityLog.Warn("Saving a driver backup needs an elevated run.");
            return;
        }

        string? folder = ChooseFolder(
            "Pick where the driver backup should go. Every driver package in the store is exported into it.",
            newFolder: true);

        if (folder is null) return;

        SetDriverStatus("EXPORTING THE DRIVER STORE\u2026", Theme.TextDim);

        List<DownloadResult> results = RunPass(
            "SAVING DRIVERS",
            "PNPUTIL \u2022 DRIVER STORE",
            new List<DownloadJob>
            {
                new(
                    "Driver backup",
                    folder,
                    (progress, token) => Task.Run(() => DriverRepository.Export(folder, progress, token), token)),
            });

        DownloadResult result = results.Count > 0
            ? results[0]
            : new DownloadResult("Driver backup", false, true, "Cancelled.");

        if (result.Stopped)
        {
            SetDriverStatus("BACKUP CANCELLED", Theme.Warn);
            ActivityLog.Warn("Driver backup cancelled.");
            return;
        }

        if (!result.Ok)
        {
            SetDriverStatus("BACKUP FAILED", Theme.Bad, result.Message);
            ActivityLog.Error(result.Message);
            return;
        }

        SetDriverStatus("DRIVER BACKUP SAVED", Theme.Good, folder);
        ActivityLog.Success(result.Message);
    }

    private string? ChooseFolder(string description, bool newFolder)
    {
        using var picker = new FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = newFolder,
        };

        return picker.ShowDialog(FindForm()) == DialogResult.OK ? picker.SelectedPath : null;
    }

    /// <summary>Logs a driver pass and puts its outcome on the toolbar; every failure reason in the tooltip.</summary>
    private void ReportDriverResults(List<DownloadResult> results)
    {
        int installed = 0;

        foreach (DownloadResult result in results)
        {
            if (result.Stopped) continue;

            if (result.Ok)
            {
                installed++;
                ActivityLog.Success(result.Message);
            }
            else
            {
                ActivityLog.Error(result.Message);
            }
        }

        if (results.Any(result => result.Stopped))
        {
            SetDriverStatus($"CANCELLED \u2014 {installed} INSTALLED", Theme.Warn);
            ActivityLog.Warn($"Driver install stopped: {installed} of {results.Count} installed.");
            return;
        }

        if (installed == results.Count)
        {
            SetDriverStatus(
                $"{installed} DRIVER PACKAGE{(installed == 1 ? string.Empty : "S")} INSTALLED",
                Theme.Good);
            return;
        }

        SetDriverStatus(
            $"{installed} OF {results.Count} DRIVERS INSTALLED",
            Theme.Warn,
            string.Join("\r\n", results.Where(result => !result.Ok).Select(result => result.Message)));
    }

    private static void SetRowsEnabled(IEnumerable<ToggleRow> rows, bool enabled)
    {
        foreach (ToggleRow row in rows)
        {
            row.Enabled = enabled;
            row.Invalidate();
        }
    }

    /// <summary>Status line kept short for the toolbar, with the full message in a tooltip.</summary>
    private void SetAppStatus(string text, Color tone, string? full = null)
    {
        _appStatus.Text = text;
        _appStatus.ForeColor = tone;
        _tips.SetToolTip(_appStatus, full ?? text);
    }

    private void SetDriverStatus(string text, Color tone, string? full = null)
    {
        _driverStatus.Text = text;
        _driverStatus.ForeColor = tone;
        _tips.SetToolTip(_driverStatus, full ?? text);
    }

    private void SyncButtons()
    {
        bool missingWinget = _winget is null;

        _downloadApps.Text = missingWinget ? "GET WINGET" : "DOWNLOAD ALL";
        _downloadApps.Glyph = missingWinget ? string.Empty : "\uE896";
        _downloadApps.Enabled = !_busyApps && (missingWinget || _appItems.Count > 0);
        _downloadApps.Invalidate();

        _downloadDrivers.Enabled = !_busyDrivers && _driverItems.Count > 0;
        _downloadDrivers.Invalidate();

        _offlineDrivers.Enabled = !_busyDrivers;
        _offlineDrivers.Invalidate();
    }
}
