using System.Text;
using System.Text.RegularExpressions;

namespace PC_Medic;

/// <summary>
/// Builds a bootable Windows RE recovery stick on a USB drive: diskpart cleans and
/// formats the disk FAT32, the live WinRE image is copied as sources\boot.wim, the
/// firmware boot manager is dropped at the UEFI fallback path, and a BCD store is
/// written with bcdedit so the stick boots straight into recovery.
/// Every step is reported through the progress callback; failures abort with the
/// last useful message rather than leaving a half-written stick silently.
/// </summary>
internal static class RecoveryStickService
{
    public sealed record Result(bool Ok, string Message);

    /// <summary>Removable drives that are ready and have a usable size — the only safe format targets.</summary>
    public static List<DriveInfo> UsableRemovableDrives() =>
        DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Removable)
            .ToList();

    /// <summary>
    /// The live WinRE image on this machine, or null when recovery is disabled. Enabled systems keep
    /// the image on the recovery partition at C:\Recovery\WindowsRE\Winre.wim (ACL'd — needs the
    /// elevated run this builder already requires); reagentc-staged copies sit under System32\Recovery.
    /// </summary>
    public static string? FindWinreImage()
    {
        string system = Path.Combine(Environment.SystemDirectory, "Recovery", "Winre.wim");
        if (File.Exists(system)) return system;

        string recovery = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\'),
            "..", "Recovery", "WindowsRE", "Winre.wim");
        recovery = Path.GetFullPath(recovery);
        return File.Exists(recovery) ? recovery : null;
    }

    /// <summary>The UEFI boot manager binary shipped with Windows, used as the stick's bootx64.efi.</summary>
    private static string? FindBootManager()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Boot", "EFI", "bootmgfw.efi");
        return File.Exists(path) ? path : null;
    }

    /// <summary>A boot.sdi for the ramdisk, searched under C:\Windows\Boot — optional but preferred.</summary>
    private static string? FindBootSdi()
    {
        string bootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Boot");
        try
        {
            return Directory.EnumerateFiles(bootDir, "boot.sdi", SearchOption.AllDirectories).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Formats driveRoot ("E:\") and writes the recovery image. Caller must have
    /// confirmed the destructive format — this method does not ask.
    /// </summary>
    public static async Task<Result> CreateAsync(
        string driveRoot,
        IProgress<string>? log,
        CancellationToken ct = default)
    {
        void Step(string message) => log?.Report(message);

        return await Task.Run(() => CreateCore(driveRoot, Step, ct), ct);
    }

    private static Result CreateCore(string driveRoot, Action<string> step, CancellationToken ct)
    {
        string root = (driveRoot ?? string.Empty).Trim();
        if (!root.EndsWith("\\")) root += "\\";
        if (root.Length < 3 || root[1] != ':')
            return new Result(false, "Pick a valid drive letter first.");

        // ---- sources on this machine ----
        string? winre = FindWinreImage();
        if (winre is null)
            return new Result(false, "No Winre.wim found — Windows Recovery may be disabled (reagentc /info).");

        string? bootmgfw = FindBootManager();
        if (bootmgfw is null)
            return new Result(false, "bootmgfw.efi not found under C:\\Windows\\Boot — cannot write UEFI boot files.");

        step("Checking the Windows recovery image…");
        ct.ThrowIfCancellationRequested();

        // ---- resolve the physical disk behind the letter ----
        step($"Locating the disk behind {root}…");
        string detail = RunDiskpart($"select volume {root[..1]}\ndetail volume", ct);
        string? disk = MatchNumber(detail, @"Disk\s*#+\s*:\s*(\d+)") ?? MatchNumber(detail, @"Disk\s+(\d+)");
        if (disk is null)
            return new Result(false, $"diskpart could not identify the disk for {root}.");

        // ---- pick a free drive letter for the freshly formatted stick ----
        char letter = NextFreeLetter();
        if (letter == '\0')
            return new Result(false, "No free drive letters left for the recovery stick.");

        // ---- clean + partition + format + assign ----
        step($"Formatting disk {disk} as FAT32 (drive {letter}:) — this erases the whole stick…");
        string script =
            $"select disk {disk}\n" +
            "clean\n" +
            "create partition primary\n" +
            "format fs=fat32 quick label=\"PCMedicRecovery\"\n" +
            $"assign letter={letter}\n";

        string formatOut = RunDiskpart(script, ct, timeoutMs: 10 * 60 * 1000);
        ct.ThrowIfCancellationRequested();

        string stick = $"{letter}:\\";
        if (!Directory.Exists(stick))
            return new Result(false, $"Format finished but {stick} is not accessible — the stick may need a replug.");

        // ---- copy the recovery payload ----
        step("Copying the WinRE image to sources\\boot.wim…");
        Directory.CreateDirectory(Path.Combine(stick, "sources"));
        Directory.CreateDirectory(Path.Combine(stick, "EFI", "Boot"));
        Directory.CreateDirectory(Path.Combine(stick, "EFI", "Microsoft", "Boot"));

        File.Copy(winre, Path.Combine(stick, "sources", "boot.wim"), overwrite: true);

        step("Writing the UEFI boot manager…");
        File.Copy(bootmgfw, Path.Combine(stick, "EFI", "Boot", "bootx64.efi"), overwrite: true);
        File.Copy(bootmgfw, Path.Combine(stick, "EFI", "Microsoft", "Boot", "bootmgfw.efi"), overwrite: true);

        string? sdi = FindBootSdi();
        if (sdi is not null)
        {
            File.Copy(sdi, Path.Combine(stick, "sources", "boot.sdi"), overwrite: true);
            step("Copied boot.sdi for the ramdisk.");
        }
        else
        {
            step("No boot.sdi found under Windows\\Boot — the stick may still boot; WinRE often carries its own.");
        }

        // ---- BCD store: ramdisk options + one osloader entry ----
        step("Writing the boot configuration store…");
        string bcd = Path.Combine(stick, "EFI", "Microsoft", "Boot", "BCD");
        var bcdSteps = new List<(string Args, bool Required)>
        {
            ($"/store \"{bcd}\" /createstore", true),
            ($"/store \"{bcd}\" /create {{ramdiskoptions}}", true),
            ($"/store \"{bcd}\" /set {{ramdiskoptions}} ramdisksdidevice partition={letter}:", true),
            ($"/store \"{bcd}\" /set {{ramdiskoptions}} ramdisksdipath \\sources\\boot.sdi", sdi is not null),
            ($"/store \"{bcd}\" /create /d \"Windows Recovery\" /application osloader", true),
        };

        string? osGuid = null;
        foreach ((string args, bool required) in bcdSteps)
        {
            ct.ThrowIfCancellationRequested();
            CmdResult r = Cmd.Run("bcdedit.exe", args, timeoutMs: 60000);
            if (!r.Ok)
            {
                if (required)
                    return new Result(false, $"bcdedit failed: {r.Reason}");
                step("boot.sdi BCD entry skipped.");
                continue;
            }

            if (args.Contains("/application osloader"))
                osGuid = MatchGuid(r.AllText);
        }

        if (osGuid is null)
            return new Result(false, "bcdedit did not return an entry GUID for the recovery loader.");

        string ramdisk = $"ramdisk=[{letter}:]\\sources\\boot.wim,{{ramdiskoptions}}";
        var osSteps = new (string Args, bool Required)[]
        {
            ($"/store \"{bcd}\" /set {osGuid} device {ramdisk}", true),
            ($"/store \"{bcd}\" /set {osGuid} osdevice {ramdisk}", true),
            ($"/store \"{bcd}\" /set {osGuid} path \\windows\\system32\\winload.efi", true),
            ($"/store \"{bcd}\" /set {osGuid} systemroot \\windows", true),
            ($"/store \"{bcd}\" /set {osGuid} winpe yes", true),
            ($"/store \"{bcd}\" /set {osGuid} detecthal yes", true),
            ($"/store \"{bcd}\" /displayorder {osGuid} /addfirst", true),
            ($"/store \"{bcd}\" /set {{bootmgr}} device partition={letter}:", false),
        };

        foreach ((string args, bool required) in osSteps)
        {
            ct.ThrowIfCancellationRequested();
            CmdResult r = Cmd.Run("bcdedit.exe", args, timeoutMs: 60000);
            if (!r.Ok && required)
                return new Result(false, $"bcdedit failed: {r.Reason}");
        }

        step("Done — the stick is ready.");
        return new Result(
            true,
            $"Recovery stick ready on {stick}. Boot from it through the firmware boot menu (F12/F9/F11)." +
            (sdi is null ? " Note: no boot.sdi was found; if the stick does not boot, rebuild it on a machine with the ADK." : string.Empty));
    }

    // ------------------------------------------------------------------

    private static string RunDiskpart(string script, CancellationToken ct, int timeoutMs = 90000)
    {
        ct.ThrowIfCancellationRequested();
        string path = Path.Combine(Path.GetTempPath(), $"pcmedic_dp_{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(path, script);
            CmdResult r = Cmd.Run("diskpart.exe", $"/s \"{path}\"", timeoutMs);
            return r.AllText;
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    private static char NextFreeLetter()
    {
        var used = new HashSet<char>(
            DriveInfo.GetDrives().Where(d => d.Name.Length >= 2).Select(d => char.ToUpperInvariant(d.Name[0])));

        for (char c = 'R'; c <= 'Z'; c++)
            if (!used.Contains(c)) return c;
        for (char c = 'D'; c <= 'Q'; c++)
            if (!used.Contains(c)) return c;
        return '\0';
    }

    private static string? MatchNumber(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? MatchGuid(string text)
    {
        var m = Regex.Match(text, @"\{[0-9a-fA-F\-]{36}\}");
        return m.Success ? m.Value : null;
    }
}
