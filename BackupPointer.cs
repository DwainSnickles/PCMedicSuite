using System.Text;
using System.Text.Json;

namespace PC_Medic;

/// <summary>
/// The card a bootable 16 GB USB needs: it does not hold the image, it holds the address of one.
///
/// A recovery stick is usually far too small for a full Windows image, so PC Medic writes a small
/// folder onto it instead - a JSON pointer to the image on the real backup disk, and a plain-text
/// note saying how to get back to it. Restoring then means booting the stick, opening PC Medic and
/// browsing to the path in the pointer, with the image itself read from wherever it was written.
/// </summary>
internal static class BackupPointer
{
    public const string FolderName = "PCMedicRecovery";
    public const string PointerFile = "backup-location.json";
    public const string ReadmeFile = "RESTORE-README.txt";

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>What a recovery stick records, so a later restore needs no guessing.</summary>
    internal sealed class Pointer
    {
        public string Product { get; set; } = "PC Medic";
        public string ImagePath { get; set; } = string.Empty;
        public long ImageBytes { get; set; }

        /// <summary>
        /// Which image in the archive to apply. Capture always writes a fresh archive holding one
        /// image, so this is 1 - but it is recorded rather than assumed, because a restore from
        /// Windows recovery has no wimlib output to read and must not pick an index by guesswork.
        /// </summary>
        public int ImageIndex { get; set; } = 1;

        /// <summary>
        /// Label of the volume holding the image. Drive letters move between Windows and Windows RE,
        /// so the recovery menu matches the label and the file name instead of the letter above.
        /// </summary>
        public string ImageVolumeLabel { get; set; } = string.Empty;

        /// <summary>
        /// "UEFI" or "BIOS": the boot mode the image was taken under. Boot files are written for one
        /// or the other, and Windows RE carries no reliable way to ask.
        /// </summary>
        public string Firmware { get; set; } = string.Empty;
        public string SourceRoot { get; set; } = string.Empty;
        public string SourceLabel { get; set; } = string.Empty;
        public string Compression { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public string Machine { get; set; } = Environment.MachineName;
    }

    /// <summary>
    /// Stamps the stick. Returns the folder written so the caller can say exactly where it went.
    /// </summary>
    public static (bool Ok, string Message) Write(
        string usbRoot,
        string imagePath,
        string sourceRoot,
        string compression)
    {
        if (string.IsNullOrWhiteSpace(usbRoot) || !Directory.Exists(usbRoot.TrimEnd('\\') + "\\"))
            return (false, $"'{usbRoot}' is not a readable drive root.");

        try
        {
            string folder = Path.Combine(usbRoot, FolderName);
            Directory.CreateDirectory(folder);

            var pointer = new Pointer
            {
                ImagePath = imagePath,
                ImageBytes = File.Exists(imagePath) ? new FileInfo(imagePath).Length : 0,
                ImageIndex = 1,      // capture writes a new archive: one image, index 1
                ImageVolumeLabel = VolumeLabel(Path.GetPathRoot(imagePath) ?? imagePath),
                Firmware = BootRepairService.MachineFirmware(),
                SourceRoot = sourceRoot,
                SourceLabel = VolumeLabel(sourceRoot),
                Compression = compression,
            };

            File.WriteAllText(
                Path.Combine(folder, PointerFile),
                JsonSerializer.Serialize(pointer, Options),
                new UTF8Encoding(false));

            File.WriteAllText(
                Path.Combine(folder, ReadmeFile),
                BuildReadme(pointer),
                new UTF8Encoding(false));

            // The menu and the engine it drives, so the stick can restore with no Windows running.
            string? payload = RecoveryPayload.Install(folder);
            if (payload is not null)
                ActivityLog.Warn($"Recovery menu not copied to {folder}: {payload}");

            ActivityLog.Success($"Recovery pointer written to {folder}.");
            Log.Info($"Recovery pointer: {Path.Combine(folder, PointerFile)} -> {imagePath}");

            return (true, folder);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Recovery pointer could not be written: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>Reads a pointer back, for the restore side to offer without browsing.</summary>
    public static Pointer? Read(string usbRoot)
    {
        try
        {
            string path = Path.Combine(usbRoot, FolderName, PointerFile);
            if (!File.Exists(path)) return null;

            Pointer? pointer = JsonSerializer.Deserialize<Pointer>(File.ReadAllText(path), Options);

            // Sticks stamped before the index was recorded leave it at 0, which is not a valid
            // image: those archives hold one image, so index 1 is the only thing it can mean.
            if (pointer is not null && pointer.ImageIndex < 1) pointer.ImageIndex = 1;

            return pointer;
        }
        catch (Exception ex)
        {
            Log.Warn("Recovery pointer unreadable: " + ex.Message);
            return null;
        }
    }

    private static string BuildReadme(Pointer pointer) =>
        string.Join(Environment.NewLine, new[]
        {
            "PC MEDIC - RECOVERY POINTER",
            "===========================",
            "",
            "This stick holds the recovery environment and a note about where the backup is -",
            "not the backup itself. A full Windows image rarely fits on a small USB stick, so the",
            "image was written to a larger disk and this file records where that is.",
            "",
            $"Image        : {pointer.ImagePath}",
            $"Image size   : {(pointer.ImageBytes > 0 ? Theme.Bytes((ulong)pointer.ImageBytes) : "not yet written")}",
            $"Image index  : {pointer.ImageIndex} (inside the archive)",
            $"Image volume : {(string.IsNullOrWhiteSpace(pointer.ImageVolumeLabel) ? "unlabelled" : pointer.ImageVolumeLabel)}",
            $"Boot mode    : {(string.IsNullOrWhiteSpace(pointer.Firmware) ? "not recorded" : pointer.Firmware)}",
            $"Source volume: {pointer.SourceRoot} {pointer.SourceLabel}",
            $"Compression  : {pointer.Compression}",
            $"Written      : {pointer.CreatedUtc.ToLocalTime():yyyy-MM-dd HH:mm} on {pointer.Machine}",
            "",
            "If this stick is a Windows recovery drive, it also offers the standard repairs:",
            RecoveryStick.StandardRepairs + ".",
            "Use those for a damaged Windows, and the steps below to put the image back.",
            "",
            "IF WINDOWS CANNOT BOOT",
            "1. Boot this stick, choose Troubleshoot - Advanced options - Command Prompt.",
            "2. Run PCMedicRecovery\\restore.cmd from this stick. Its drive letter changes inside",
            "   recovery, so find it first:",
            "   for %d in (c d e f g h i j) do @if exist %d:\\PCMedicRecovery\\restore.cmd call %d:\\PCMedicRecovery\\restore.cmd",
            "3. The menu locates the image by the path and volume label above, checks the archive,",
            "   lists the disks, and asks before it writes anything.",
            "",
            "HOW TO RESTORE WITH WINDOWS RUNNING",
            "1. Boot this stick (or any Windows media with PC Medic on it).",
            "2. Attach the backup disk the image is on, then open PC Medic and go to Backup & Restore.",
            "3. IMAGE RESTORE reads this pointer by itself and fills in the image path above.",
            "   If it does not, press STICK in that card or BROWSE to the path by hand.",
            "4. Pick the target disk, then press RESTORE IMAGE.",
            "",
            "A restore overwrites everything on the target disk. Boot files are repaired afterwards.",
        });

    private static string VolumeLabel(string root)
    {
        try
        {
            var drive = new DriveInfo(root);
            return drive.IsReady ? drive.VolumeLabel : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
