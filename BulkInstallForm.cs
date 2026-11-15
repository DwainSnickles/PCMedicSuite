namespace PC_Medic;

/// <summary>
/// Bulk installer: a folder of vendor installers — Dell update packages, MSI and MSU hotfixes — ticked
/// off one at a time. Each format gets the switch set that keeps it quiet, and every restart an
/// installer asks for is held back until the last package has finished, so a mandatory restart cannot
/// land in the middle of the batch.
/// </summary>
internal sealed partial class BulkInstallForm : Form
{
    private const int Wide = 960;
    private const int Tall = 620;

    private readonly List<(ToggleRow Row, BulkPackage Package)> _rows = new();
    private List<BulkPackage> _packages = new();

    private bool _silentMode = true;
    private bool _deferRestarts = true;
    private bool _running;

    public BulkInstallForm()
    {
        Text = "Bulk installer";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        ClientSize = new Size(Wide, Tall);
        MinimumSize = new Size(780, 520);
        KeyPreview = true;

        InitializeComponent();

        _help.Click += (_, _) => HelpForm.Open(this, "bulk");
        _tips.SetToolTip(_help, "Opens the manual at the bulk installer topic: switches, exit codes and restart handling.");

        _pickFolder.Click += (_, _) => PickFolder();
        _addFiles.Click += (_, _) => AddFiles();
        _all.Click += (_, _) => SetAll(true);
        _none.Click += (_, _) => SetAll(false);
        _silent.Click += (_, _) =>
        {
            _silentMode = !_silentMode;
            RefreshRows();
        };
        _defer.Click += (_, _) =>
        {
            _deferRestarts = !_deferRestarts;
            SyncOptions();
        };
        _run.Click += (_, _) => RunSelected();
        _restart.Click += (_, _) => RestartNow();
        _close.Click += (_, _) => Close();

        _restart.Tone = Theme.Warn;

        _tips.SetToolTip(
            _pickFolder,
            "Scan a folder for .exe, .msi and .msu installers, including subfolders.");
        _tips.SetToolTip(_addFiles, "Add individual installers from anywhere on this PC.");
        _tips.SetToolTip(
            _silent,
            "Silent runs the package with the switch its format documents. Off runs it with its own window.");
        _tips.SetToolTip(
            _defer,
            "Holds every restart back until the batch finishes: pending shutdowns are cancelled and Windows " +
            "Update is stopped from restarting on its own. The previous setting is put back at the end.");

        _list.Resize += (_, _) => SizeRows();
        Resize += (_, _) => Arrange();

        SyncOptions();
        Rebuild();
        SetStatus("PICK A FOLDER OF INSTALLERS TO START", Theme.TextDim);
        Arrange();
    }

    // ------------------------------------------------------------------ layout

    private void Arrange()
    {
        int wide = ClientSize.Width;
        int tall = ClientSize.Height;

        _pickFolder.SetBounds(18, 88, _pickFolder.Width, 30);
        _addFiles.SetBounds(_pickFolder.Right + 8, 88, _addFiles.Width, 30);
        _all.SetBounds(_addFiles.Right + 8, 88, _all.Width, 30);
        _none.SetBounds(_all.Right + 8, 88, _none.Width, 30);

        // The blurb wraps rather than clips: the window can be dragged down to MinimumSize.
        _subtitle.SetBounds(20, 44, Math.Max(200, wide - 40), 36);

        _status.SetBounds(18, 126, wide - 36, 18);
        _list.SetBounds(16, 150, wide - 32, Math.Max(80, tall - 150 - 122));

        _silent.SetBounds(18, tall - 110, _silent.Width, 30);
        _defer.SetBounds(_silent.Right + 10, tall - 110, _defer.Width, 30);

        _run.SetBounds(18, tall - 52, _run.Width, 34);
        _restart.SetBounds(_run.Right + 10, tall - 52, _restart.Width, 34);
        _close.SetBounds(wide - 18 - _close.Width, tall - 52, _close.Width, 34);
        _help.SetBounds(_close.Left - 8 - _help.Width, tall - 52, _help.Width, 34);
    }

    private void SizeRows()
    {
        foreach ((ToggleRow row, BulkPackage _) in _rows) row.Width = RowWidth();

        // Rows narrowed during a resize leave the scroll range as it was, and the list keeps the
        // horizontal bar it grew while they overhung. Recomputing the range drops it again.
        _list.PerformLayout();
    }

    /// <summary>
    /// The vertical scrollbar is always reserved: rows sized while it is still absent would overhang the
    /// client area by its width, and the list would answer by growing a pointless horizontal scrollbar.
    /// </summary>
    private int RowWidth() =>
        Math.Max(220, _list.ClientSize.Width - 22 - SystemInformation.VerticalScrollBarWidth);

    // ------------------------------------------------------------------ loading

    private void PickFolder()
    {
        using var picker = new FolderBrowserDialog
        {
            Description = "Pick the folder that holds the installers — a downloaded update set, a USB stick, a driver bundle.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };

        if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;

        LoadFolder(picker.SelectedPath);
    }

    private void LoadFolder(string folder)
    {
        using IDisposable busy = Busy.Enter(this);

        try
        {
            List<BulkPackage> found = BulkInstaller.ScanFolder(folder, recursive: true);

            _packages = found;
            Rebuild();

            SetStatus(
                found.Count == 0
                    ? "NO INSTALLERS IN THAT FOLDER"
                    : $"{found.Count} INSTALLER{(found.Count == 1 ? string.Empty : "S")} · {Shorten(folder)}",
                found.Count == 0 ? Theme.Warn : Theme.TextDim);
        }
        catch (Exception ex)
        {
            LoadFailed(ex);
        }
    }

    private void AddFiles()
    {
        using var picker = new OpenFileDialog
        {
            Title = "Add installers",
            Filter = "Installers (*.exe;*.msi;*.msu)|*.exe;*.msi;*.msu|All files (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true,
        };

        if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            foreach (string file in picker.FileNames)
            {
                if (!BulkInstaller.IsInstaller(file)) continue;
                if (_packages.Any(existing => string.Equals(existing.Path, file, StringComparison.OrdinalIgnoreCase))) continue;

                _packages.Add(new BulkPackage(file));
            }

            _packages.Sort((left, right) =>
                string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));

            Rebuild();
            SetStatus($"{_packages.Count} INSTALLER{(_packages.Count == 1 ? string.Empty : "S")}", Theme.TextDim);
        }
        catch (Exception ex)
        {
            LoadFailed(ex);
        }
    }

    private void LoadFailed(Exception ex)
    {
        SetStatus(Theme.Truncate(ex.Message.ToUpperInvariant(), 70), Theme.Bad);
        ActivityLog.Error($"Bulk installer scan: {ex.Message}");
    }

    private void Rebuild()
    {
        _list.SuspendLayout();
        foreach (Control control in _list.Controls.Cast<Control>().ToArray()) control.Dispose();
        _list.Controls.Clear();
        _rows.Clear();

        int y = 4;

        if (_packages.Count == 0)
        {
            var empty = new Label
            {
                Text = "Pick a folder of installers — Dell update packages, MSI or MSU hotfixes.",
                Font = Theme.MonoSmall,
                ForeColor = Theme.TextDim,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(10, y + 8),
            };

            _list.Controls.Add(empty);
            y += 40;
        }

        foreach (BulkPackage package in _packages)
        {
            var row = new ToggleRow
            {
                Title = Theme.Truncate(package.Name, 78),
                Subtitle = RowDetail(package),
                BadgeText = package.KindLabel,
                BadgeTone = ToneFor(package),
                Tone = Theme.Accent,
                Clickable = true,
                ShowSwitch = true,
                Checked = true,
                Tag = package,
            };

            row.SetBounds(6, y, RowWidth(), 56);

            row.Toggled += (clicked, on) =>
            {
                clicked.Checked = on;
                SyncStatus();
            };

            row.RowClicked += clicked =>
            {
                clicked.Checked = !clicked.Checked;
                clicked.Invalidate();
                SyncStatus();
            };

            _tips.SetToolTip(row, $"{package.Path}\r\n{package.KindLabel} · {package.SizeText}\r\n{RowDetail(package)}");

            _list.Controls.Add(row);
            _rows.Add((row, package));
            y += 62;
        }

        _list.AutoScrollMinSize = new Size(0, y + 8);
        _list.ResumeLayout(false);

        // AutoScroll works its range out during a layout pass, and rows added while the panel had layout
        // suspended leave that range stale: without this the list never grows a scrollbar and everything
        // past the first screenful of installers is unreachable.
        _list.PerformLayout();

        SyncStatus();
    }

    /// <summary>Rewrites the subtitle and badge after a switch change, since the arguments change with it.</summary>
    private void RefreshRows()
    {
        foreach ((ToggleRow row, BulkPackage package) in _rows)
        {
            row.Subtitle = RowDetail(package);
            row.BadgeText = package.KindLabel;
            row.BadgeTone = ToneFor(package);
            row.Invalidate();
            _tips.SetToolTip(row, $"{package.Path}\r\n{package.KindLabel} · {package.SizeText}\r\n{RowDetail(package)}");
        }

        SyncOptions();
    }

    private string RowDetail(BulkPackage package) =>
        Theme.Truncate($"{package.SizeText} · {ModeText(package)}", 94);

    /// <summary>The switch set the package will get, or the note that it opens its own window.</summary>
    private string ModeText(BulkPackage package)
    {
        string args = package.Arguments(_silentMode, "<log>");
        if (args.Length == 0) return "its own window";

        // A guessed switch is said out loud: /S is right for many installers, not all of them.
        return _silentMode && !package.HasKnownSilentSwitch ? args + " (guessed)" : args;
    }

    private static Color ToneFor(BulkPackage package) => package.Kind switch
    {
        BulkKind.DellDup => Theme.Accent,
        BulkKind.Msi => Theme.Violet,
        BulkKind.Msu => Theme.Warn,
        _ => Theme.TextDim,
    };

    private void SetAll(bool on)
    {
        foreach ((ToggleRow row, BulkPackage _) in _rows)
        {
            row.Checked = on;
            row.Invalidate();
        }

        SyncStatus();
    }

    private void SyncStatus()
    {
        if (_packages.Count == 0) return;

        int ticked = _rows.Count(pair => pair.Row.Checked);
        SetStatus($"{ticked} OF {_packages.Count} TICKED", ticked == 0 ? Theme.Warn : Theme.TextDim);
    }

    private void SyncOptions()
    {
        _silent.Text = _silentMode ? "SILENT: ON" : "SILENT: OFF";
        _silent.Tone = _silentMode ? Theme.Accent : Theme.TextDim;
        _silent.Invalidate();

        _defer.Text = _deferRestarts ? "DEFER RESTARTS: ON" : "DEFER RESTARTS: OFF";
        _defer.Tone = _deferRestarts ? Theme.Good : Theme.TextDim;
        _defer.Invalidate();

        _defer.Enabled = !_running;
        _silent.Enabled = !_running;
    }

    private void SetStatus(string text, Color tone)
    {
        _status.Text = text;
        _status.ForeColor = tone;
        _status.Invalidate();
    }

    // ------------------------------------------------------------------ running

    private void RunSelected()
    {
        if (_running) return;

        List<BulkPackage> queue = _rows.Where(pair => pair.Row.Checked).Select(pair => pair.Package).ToList();

        if (queue.Count == 0)
        {
            SetStatus("TICK THE INSTALLERS YOU WANT FIRST", Theme.Warn);
            return;
        }

        if (!Native.IsElevated)
        {
            SetStatus("NEEDS ADMIN — PRESS ELEVATE IN THE HEADER, THEN COME BACK", Theme.Warn);
            ActivityLog.Warn("Bulk install needs administrator rights; nothing was started.");
            return;
        }

        _running = true;
        _run.Enabled = false;
        _restart.Visible = false;
        _restart.Enabled = true;
        SyncOptions();
        SetStatus($"RUNNING {queue.Count} INSTALLER{(queue.Count == 1 ? string.Empty : "S")}…", Theme.TextDim);

        var heldRestarts = new List<string>();

        using RestartBarrier? barrier = _deferRestarts ? RestartBarrier.Engage() : null;

        using (KeepAwake.Enter(keepDisplayAwake: true))
        {
            List<DownloadJob> jobs = queue.Select(package => new DownloadJob(
                package.Name,
                $"{package.KindLabel} · {package.SizeText} · {ModeText(package)}",
                async (progress, token) =>
                {
                    BulkOutcome outcome = await BulkInstaller.RunAsync(package, _silentMode, progress, token);

                    if (outcome.RestartNeeded) heldRestarts.Add(package.Name);

                    return (outcome.Ok, outcome.Message);
                })).ToList();

            using var pass = new DownloadForm(
                "INSTALLING UPDATES",
                $"BULK · {jobs.Count} PACKAGE{(jobs.Count == 1 ? string.Empty : "S")}" +
                (_deferRestarts ? " · RESTARTS HELD" : string.Empty),
                jobs);

            pass.ShowDialog(FindForm());

            ApplyResults(pass.Results.ToList());
        }

        _running = false;
        _run.Enabled = true;
        SyncOptions();

        if (barrier is { AbortedRestarts: > 0 })
        {
            ActivityLog.Warn($"Bulk install caught {barrier.AbortedRestarts} restart(s) before they could fire.");
        }

        // A restart is only offered now, with everything installed, never in the middle of the batch.
        bool pending = heldRestarts.Count > 0 || SafeRebootPending();
        _restart.Visible = pending;

        if (pending)
        {
            SetStatus("ALL DONE — RESTART HELD UNTIL NOW", Theme.Warn);
            ActivityLog.Warn("Bulk install finished; a restart is waiting to finish the packages.");
        }
        else
        {
            SetStatus("ALL DONE — NO RESTART NEEDED", Theme.Good);
        }
    }

    /// <summary>Puts each package's outcome on its own row, so the list still explains itself after the pass.</summary>
    private void ApplyResults(IReadOnlyList<DownloadResult> results)
    {
        foreach (DownloadResult result in results)
        {
            (ToggleRow Row, BulkPackage Package) match = _rows.FirstOrDefault(pair =>
                string.Equals(pair.Package.Name, result.Name, StringComparison.OrdinalIgnoreCase));

            if (match.Row is null) continue;

            match.Row.BadgeText = result.Stopped ? "STOPPED" : result.Ok ? "DONE" : "FAILED";
            match.Row.BadgeTone = result.Stopped ? Theme.Warn : result.Ok ? Theme.Good : Theme.Bad;

            if (!result.Ok && !result.Stopped)
            {
                match.Row.Subtitle = Theme.Truncate(result.Message.ToUpperInvariant(), 84);
            }

            match.Row.Checked = false;
            match.Row.Invalidate();
        }
    }

    private void RestartNow()
    {
        DialogResult answer = MessageBox.Show(
            this,
            "Restart now to finish the packages that asked for it?" + Environment.NewLine + Environment.NewLine +
            "PC Medic schedules the restart with a 60-second countdown, so 'shutdown /a' can still cancel it.",
            "Restart",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes) return;

        if (RebootProbe.Restart(TimeSpan.FromSeconds(60), "the bulk install finished and needs a restart"))
        {
            _restart.Enabled = false;
            SetStatus("RESTARTING IN 60 SECONDS — RUN shutdown /a TO CANCEL", Theme.Warn);
        }
        else
        {
            SetStatus("THE RESTART COULD NOT BE SCHEDULED", Theme.Bad);
        }
    }

    private static bool SafeRebootPending()
    {
        try { return RebootProbe.IsRebootPending(); }
        catch (Exception ex)
        {
            Log.Warn("Reboot probe failed after the bulk run: " + ex.Message);
            return false;
        }
    }

    private static string Shorten(string path) => Theme.Truncate(path, 64);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape && !_running) Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tips.Dispose();
        base.Dispose(disposing);
    }
}
