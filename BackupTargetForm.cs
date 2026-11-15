namespace PC_Medic;

/// <summary>What the wizard decided: where to read from, where the image goes, and which stick carries the pointer.</summary>
internal sealed record BackupPlan(
    string SourceRoot,
    string ImagePath,
    string Compression,
    string? RecoveryUsbRoot);

/// <summary>
/// Back up to a disk: a small wizard for the case this suite is actually used in - a full Windows
/// image that cannot fit on the 16 GB recovery stick beside it.
///
/// It picks one source volume, one destination disk (external and USB disks are offered here, not
/// just fixed ones), works out roughly how big the image will be and whether the destination has the
/// room, and optionally stamps a recovery stick with a pointer to the image. The capture itself runs
/// on the Backup page, so there is one imaging path and one progress surface.
/// </summary>
internal sealed partial class BackupTargetForm : Form
{
    private readonly List<DriveInfo> _sources = new();
    private readonly List<DriveInfo> _targets = new();
    private readonly List<DriveInfo> _sticks = new();

    private int _sourceIndex;
    private int _targetIndex;
    private int _stickIndex = -1;   // -1 is "no stick"
    private string _compression = "fast";
    private string _imagePath = string.Empty;
    private bool _pathManual;

    public BackupTargetForm(string? preferredSource, string compression)
    {
        _compression = compression;

        Text = "Back up to disk";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(720, 470);
        MinimumSize = new Size(680, 500);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        KeyPreview = true;

        InitializeComponent();

        _help.Click += (_, _) => HelpForm.Open(this, "backup-disk");
        _tips.SetToolTip(_help, "Opens the manual at the backup wizard topic.");
        _tips.SetToolTip(
            _recoveryDrive,
            "Opens Windows' own \"Create a recovery drive\" tool, which adds Startup Repair and Reset this PC " +
            "to the same stick. Tick \"Back up system files to the recovery drive\" to include them.");

        _cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        _sourcePick.Click += (_, _) =>
        {
            if (_sources.Count == 0) return;
            _sourceIndex = (_sourceIndex + 1) % _sources.Count;
            RefreshPlan();
        };

        _targetPick.Click += (_, _) =>
        {
            if (_targets.Count == 0) return;
            _targetIndex = (_targetIndex + 1) % _targets.Count;
            _pathManual = false;
            RefreshPlan();
        };

        _compressionPick.Click += (_, _) =>
        {
            _compression = _compression switch { "fast" => "maximum", "maximum" => "none", _ => "fast" };
            RefreshPlan();
        };

        _stickPick.Click += (_, _) =>
        {
            if (_sticks.Count == 0) return;

            // Cycle through "none" and each stick, so the pointer is opt-in but one press away.
            _stickIndex++;
            if (_stickIndex >= _sticks.Count) _stickIndex = -1;
            RefreshPlan();
        };

        _browse.Click += (_, _) => BrowseImage();
        _recoveryDrive.Click += (_, _) => _ = OnRecoveryDriveAsync();

        _start.Click += (_, _) =>
        {
            if (Plan() is null) return;
            DialogResult = DialogResult.OK;
        };

        Resize += (_, _) => Arrange();
        Arrange();

        LoadDrives();
        SelectPreferred(preferredSource);
        RefreshPlan();
    }

    /// <summary>The plan the page runs, or null while the wizard is still open.</summary>
    public BackupPlan? PlanResult { get; private set; }

    /// <summary>The settings the page will run, or null when START cannot go ahead.</summary>
    public BackupPlan? Plan()
    {
        DriveInfo? source = At(_sources, _sourceIndex);
        DriveInfo? target = At(_targets, _targetIndex);

        if (source is null || target is null || string.IsNullOrWhiteSpace(_imagePath)) return null;

        if (Estimate(source) > TargetFree())
        {
            _verdict.Text = "NOT ENOUGH ROOM ON THAT DISK — PICK ANOTHER DESTINATION";
            _verdict.ForeColor = Theme.Bad;
            return null;
        }

        return PlanResult = new BackupPlan(
            source.Name,
            _imagePath,
            _compression,
            At(_sticks, _stickIndex)?.Name);
    }

    /// <summary>
    /// The estimate comes from the imaging service, which also decides the compression the capture
    /// will really use - so the wizard's "needs about" and the page's room check cannot disagree.
    /// </summary>
    private long Estimate(DriveInfo? source)
    {
        try
        {
            long used = source is null ? 1 : Math.Max(1, source.TotalSize - source.AvailableFreeSpace);
            return ImagingService.EstimateBytes(used, _compression, _imagePath);
        }
        catch
        {
            return 1;
        }
    }

    // ------------------------------------------------------------------ layout

    private void Arrange()
    {
        int wide = ClientSize.Width;
        int tall = ClientSize.Height;
        int inner = Math.Max(300, wide - 36);

        _sourcePick.SetBounds(18, 96, inner, 32);
        _sourceDetail.SetBounds(20, 132, inner - 4, 18);

        _targetPick.SetBounds(18, 168, inner - 122, 32);
        _browse.SetBounds(18 + inner - 116, 169, 116, 30);
        _targetDetail.SetBounds(20, 204, inner - 4, 18);

        _pathLabel.SetBounds(20, 226, inner - 4, 18);

        _compressionPick.SetBounds(18, 262, 170, 32);
        _stickPick.SetBounds(198, 262, Math.Max(160, inner - 390), 32);
        _recoveryDrive.SetBounds(18 + inner - 180, 262, 180, 32);

        _stickNote.SetBounds(20, 302, inner - 4, 52);
        _verdict.SetBounds(20, Math.Max(362, tall - 96), inner - 4, 18);

        _start.SetBounds(18, tall - 52, _start.Width, 34);
        _cancel.SetBounds(wide - 18 - _cancel.Width, tall - 52, _cancel.Width, 34);
        _help.SetBounds(_cancel.Left - 8 - _help.Width, tall - 52, _help.Width, 34);
    }

    // ------------------------------------------------------------------ drives

    private void LoadDrives()
    {
        _sources.Clear();
        _targets.Clear();
        _sticks.Clear();

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;

            // A USB hard disk reports Fixed, so both disk kinds are offered - a stick is only
            // singled out when it can carry the pointer.
            if (drive.DriveType is not (DriveType.Fixed or DriveType.Removable)) continue;

            _sources.Add(drive);
            _targets.Add(drive);

            if (drive.DriveType == DriveType.Removable && drive.TotalSize > 0) _sticks.Add(drive);
        }
    }

    /// <summary>A volume cannot be its own destination, so the source is never left as the target.</summary>
    private void SyncTargetExcludesSource()
    {
        DriveInfo? source = At(_sources, _sourceIndex);
        if (source is null) return;

        _targetIndex = Math.Clamp(_targetIndex, 0, Math.Max(0, _targets.Count - 1));

        bool onSource = At(_targets, _targetIndex) is { } chosen &&
                        chosen.Name.Equals(source.Name, StringComparison.OrdinalIgnoreCase);
        if (!onSource) return;

        int other = _targets.FindIndex(d => !d.Name.Equals(source.Name, StringComparison.OrdinalIgnoreCase));
        if (other >= 0) _targetIndex = other;
    }

    private void SelectPreferred(string? preferredSource)
    {
        if (!string.IsNullOrWhiteSpace(preferredSource))
        {
            int match = _sources.FindIndex(d =>
                d.Name.TrimEnd('\\').Equals(preferredSource.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));

            if (match >= 0) _sourceIndex = match;
        }

        SyncTargetExcludesSource();
    }

    private void RefreshPlan()
    {
        DriveInfo? source = At(_sources, _sourceIndex);
        DriveInfo? target = At(_targets, _targetIndex);

        _sourcePick.Text = "SOURCE: " + Describe(source);
        _targetPick.Text = "DESTINATION: " + Describe(target);
        _sourceDetail.Text = Detail(source);
        _targetDetail.Text = Detail(target);
        _compressionPick.Text = "COMPRESSION: " + _compression.ToUpperInvariant();

        DriveInfo? stick = At(_sticks, _stickIndex);
        _stickPick.Text = stick is null
            ? _sticks.Count == 0 ? "RECOVERY USB: NONE FOUND" : "RECOVERY USB: NONE"
            : "RECOVERY USB: " + Describe(stick);

        if (!_pathManual || string.IsNullOrWhiteSpace(_imagePath)) _imagePath = DefaultImagePath(target);
        _pathLabel.Text = _imagePath;
        _tips.SetToolTip(_pathLabel, _imagePath);

        long estimate = Estimate(source);
        long free = TargetFree();

        if (source is null || target is null)
        {
            _verdict.Text = "NO DISK AVAILABLE";
            _verdict.ForeColor = Theme.Warn;
        }
        else
        {
            _verdict.Text = $"IMAGE NEEDS ABOUT {FormatSize(estimate)} — {target.Name.TrimEnd('\\')} HAS {FormatSize(free)} FREE";
            _verdict.ForeColor = estimate > free ? Theme.Bad : Theme.Good;
        }

        // The pointer needs something to boot from: a Windows recovery drive offers the standard
        // repairs and the OS reset beside PC Medic's restore, and the wizard says which stick has one.
        if (stick is null)
        {
            _stickNote.Text =
                "A recovery stick can carry a pointer to the image instead of the image itself. Plug one in and " +
                "press the button to include it; RECOVERY DRIVE\u2026 makes it a Windows repair stick too.";
        }
        else if (RecoveryStick.HasWindowsRepair(stick.Name))
        {
            _stickNote.Text =
                $"{RecoveryStick.PresentVerdict} \u2014 this stick boots into Windows recovery, where " +
                $"{RecoveryStick.StandardRepairs} sit. PC Medic adds its image pointer beside them.";
        }
        else
        {
            _stickNote.Text =
                $"{RecoveryStick.MissingVerdict} \u2014 it can carry the pointer but offers none of those repairs. " +
                "Press RECOVERY DRIVE\u2026 to make it a Windows recovery drive first.";
        }

        _tips.SetToolTip(_stickNote, _stickNote.Text);

        _start.Enabled = source is not null && target is not null;
    }

    private long TargetFree()
    {
        try
        {
            DriveInfo? target = At(_targets, _targetIndex);
            return target?.AvailableFreeSpace ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private string DefaultImagePath(DriveInfo? target)
    {
        if (target is null) return string.Empty;

        string folder = Path.Combine(target.Name, "PCMedic-Backups");

        try { Directory.CreateDirectory(folder); }
        catch { folder = target.Name; }

        return Path.Combine(folder, $"PCMedic-Backup-{DateTime.Now:yyyyMMdd-HHmm}.wim");
    }

    /// <summary>
    /// Hands off to Windows' recovery drive tool, then re-reads the drives: the tool reformats the
    /// stick, so the pointer is written afterwards, against whatever the tool left there.
    /// </summary>
    private async Task OnRecoveryDriveAsync()
    {
        string? previousStick = At(_sticks, _stickIndex)?.Name;

        string? error = await RecoveryStick.OpenBuilderAsync();
        if (IsDisposed) return;

        if (error is not null)
        {
            MessageBox.Show(
                this,
                error + "\n\nUse Windows' own \"Create a recovery drive\" tool (Control Panel \u2192 Recovery), " +
                "then press RECOVERY USB to pick the stick again.",
                "Recovery drive",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        LoadDrives();
        _sourceIndex = Math.Clamp(_sourceIndex, 0, Math.Max(0, _sources.Count - 1));
        SelectStickAfterRecoveryTool(previousStick);
        RefreshPlan();
    }

    /// <summary>
    /// Re-selects when the tool closes: the stick it just wrote, so the verdict shows what that stick
    /// now offers and the pointer lands on it - or, when the tool left no Windows repair behind, the
    /// stick the user had already picked.
    /// </summary>
    private void SelectStickAfterRecoveryTool(string? previousRoot)
    {
        _stickIndex = _sticks.FindIndex(drive => RecoveryStick.HasWindowsRepair(drive.Name));

        if (_stickIndex >= 0 || previousRoot is null) return;

        _stickIndex = _sticks.FindIndex(drive =>
            drive.Name.TrimEnd('\\').Equals(previousRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
    }

    private void BrowseImage()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Where should the image go?",
            Filter = "Windows Image|*.wim;*.esd",
            FileName = Path.GetFileName(_imagePath),
            InitialDirectory = At(_targets, _targetIndex)?.Name ?? string.Empty,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _imagePath = dialog.FileName;
        _pathManual = true;
        RefreshPlan();
    }

    // ------------------------------------------------------------------ helpers

    private static T? At<T>(List<T> list, int index) where T : class
        => list.Count == 0 || index < 0 || index >= list.Count ? null : list[index];

    private static string Describe(DriveInfo? drive)
    {
        if (drive is null) return "—";

        string letter = drive.Name.TrimEnd('\\');
        try
        {
            string label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "DISK" : Theme.Truncate(drive.VolumeLabel, 14);
            return $"{letter}  {label}";
        }
        catch
        {
            return letter;
        }
    }

    private static string Detail(DriveInfo? drive)
    {
        if (drive is null) return string.Empty;

        try
        {
            string kind = drive.DriveType == DriveType.Removable ? "USB / REMOVABLE" : "FIXED";
            return $"{kind}  ·  {FormatSize(drive.AvailableFreeSpace)} free of {FormatSize(drive.TotalSize)}";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatSize(long bytes) => Theme.Bytes((ulong)Math.Max(0, bytes));

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // The client size a form actually gets can differ from the one in the constructor (scaling,
        // a small desktop), and the footer is positioned from it.
        Arrange();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tips.Dispose();
        base.Dispose(disposing);
    }
}
