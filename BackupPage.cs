namespace PC_Medic;

/// <summary>
/// Backup &amp; Restore page: wimlib disk-image capture/apply, single-target boot repair
/// (bcdboot port) and the live component-store health check. All heavy work runs through
/// Task.Run-style async service calls with the NeverKill process policy; imaging aborts
/// go through taskkill inside the services.
/// </summary>
internal sealed partial class BackupPage : PageBase
{
    // Shared state
    private readonly List<DriveInfo> _backupDrives = new();
    private readonly List<DriveInfo> _restoreDrives = new();
    private readonly List<string> _bootDrives = new();
    private List<ImagingService.WimInfo> _images = new();
    private int _srcIndex;
    private int _dstIndex;
    private int _bootIndex;
    private int _imageIndex;
    private string _destPath = string.Empty;
    private string _wimPath = string.Empty;

    /// <summary>A pointer and the stick it was read from, so the stick itself can be re-checked.</summary>
    private sealed record FoundStick(string Root, BackupPointer.Pointer Pointer);

    /// <summary>Pointers found on attached drives, and whether the current image came from one.</summary>
    private readonly List<FoundStick> _pointers = new();
    private int _pointerIndex;
    private bool _pointerAdopted;
    private string? _missingPointer;   // the last unattached pointer already logged

    private bool _destAuto = true;
    private string _compression = "fast";
    // Default from the machine, not from an assumption: a legacy BIOS PC must offer BIOS first.
    private bool _firmwareUefi = BootRepairService.MachineFirmware() != "BIOS";
    private bool _busy;
    private bool _backupRunning;
    private bool _restoreRunning;
    private CancellationTokenSource? _opCts;

    public BackupPage()
        : base("BACKUP & RESTORE", "Wimlib disk images, boot repair and component-store health checks — one console.")
    {
        InitializeComponent();

        StyleLabels();
        WireEvents();

        // The default lines come first: PopulateDrives adopts a recovery pointer, and that message
        // must not be overwritten by the idle text afterwards.
        SetPageStatus(ImagingService.WimlibStatus);
        _backupStatus.Text = "Pick a source drive and destination, then start the capture.";
        _restoreStatus.Text = "Pick a .wim/.esd image, choose a target drive, then restore.";
        _bootStatus.Text = "Pick an offline Windows install and repair its boot files.";
        _healthStatus.Text = "Checks the live component store with DISM, repairs if needed.";

        PopulateDrives();
        UpdatePickerTexts();

        _backupCard.Resize += (_, _) => LayoutBackup();
        _imageHost.Resize += (_, _) => LayoutImageList();
        _restoreCard.Resize += (_, _) => LayoutRestore();
        _bootCard.Resize += (_, _) => LayoutBoot();
        _healthCard.Resize += (_, _) => LayoutHealth();
        _footerPanel.Resize += (_, _) => LayoutFooter();

        LayoutBackup();
        LayoutRestore();
        LayoutBoot();
        LayoutHealth();
    }

    public override void OnNavigated() => RefreshData();

    public override void RefreshData()
    {
        PopulateDrives();
        _ = LoadBootInstallsAsync();
        SetPageStatus(ImagingService.WimlibStatus);
    }

    // ------------------------------------------------------------------
    // Construction helpers
    // ------------------------------------------------------------------

    private void StyleLabels()
    {
        foreach (Label label in new[]
                 {
                     _destLabel, _backupStatus, _wimLabel, _imageName, _restoreStatus, _bootStatus, _healthStatus,
                     _healthSummary,
                 })
        {
            label.Font = Theme.MonoSmall;
            label.ForeColor = Theme.TextDim;
            label.BackColor = Color.Transparent;
        }

        _destLabel.AutoEllipsis = true;
        _wimLabel.AutoEllipsis = true;
        _imageName.AutoEllipsis = true;
        _healthStatus.TextAlign = ContentAlignment.MiddleLeft;
        _healthSummary.AutoSize = false;
        _healthSummary.AutoEllipsis = true;
    }

    private void WireEvents()
    {
        _refresh.Click += (_, _) => RefreshData();

        _srcPick.Click += (_, _) => { Cycle(ref _srcIndex, _backupDrives.Count); UpdatePickerTexts(); };
        _dstPick.Click += (_, _) => { Cycle(ref _dstIndex, _restoreDrives.Count); UpdatePickerTexts(); };
        _bootPick.Click += (_, _) => { Cycle(ref _bootIndex, _bootDrives.Count); UpdatePickerTexts(); };
        _firmwarePick.Click += (_, _) => { _firmwareUefi = !_firmwareUefi; UpdatePickerTexts(); };
        _compPick.Click += (_, _) =>
        {
            _compression = _compression switch { "fast" => "maximum", "maximum" => "none", _ => "fast" };
            UpdatePickerTexts();
        };
        _imagePick.Click += (_, _) =>
        {
            if (_images.Count == 0) return;
            _imageIndex = (_imageIndex + 1) % _images.Count;
            UpdateImageUi();
        };

        _browseDest.Click += (_, _) => BrowseDestination();
        _browseWim.Click += (_, _) => BrowseImage();
        _pointerPick.Click += (_, _) => OnPointerStick();
        _startBackup.Click += (_, _) => _ = OnStartBackupAsync();
        _backupToDisk.Click += (_, _) => OnBackUpToDisk();
        _bootableDisk.Click += (_, _) =>
        {
            using var dialog = new RecoveryStickForm();
            dialog.ShowDialog(FindForm());
        };
        _restoreBtn.Click += (_, _) => _ = OnRestoreAsync();
        _checkStick.Click += (_, _) => _ = OnCheckStickAsync();
        _tips.SetToolTip(
            _checkStick,
            "Read this stick's pointer, find the image it names and run wimlib's integrity check on the archive." + Environment.NewLine +
            "A stick that fails here would fail when it is actually needed.");
        _bootRepair.Click += (_, _) => _ = OnBootRepairAsync();
        _healthRun.Click += (_, _) => _ = OnHealthCheckAsync();
    }

    private static void Cycle(ref int index, int count)
    {
        if (count <= 0) return;
        index = (index + 1) % count;
    }

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    private void LayoutBackup()
    {
        int width = Math.Max(240, _backupCard.ClientSize.Width - 36);
        const int x = 18;
        int bottom = Math.Max(120, _backupCard.ClientSize.Height - 18);
        int y = 58;

        _srcPick.SetBounds(x, y, width, 32); y += 40;
        _destLabel.SetBounds(x, y + 5, Math.Max(60, width - 130), 22);
        _browseDest.SetBounds(x + width - 120, y, 120, 30); y += 40;
        _compPick.SetBounds(x, y, 190, 30);
        _startBackup.SetBounds(x + width - 170, y, 170, 32); y += 44;
        _backupToDisk.SetBounds(x, y, Math.Min(240, width), 32);
        _bootableDisk.SetBounds(Math.Min(x + 250, x + width - 140), y, 140, 30); y += 42;

        int statusY = Math.Max(y, bottom - 30);
        _backupStatus.SetBounds(x, statusY, width, 20);
        _backupBar.SetBounds(x, statusY + 24, width, 6);
        _backupOverlay.SetBounds(0, 50, Math.Max(1, _backupCard.Width - 1), Math.Max(40, _backupCard.Height - 51));
    }

    private void LayoutRestore()
    {
        int width = Math.Max(240, _restoreCard.ClientSize.Width - 36);
        const int x = 18;
        int bottom = Math.Max(200, _restoreCard.ClientSize.Height - 18);
        int y = 58;

        _wimLabel.SetBounds(x, y + 5, Math.Max(60, width - 250), 22);
        _pointerPick.SetBounds(x + width - 244, y, 116, 30);
        _browseWim.SetBounds(x + width - 120, y, 120, 30); y += 38;
        _imagePick.SetBounds(x, y, 150, 30);
        _imageName.SetBounds(x + 160, y + 5, Math.Max(40, width - 160), 22); y += 38;

        // Target, action and status stack up from the card floor, so the image list gets whatever is left.
        int statusY = bottom - 30;
        int buttonY = statusY - 40;
        int targetY = buttonY - 40;
        int listHeight = targetY - y - 10;

        _imageHost.Visible = _images.Count > 0 && listHeight >= 28;
        if (_imageHost.Visible) _imageHost.SetBounds(x, y, width, listHeight);

        _dstPick.SetBounds(x, targetY, width, 32);
        _restoreBtn.SetBounds(x, buttonY, 170, 32);

        // The stick check sits beside the restore button, and is dropped rather than squeezed when
        // the card is too narrow to hold both.
        _checkStick.Visible = width >= 360;
        if (_checkStick.Visible) _checkStick.SetBounds(x + 182, buttonY, 150, 32);
        _restoreStatus.SetBounds(x, statusY, width, 20);
        _restoreBar.SetBounds(x, statusY + 24, width, 6);
        _restoreOverlay.SetBounds(0, 50, Math.Max(1, _restoreCard.Width - 1), Math.Max(40, _restoreCard.Height - 51));

        LayoutImageList();
    }

    private void LayoutFooter()
    {
        _refresh.SetBounds(Math.Max(0, _footerPanel.ClientSize.Width - _refresh.Width), 3, _refresh.Width, 30);
    }

    /// <summary>The image rows scroll inside their own plate, so a .wim with many images never overflows the card.</summary>
    private void LayoutImageList()
    {
        if (_imageHost.IsDisposed) return;

        int scrollbar = _imageHost.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        int width = Math.Max(120, _imageHost.ClientSize.Width - scrollbar);
        _imageList.SetBounds(0, 0, width, _imageList.Height);
    }

    private void LayoutBoot()
    {
        int width = Math.Max(280, _bootCard.ClientSize.Width - 36);
        const int x = 18;
        int bottom = Math.Max(140, _bootCard.ClientSize.Height - 18);
        int y = 58;

        _bootPick.SetBounds(x, y, Math.Max(120, width - 290), 32);
        _firmwarePick.SetBounds(x + width - 280, y, 140, 32);
        _bootRepair.SetBounds(x + width - 130, y, 130, 32); y += 42;
        _bootStatus.SetBounds(x, y, width, Math.Max(20, bottom - y - 26));
        _bootNote.SetBounds(x, bottom - 22, width, 20);
    }

    private void LayoutHealth()
    {
        int width = Math.Max(280, _healthCard.ClientSize.Width - 36);
        const int x = 18;
        int bottom = Math.Max(140, _healthCard.ClientSize.Height - 18);

        _healthRun.SetBounds(x, 56, 210, 32);
        _healthStatus.SetBounds(x + 220, 56, Math.Max(60, width - 220), 32);
        _healthSummary.SetBounds(x, 100, width, Math.Max(20, bottom - 122));
        _healthNote.SetBounds(x, bottom - 22, width, 20);
    }

    // ------------------------------------------------------------------
    // Data
    // ------------------------------------------------------------------

    private void PopulateDrives()
    {
        _backupDrives.Clear();
        _restoreDrives.Clear();

        string sys = (Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\")
            .TrimEnd('\\');

        foreach (DriveInfo drive in UsableDisks())
        {
            _backupDrives.Add(drive);
            if (!drive.Name.TrimEnd('\\').Equals(sys, StringComparison.OrdinalIgnoreCase))
                _restoreDrives.Add(drive);
        }

        if (_restoreDrives.Count == 0) _restoreDrives.AddRange(_backupDrives);
        _srcIndex = Math.Clamp(_srcIndex, 0, Math.Max(0, _backupDrives.Count - 1));
        _dstIndex = Math.Clamp(_dstIndex, 0, Math.Max(0, _restoreDrives.Count - 1));
        if (_destAuto) _destPath = DefaultDestination();
        UpdatePickerTexts();

        // A stick plugged in for a restore is the reason the page was opened: look for one every
        // refresh, not only when the restore card is used.
        ScanPointers();
        OfferPointer();
    }

    /// <summary>
    /// The disks this page works with. Removable ones are first-class: a full image rarely fits on the
    /// recovery stick, so the destination is usually an external drive plugged in for the job.
    /// </summary>
    private static IEnumerable<DriveInfo> UsableDisks() => DriveInfo.GetDrives().Where(drive =>
        drive.IsReady && drive.DriveType is DriveType.Fixed or DriveType.Removable);

    // ------------------------------------------------------------------
    // Recovery stick pointers
    // ------------------------------------------------------------------

    /// <summary>
    /// Finds every attached disk that carries PCMedicRecovery\backup-location.json. The pointer is
    /// written by BACK UP TO DISK and names an image on a larger disk, so the card can offer that
    /// image instead of asking a human to retype a path from the stick's README.
    /// </summary>
    private void ScanPointers()
    {
        _pointers.Clear();

        foreach (DriveInfo drive in UsableDisks())
        {
            BackupPointer.Pointer? pointer = BackupPointer.Read(drive.Name);
            if (pointer is not null && !string.IsNullOrWhiteSpace(pointer.ImagePath))
                _pointers.Add(new FoundStick(drive.Name, pointer));
        }

        _pointerIndex = Math.Clamp(_pointerIndex, 0, Math.Max(0, _pointers.Count - 1));
        UpdatePointerButton();
    }

    private void UpdatePointerButton()
    {
        _checkStick.Enabled = _pointers.Count > 0 && !_busy && !_restoreRunning;
        _pointerPick.Text = _pointers.Count == 0 ? "STICK\u2026" : $"POINTER {_pointerIndex + 1}/{_pointers.Count}";
        _tips.SetToolTip(
            _pointerPick,
            _pointers.Count == 0
                ? "No PCMedicRecovery pointer on any attached drive. Plug in a stick written by BACK UP TO DISK."
                : "Read the recovery stick's pointer and fill in the image it names. Press again to cycle when more than one is attached.");
    }

    /// <summary>Prefills the image from a pointer, unless a different image was already chosen.</summary>
    private void OfferPointer()
    {
        // A BROWSE choice wins; the stick stays one press away on its own button.
        if (_pointers.Count == 0 || (!string.IsNullOrEmpty(_wimPath) && !_pointerAdopted)) return;

        AdoptPointer(_pointers[_pointerIndex].Pointer);
    }

    private void OnPointerStick()
    {
        if (_busy || _restoreRunning) return;

        ScanPointers();
        if (_pointers.Count == 0)
        {
            SetStatus(_restoreStatus, "NO RECOVERY POINTER FOUND — PLUG THE STICK IN, THEN PRESS AGAIN", Theme.TextDim);
            return;
        }

        if (_pointers.Count > 1) _pointerIndex = (_pointerIndex + 1) % _pointers.Count;
        UpdatePointerButton();

        _pointerAdopted = false;   // the same pointer is re-read on demand
        AdoptPointer(_pointers[_pointerIndex].Pointer);
    }

    /// <summary>
    /// Takes the image a pointer names. When that disk is not attached the path is still shown, so
    /// the human knows what to plug in; nothing is marked ready to restore until the file exists.
    /// </summary>
    private void AdoptPointer(BackupPointer.Pointer pointer)
    {
        string image = pointer.ImagePath;
        string root = pointer.SourceRoot.TrimEnd('\\');
        string source = string.IsNullOrWhiteSpace(pointer.SourceLabel) ? root : $"{root} {pointer.SourceLabel}";
        string when = pointer.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        if (!File.Exists(image))
        {
            // The label stays on whatever the card would actually restore. It only takes the pointer's
            // path when nothing else is loaded, so the two can never disagree.
            if (string.IsNullOrEmpty(_wimPath))
            {
                _wimLabel.Text = image;
                _tips.SetToolTip(_wimLabel, $"{image}\nNamed by a recovery stick; that disk is not attached.");
            }

            SetStatus(_restoreStatus, $"RECOVERY POINTER — DISK NOT ATTACHED: {image}", Theme.Warn);

            // A refresh runs on every navigation: log the problem when it appears, not each time.
            if (_missingPointer != image)
            {
                _missingPointer = image;
                ActivityLog.Warn($"Recovery pointer names {image}, which is not attached.");
            }

            return;
        }

        _missingPointer = null;

        _wimLabel.Text = image;
        _tips.SetToolTip(_wimLabel, $"{image}\nFrom a recovery stick: backup of {source}, written {when}.");

        if (_pointerAdopted && string.Equals(_wimPath, image, StringComparison.OrdinalIgnoreCase))
            return;   // already adopted: a refresh must not re-read the image

        _wimPath = image;
        _pointerAdopted = true;
        _images = new List<ImagingService.WimInfo>();
        _imageIndex = 0;
        _imageList.Clear();
        UpdateImageUi();
        LayoutRestore();
        SetStatus(_restoreStatus, $"RECOVERY POINTER — image of {source} written {when}", Theme.Good);
        ActivityLog.Success($"Recovery pointer adopted: {image} (backup of {source}, {when}).");

        _ = LoadImagesAsync(image);
    }

    private async Task LoadBootInstallsAsync()
    {
        try
        {
            List<string> installs = await BootRepairService.FindWindowsInstallsAsync();
            if (IsDisposed) return;

            _bootDrives.Clear();
            _bootDrives.AddRange(installs);
            _bootIndex = Math.Clamp(_bootIndex, 0, Math.Max(0, _bootDrives.Count - 1));
            UpdatePickerTexts();

            if (_bootDrives.Count == 0)
                ActivityLog.Info("Boot repair: no Windows installs found on other fixed drives.");
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Boot target scan failed: {ex.Message}");
        }
    }

    private string DefaultDestination()
    {
        DriveInfo? source = Current(_backupDrives, _srcIndex);
        DriveInfo? other = _backupDrives.FirstOrDefault(d =>
            source is null || !d.Name.Equals(source.Name, StringComparison.OrdinalIgnoreCase));
        string root = (other ?? source)?.Name ?? "C:\\";
        return Path.Combine(root, $"PCMedic-Backup-{DateTime.Now:yyyyMMdd-HHmm}.wim");
    }

    private void SyncDestination()
    {
        if (_destAuto) _destPath = DefaultDestination();
        _destLabel.Text = string.IsNullOrEmpty(_destPath) ? "NO DESTINATION" : _destPath;
        _tips.SetToolTip(_destLabel, _destPath);
    }

    private void UpdatePickerTexts()
    {
        _srcPick.Text = "SOURCE: " + DescribeDrive(Current(_backupDrives, _srcIndex));
        _dstPick.Text = "TARGET: " + DescribeDrive(Current(_restoreDrives, _dstIndex));
        _bootPick.Text = _bootDrives.Count == 0
            ? "NO WINDOWS INSTALLS FOUND"
            : "TARGET: " + _bootDrives[_bootIndex];
        _bootPick.Enabled = _bootDrives.Count > 0 && !_busy;
        _firmwarePick.Text = "FIRMWARE: " + (_firmwareUefi ? "UEFI" : "BIOS");
        _compPick.Text = "COMPRESSION: " + _compression.ToUpperInvariant();
        SyncDestination();
    }

    private static T? Current<T>(List<T> list, int index) where T : class
        => list.Count == 0 ? null : list[Math.Clamp(index, 0, list.Count - 1)];

    private static string DescribeDrive(DriveInfo? drive)
    {
        if (drive is null) return "—";
        string letter = drive.Name.TrimEnd('\\');
        try
        {
            string label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "LOCAL DISK" : drive.VolumeLabel;
            string kind = drive.DriveType == DriveType.Removable ? " USB" : string.Empty;
            return $"{letter}  {Theme.Truncate(label, 16)}  {Theme.Bytes((ulong)drive.AvailableFreeSpace)} free / {Theme.Bytes((ulong)drive.TotalSize)}{kind}";
        }
        catch
        {
            return letter;
        }
    }

    private void UpdateImageUi()
    {
        if (_images.Count == 0)
        {
            _imagePick.Text = "IMAGE 1/1";
            _imageName.Text = string.Empty;
            return;
        }

        var image = _images[Math.Clamp(_imageIndex, 0, _images.Count - 1)];
        _imagePick.Text = $"IMAGE {_imageIndex + 1}/{_images.Count}";
        _imagePick.Enabled = true;
        _imageName.Text = Theme.Truncate(
            $"#{image.Index} {image.Name}" +
            (image.Description.Length > 0 ? $" — {image.Description}" : string.Empty) +
            (image.SizeText.Length > 0 ? $"  ·  {image.SizeText}" : string.Empty), 90);
        _tips.SetToolTip(_imageName, _imageName.Text);
    }

    // ------------------------------------------------------------------
    // Dialogs
    // ------------------------------------------------------------------

    private void BrowseDestination()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save disk image",
            Filter = "Windows Image|*.wim;*.esd",
            FileName = Path.GetFileName(_destPath),
            InitialDirectory = Current(_backupDrives, _srcIndex)?.Name ?? string.Empty,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _destPath = dialog.FileName;
            _destAuto = false;
            _destLabel.Text = _destPath;
            _tips.SetToolTip(_destLabel, _destPath);
        }
    }

    private void BrowseImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select install image",
            Filter = "Windows Image|*.wim;*.esd;*.swm",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _wimPath = dialog.FileName;
        _pointerAdopted = false;   // a browsed image is the user's choice, not the stick's
        _wimLabel.Text = _wimPath;
        _tips.SetToolTip(_wimLabel, _wimPath);
        _images = new List<ImagingService.WimInfo>();
        _imageIndex = 0;
        _imageList.Clear();
        UpdateImageUi();
        LayoutRestore();
        _ = LoadImagesAsync(_wimPath);
    }

    private async Task LoadImagesAsync(string path)
    {
        _imageName.Text = "READING IMAGE…";
        try
        {
            List<ImagingService.WimInfo> images = await ImagingService.ReadImagesAsync(path);
            if (IsDisposed) return;

            _images = images;
            _imageIndex = 0;
            _imageList.Clear();
            foreach (ImagingService.WimInfo image in images.Take(8))
            {
                _imageList.Add(
                    $"#{image.Index}",
                    Theme.Truncate(image.Name + (image.SizeText.Length > 0 ? $" · {image.SizeText}" : string.Empty), 40));
            }
            if (images.Count > 8) _imageList.Add("…", $"{images.Count - 8} more image(s)");

            UpdateImageUi();
            LayoutRestore();
            ActivityLog.Info($"Image '{Path.GetFileName(path)}': {images.Count} image(s) found.");
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Could not read image info: {ex.Message}");
            _imageName.Text = "IMAGE READ FAILED";
        }
    }

    // ------------------------------------------------------------------
    // Operations
    // ------------------------------------------------------------------

    private async Task OnStartBackupAsync()
    {
        if (_backupRunning)
        {
            _opCts?.Cancel();
            _backupStatus.Text = "ABORTING — killing wimlib…";
            return;
        }
        if (_busy) return;
        if (_backupDrives.Count == 0 || string.IsNullOrEmpty(_destPath)) return;

        await RunCaptureAsync(_backupDrives[_srcIndex].Name, _destPath, _compression, recoveryUsb: null);
    }

    /// <summary>
    /// The guided path for a full image: choose a disk that can hold it, then let a small USB stick
    /// carry a pointer instead of the image. The page adopts the choice so its pickers agree with
    /// what actually runs.
    /// </summary>
    private async void OnBackUpToDisk()
    {
        if (_busy || _backupRunning) return;

        using var wizard = new BackupTargetForm(Current(_backupDrives, _srcIndex)?.Name, _compression);
        if (wizard.ShowDialog(FindForm()) != DialogResult.OK || wizard.PlanResult is not { } plan) return;

        int source = _backupDrives.FindIndex(d =>
            d.Name.TrimEnd('\\').Equals(plan.SourceRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
        if (source >= 0) _srcIndex = source;

        _destPath = plan.ImagePath;
        _destAuto = false;
        _compression = plan.Compression;
        UpdatePickerTexts();

        await RunCaptureAsync(plan.SourceRoot, plan.ImagePath, plan.Compression, plan.RecoveryUsbRoot);
    }

    /// <summary>
    /// The one capture path: room check, progress, keep-awake, and - when a recovery stick was chosen -
    /// the pointer that tells a later boot where the image went.
    /// </summary>
    private async Task RunCaptureAsync(string source, string dest, string compression, string? recoveryUsb)
    {
        if (_busy) return;

        if (!ImagingService.IsAvailable)
        {
            ActivityLog.Error("wimlib-imagex.exe was not found in bin\\ or on PATH.");
            SetStatus(_backupStatus, "WIMLIB MISSING", Theme.Bad);
            return;
        }

        if (!HasRoomFor(source, dest, compression, out string room))
        {
            SetStatus(_backupStatus, room.ToUpperInvariant(), Theme.Bad);
            ActivityLog.Error($"Backup not started — {room}");
            return;
        }

        _busy = true;
        _backupRunning = true;
        using var cts = new CancellationTokenSource();
        _opCts = cts;
        using var busy = Working();
        SetBackupRunning(true);
        _backupOverlay.Begin();
        _backupBar.Visible = true;
        _backupBar.Percent = 0;
        SetStatus(_backupStatus, "STARTING CAPTURE…", Theme.TextDim);
        SetPageStatus("BACKUP RUNNING");

        var progress = new Progress<int>(pct =>
        {
            if (IsDisposed) return;
            _backupOverlay.Percent = pct;
            _backupBar.Percent = pct;
            SetStatus(_backupStatus, $"CAPTURING IMAGE — {pct}%", Theme.TextDim);
        });

        try
        {
            ImagingService.BackupResult result =
                await ImagingService.CaptureAsync(source, dest, compression, progress, cts.Token);
            if (IsDisposed) return;

            if (result.Success)
            {
                SetStatus(_backupStatus, "BACKUP COMPLETE — " + result.Message, Theme.Good);

                if (recoveryUsb is not null)
                {
                    (bool ok, string pointerMessage) =
                        BackupPointer.Write(recoveryUsb, dest, source, compression);

                    SetStatus(
                        _backupStatus,
                        ok
                            ? $"BACKUP COMPLETE — RECOVERY POINTER ON {recoveryUsb.TrimEnd('\\')}"
                            : "BACKUP COMPLETE — RECOVERY POINTER FAILED",
                        ok ? Theme.Good : Theme.Warn);

                    if (!ok) ActivityLog.Warn($"Recovery pointer: {pointerMessage}");
                }
            }
            else if (result.Aborted)
                SetStatus(_backupStatus, "BACKUP ABORTED", Theme.Warn);
            else
                SetStatus(_backupStatus, "BACKUP FAILED — see activity log", Theme.Bad);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Backup failed: {ex.Message}");
            SetStatus(_backupStatus, "BACKUP FAILED", Theme.Bad);
        }
        finally
        {
            _backupRunning = false;
            _busy = false;
            _opCts = null;
            _backupOverlay.End();
            _backupBar.Visible = false;
            SetBackupRunning(false);
            SetPageStatus(ImagingService.WimlibStatus);
        }
    }

    /// <summary>
    /// Proves the stick while Windows still runs: pointer readable, image present at its recorded
    /// size, archive intact. The only step it cannot take is booting the stick, which needs a restart.
    /// </summary>
    private async Task OnCheckStickAsync()
    {
        if (_busy || _restoreRunning || _pointers.Count == 0) return;

        FoundStick stick = _pointers[_pointerIndex];

        _busy = true;
        using var busy = Working();
        using var awake = KeepAwake.Enter(keepDisplayAwake: true);
        _restoreOverlay.Begin();
        _restoreBar.Visible = true;
        _restoreBar.Percent = 0;
        SetStatus(_restoreStatus, "CHECKING STICK\u2026", Theme.TextDim);
        SetPageStatus("CHECKING STICK");
        UpdatePointerButton();

        // Progress goes to the bar, not the status label: those callbacks are posted, and a late one
        // would land after the verdict and replace it with a percentage.
        var progress = new Progress<int>(pct =>
        {
            if (IsDisposed) return;
            _restoreOverlay.Percent = pct;
            _restoreBar.Percent = pct;
        });

        try
        {
            RecoveryCheck.Result result =
                await RecoveryCheck.RunAsync(stick.Root, progress, CancellationToken.None);
            if (IsDisposed) return;

            foreach (RecoveryCheck.Step step in result.Steps)
            {
                ActivityLog.Write(
                    step.Ok ? LogLevel.Info : LogLevel.Error,
                    $"Stick check {stick.Root.TrimEnd('\\')} \u2014 {step.Name}: {step.Detail}");
            }

            ActivityLog.Write(
                result.Ok ? LogLevel.Success : LogLevel.Warn,
                $"Recovery stick {stick.Root.TrimEnd('\\')}: {result.Summary}");

            SetStatus(_restoreStatus, result.Summary, result.Ok ? Theme.Good : Theme.Bad);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Stick check failed: {ex.Message}");
            SetStatus(_restoreStatus, "STICK CHECK FAILED \u2014 SEE ACTIVITY LOG", Theme.Bad);
        }
        finally
        {
            _busy = false;
            _restoreOverlay.End();
            _restoreBar.Visible = false;
            if (!IsDisposed) SetPageStatus(ImagingService.WimlibStatus);
            UpdatePointerButton();
        }
    }

    private async Task OnRestoreAsync()
    {
        if (_restoreRunning)
        {
            _opCts?.Cancel();
            _restoreStatus.Text = "ABORTING — killing wimlib…";
            return;
        }
        if (_busy) return;
        if (_images.Count == 0 || string.IsNullOrEmpty(_wimPath) || _restoreDrives.Count == 0) return;
        if (!ImagingService.IsAvailable)
        {
            ActivityLog.Error("wimlib-imagex.exe was not found in bin\\ or on PATH.");
            return;
        }

        string target = _restoreDrives[_dstIndex].Name;
        ImagingService.WimInfo image = _images[Math.Clamp(_imageIndex, 0, _images.Count - 1)];

        DialogResult confirm = MessageBox.Show(
            this,
            $"Restore image #{image.Index} \"{image.Name}\" onto {target.TrimEnd('\\')}?\n\n" +
            $"ALL DATA on {target.TrimEnd('\\')} WILL BE OVERWRITTEN.\n" +
            "Boot files will be repaired afterwards.",
            "Confirm restore",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        _busy = true;
        _restoreRunning = true;
        using var cts = new CancellationTokenSource();
        _opCts = cts;
        using var busy = Working();
        SetRestoreRunning(true);
        _restoreOverlay.Begin();
        _restoreBar.Visible = true;
        _restoreBar.Percent = 0;
        SetStatus(_restoreStatus, "STARTING APPLY…", Theme.TextDim);
        SetPageStatus("RESTORE RUNNING");

        var progress = new Progress<int>(pct =>
        {
            if (IsDisposed) return;
            _restoreOverlay.Percent = pct;
            _restoreBar.Percent = pct;
            SetStatus(_restoreStatus, $"APPLYING IMAGE — {pct}%", Theme.TextDim);
        });

        var bootLog = new Progress<string>(line =>
        {
            ActivityLog.Info($"[boot] {line}");
            if (!IsDisposed) _bootStatus.Text = Theme.Truncate(line, 90);
        });

        try
        {
            ImagingService.RestoreResult result =
                await ImagingService.ApplyAsync(
                    _wimPath, image.Index, target, _firmwareUefi, progress, bootLog, cts.Token);
            if (IsDisposed) return;

            if (result.Aborted)
            {
                SetStatus(_restoreStatus, "RESTORE ABORTED", Theme.Warn);
            }
            else if (result.ApplySucceeded)
            {
                bool bootOk = result.BootRepairSucceeded;
                ActivityLog.Write(
                    bootOk ? LogLevel.Success : LogLevel.Error,
                    $"Restore to {target}: {(bootOk ? "boot repair OK" : "boot repair FAILED")}");
                SetStatus(_restoreStatus,
                    bootOk ? "RESTORE + BOOT REPAIR COMPLETE" : "RESTORED — BOOT REPAIR FAILED",
                    bootOk ? Theme.Good : Theme.Bad);
            }
            else
            {
                SetStatus(_restoreStatus, "RESTORE FAILED — see activity log", Theme.Bad);
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Restore failed: {ex.Message}");
            SetStatus(_restoreStatus, "RESTORE FAILED", Theme.Bad);
        }
        finally
        {
            _restoreRunning = false;
            _busy = false;
            _opCts = null;
            _restoreOverlay.End();
            _restoreBar.Visible = false;
            SetRestoreRunning(false);
            SetPageStatus(ImagingService.WimlibStatus);
        }
    }

    private async Task OnBootRepairAsync()
    {
        if (_busy || _bootDrives.Count == 0) return;

        string target = _bootDrives[_bootIndex];
        bool uefi = _firmwareUefi;

        _busy = true;
        using var busy = Working();

        // A repair the machine falls asleep under leaves an install with half-written boot files.
        using var awake = KeepAwake.Enter(keepDisplayAwake: true);
        SetBootEnabled(false);
        SetStatus(_bootStatus, "RUNNING BCDBOOT…", Theme.TextDim);
        SetPageStatus("BOOT REPAIR RUNNING");

        var log = new Progress<string>(line =>
        {
            ActivityLog.Info($"[boot] {line}");
            if (IsDisposed) return;
            _bootStatus.Text = Theme.Truncate(line, 90);
        });

        try
        {
            BootRepairService.BootRepairResult result =
                await BootRepairService.RepairAsync(target, uefi, log, CancellationToken.None);
            if (IsDisposed) return;

            ActivityLog.Write(
                result.Success ? LogLevel.Success : LogLevel.Error,
                $"Boot repair {target}: {result.Message}");
            SetStatus(_bootStatus,
                result.Success ? "BOOT FILES WRITTEN" : "BOOT REPAIR FAILED",
                result.Success ? Theme.Good : Theme.Bad);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Boot repair failed: {ex.Message}");
            SetStatus(_bootStatus, "BOOT REPAIR FAILED", Theme.Bad);
        }
        finally
        {
            _busy = false;
            SetBootEnabled(true);
            SetPageStatus(ImagingService.WimlibStatus);
        }
    }

    private async Task OnHealthCheckAsync()
    {
        if (_busy) return;

        _busy = true;
        using var busy = Working();
        using var awake = KeepAwake.Enter(keepDisplayAwake: true);
        _healthRun.Enabled = false;
        SetStatus(_healthStatus, "CHECKING…", Theme.TextDim);
        _healthSummary.Text = string.Empty;
        SetPageStatus("HEALTH CHECK RUNNING");

        var progress = new Progress<string>(phase =>
        {
            if (IsDisposed) return;
            SetStatus(_healthStatus, phase, Theme.TextDim);
        });

        try
        {
            HealthReport report = await HealthCheckService.RunAsync(progress, CancellationToken.None);
            if (IsDisposed) return;

            Color tone = report.Status switch
            {
                HealthStatus.Healthy => Theme.Good,
                HealthStatus.Repairable => Theme.Warn,
                _ => Theme.Bad,
            };
            SetStatus(_healthStatus, report.Summary.ToUpperInvariant(), tone);
            _healthSummary.Text = Theme.Truncate(report.Summary, 110);
            LogHealthDetail(report.Detail);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Health check failed: {ex.Message}");
            SetStatus(_healthStatus, "HEALTH CHECK FAILED", Theme.Bad);
        }
        finally
        {
            _busy = false;
            _healthRun.Enabled = true;
            SetPageStatus(ImagingService.WimlibStatus);
        }
    }

    /// <summary>
    /// A backup that fills the disk halfway through leaves a useless partial image, so the room is
    /// checked up front: used bytes on the source times a compression factor, against the free space on
    /// the destination's volume. Unreadable sizes never block a backup the user asked for.
    /// </summary>
    private static bool HasRoomFor(string sourceRoot, string destPath, string compression, out string message)
    {
        message = string.Empty;

        try
        {
            var source = new DriveInfo(sourceRoot);
            long used = Math.Max(1, source.TotalSize - source.AvailableFreeSpace);
            long estimate = ImagingService.EstimateBytes(used, compression, destPath);

            string? root = Path.GetPathRoot(destPath);
            if (string.IsNullOrEmpty(root)) return true;

            var target = new DriveInfo(root);
            if (!target.IsReady) return true;

            long free = target.AvailableFreeSpace;
            if (estimate <= free) return true;

            message = $"image needs about {Theme.Bytes((ulong)estimate)} but {root.TrimEnd('\\')} has " +
                      $"{Theme.Bytes((ulong)free)} free — pick a bigger disk in BACK UP TO DISK";
            return false;
        }
        catch (Exception ex)
        {
            Log.Warn("Free-space check skipped: " + ex.Message);
            return true;
        }
    }

    /// <summary>Surfaces the interesting DISM/SFC lines without flooding the activity feed.</summary>
    private static void LogHealthDetail(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return;

        int logged = 0;
        foreach (string raw in detail.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            string lower = line.ToLowerInvariant();
            bool interesting =
                lower.Contains("error") || lower.Contains("repair") || lower.Contains("corrupt") ||
                lower.Contains("healthy") || lower.Contains("scan") || lower.Contains("restore") ||
                lower.Contains("sfc") || lower.Contains("completed");
            if (!interesting) continue;
            ActivityLog.Info(Theme.Truncate(line, 140));
            if (++logged >= 30) break;
        }
    }

    // ------------------------------------------------------------------
    // Small UI helpers
    // ------------------------------------------------------------------

    private static void SetStatus(Label label, string text, Color tone)
    {
        label.Text = text;
        label.ForeColor = tone;
    }

    /// <summary>Engine state and elevation in one line, the way the other pages report their context.</summary>
    private void SetPageStatus(string state)
    {
        _status.Text = $"{state}  \u2022  {(Native.IsElevated ? "ELEVATED" : "STANDARD USER")}";
        _status.ForeColor = Theme.TextDim;
    }

    private void SetBackupRunning(bool running)
    {
        _startBackup.Text = running ? "ABORT" : "START BACKUP";
        _srcPick.Enabled = !running;
        _compPick.Enabled = !running;
        _browseDest.Enabled = !running;
    }

    private void SetRestoreRunning(bool running)
    {
        _restoreBtn.Text = running ? "ABORT" : "RESTORE IMAGE";
        _checkStick.Enabled = !running && _pointers.Count > 0;
        _browseWim.Enabled = !running;
        _imagePick.Enabled = !running && _images.Count > 0;
        _dstPick.Enabled = !running;
    }

    private void SetBootEnabled(bool enabled)
    {
        _bootPick.Enabled = enabled && _bootDrives.Count > 0;
        _firmwarePick.Enabled = enabled;
        _bootRepair.Enabled = enabled;
    }
}
