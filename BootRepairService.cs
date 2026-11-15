using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PC_Medic;

/// <summary>
/// Single-target port of the legacy "Auto Boot Fix": finds offline Windows installs,
/// locates the disk/partition through diskpart (no WMI in this project), detects GPT
/// vs MBR, then runs bcdboot with a /f ALL fallback and bcdedit to pin the boot manager.
/// </summary>
internal static class BootRepairService
{
    public sealed record BootRepairResult(bool Success, List<string> Steps, string Message);

    /// <summary>Fixed, ready drives (other than the running OS) that hold a valid Windows install.</summary>
    public static async Task<List<string>> FindWindowsInstallsAsync(CancellationToken ct = default)
    {
        var found = new List<string>();
        string sys = (Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\")
            .TrimEnd('\\');

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            ct.ThrowIfCancellationRequested();
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;

            string root = drive.Name;
            if (root.TrimEnd('\\').Equals(sys, StringComparison.OrdinalIgnoreCase)) continue;
            if (IsValidWindowsInstall(root)) found.Add(root);
        }

        await Task.CompletedTask;
        return found;
    }

    /// <summary>ntoskrnl plus at least one bootloader variant — the source tool's validity check.</summary>
    private static bool IsValidWindowsInstall(string driveRoot)
    {
        try
        {
            string winDir = Path.Combine(driveRoot, "Windows");
            if (!Directory.Exists(winDir)) return false;
            if (!File.Exists(Path.Combine(winDir, "System32", "ntoskrnl.exe"))) return false;

            string winloadEfi = Path.Combine(winDir, "System32", "winload.efi");
            string winloadExe = Path.Combine(winDir, "System32", "winload.exe");
            return File.Exists(winloadEfi) || File.Exists(winloadExe);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// The firmware this machine booted from - "UEFI", "BIOS", or empty when Windows will not say.
    /// The FIRMWARE picker and the recovery pointer both default from this, so a restore or a boot
    /// repair starts from the mode the machine actually uses instead of an assumed one.
    /// </summary>
    public static string MachineFirmware()
    {
        try
        {
            if (!GetFirmwareType(out FirmwareType type)) return string.Empty;

            return type switch
            {
                FirmwareType.Uefi => "UEFI",
                FirmwareType.Bios => "BIOS",
                _ => string.Empty,
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// The /f argument to try first. A GPT disk is always UEFI; an MBR disk keeps the caller's mode,
    /// because a legacy machine needs the BIOS files first. A failed attempt still falls back to
    /// /f ALL below, so one wrong guess cannot leave the install without any boot files.
    /// </summary>
    private static string FirmwareArgument(bool isGpt, bool uefi) => isGpt || uefi ? "UEFI" : "BIOS";

    private enum FirmwareType
    {
        Unknown = 0,
        Bios = 1,
        Uefi = 2,
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFirmwareType(out FirmwareType type);

    /// <summary>
    /// Repairs boot files for one Windows install. windowsRoot may be "D:\" or "D:\Windows".
    /// uefi is the preferred firmware; a GPT disk always gets the UEFI path first.
    /// </summary>
    public static async Task<BootRepairResult> RepairAsync(
        string windowsRoot,
        bool uefi,
        IProgress<string>? log,
        CancellationToken ct = default)
    {
        var steps = new List<string>();
        void Step(string message)
        {
            steps.Add(message);
            log?.Report(message);
        }

        // Normalise to a drive root ("D:\") and a Windows directory ("D:\Windows").
        string root = (windowsRoot ?? string.Empty).Trim().Replace('/', '\\');
        if (!root.Contains(':')) root = root.TrimEnd('\\') + ":\\";

        string driveRoot = root.EndsWith("\\Windows", StringComparison.OrdinalIgnoreCase)
            ? root[..^"\\Windows".Length]
            : root;
        if (!driveRoot.EndsWith("\\")) driveRoot += "\\";

        string letter = driveRoot[..1].ToUpperInvariant();
        string windowsDir = Path.Combine(driveRoot, "Windows");

        Step($"Inspecting {driveRoot} for a Windows install…");
        if (!IsValidWindowsInstall(driveRoot))
        {
            return new BootRepairResult(false, steps, $"{driveRoot} does not contain a bootable Windows install.");
        }

        // Disk/partition location through diskpart — the project has no System.Management.
        var location = await GetVolumeLocationAsync(letter, ct);
        bool isGPT = location.Disk is not null && await IsDiskGPTAsync(location.Disk, ct);
        string firmware = FirmwareArgument(isGPT, uefi);
        Step($"Disk layout: {(location.Disk is null ? "unknown" : "disk " + location.Disk)}, {(isGPT ? "GPT" : "MBR")}; trying /f {firmware}.");

        // Boot partition: a mounted EFI system partition wins for UEFI; otherwise the
        // Windows volume itself is the /s target (the common single-partition case).
        string bootTarget = driveRoot.TrimEnd('\\') + ":";
        if (firmware == "UEFI")
        {
            string? esp = FindEspDrive();
            if (esp is not null && !esp.TrimEnd('\\').Equals(driveRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                bootTarget = esp.TrimEnd('\\') + ":";
                Step($"Using EFI system partition {esp} as the boot target.");
            }
        }

        // Legacy MBR: make sure the Windows partition carries the active flag.
        if (!isGPT && location.Disk is { } disk && location.Partition is { } part)
        {
            Step("MBR layout: marking the Windows partition active.");
            await RunDiskpartAsync($"select disk {disk}\nselect partition {part}\nactive", ct);
        }

        Step($"Running: bcdboot \"{windowsDir}\" /f {firmware} /s {bootTarget}");
        var first = await ProcessRunner.RunAsync(
            "bcdboot.exe",
            $"\"{windowsDir}\" /f {firmware} /s {bootTarget}",
            ct,
            timeout: TimeSpan.FromMinutes(3));

        bool ok = first.ExitCode == 0;
        string used = firmware;
        if (!ok)
        {
            string detail = Tail(first.Combined);
            Step($"bcdboot /f {firmware} failed (exit {first.ExitCode}){(detail.Length > 0 ? ": " + detail : "")}; retrying /f ALL.");
            var second = await ProcessRunner.RunAsync(
                "bcdboot.exe",
                $"\"{windowsDir}\" /f ALL /s {bootTarget}",
                ct,
                timeout: TimeSpan.FromMinutes(3));
            ok = second.ExitCode == 0;
            used = "ALL";
        }

        // The firmware boot order belongs only to a run that wrote UEFI boot files: after /f BIOS the
        // firmware's own entries are left alone. A successful bcdboot already needs elevation, so
        // bcdedit reports its own refusal if it ever has to.
        if (ok && used != "BIOS")
        {
            Step("Setting Windows Boot Manager first in the UEFI boot order.");
            var bcdedit = await ProcessRunner.RunAsync(
                "bcdedit.exe",
                "/set {fwbootmgr} displayorder {bootmgr} /addfirst",
                ct,
                timeout: TimeSpan.FromSeconds(60));
            if (bcdedit.ExitCode != 0)
                Step("bcdedit displayorder did not apply.");
        }
        else if (ok)
        {
            Step("Skipping bcdedit boot-order change: BIOS boot files were written, not UEFI.");
        }

        Step(ok
            ? $"Boot files written for {driveRoot} (/f {used})."
            : $"bcdboot could not write boot files for {driveRoot}.");

        return new BootRepairResult(
            ok,
            steps,
            ok ? "Boot repair completed." : "bcdboot failed to write boot files.");
    }

    // ------------------------------------------------------------------
    // diskpart helpers (minimal port of the source's WMI/diskpart lookups)
    // ------------------------------------------------------------------

    private static async Task<string> RunDiskpartAsync(string script, CancellationToken ct)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pcmedic_dp_{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(path, script);
            var result = await ProcessRunner.RunAsync(
                "diskpart.exe",
                $"/s \"{path}\"",
                ct,
                timeout: TimeSpan.FromSeconds(90));
            return result.Combined;
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    /// <summary>"select volume X" + "detail volume" → (Disk ###, Partition ###), either may be null.</summary>
    private static async Task<(string? Disk, string? Partition)> GetVolumeLocationAsync(
        string letter, CancellationToken ct)
    {
        string output = await RunDiskpartAsync(
            $"select volume {letter}\ndetail volume", ct);

        return (
            MatchNumber(output, @"Disk\s*#+\s*:\s*(\d+)") ?? MatchNumber(output, @"Disk\s+(\d+)"),
            MatchNumber(output, @"Partition\s*#+\s*:\s*(\d+)") ?? MatchNumber(output, @"Partition\s+(\d+)"));
    }

    /// <summary>"list disk": the Gpt column carries '*' on GPT disks (same heuristic as the source).</summary>
    private static async Task<bool> IsDiskGPTAsync(string diskIndex, CancellationToken ct)
    {
        string output = await RunDiskpartAsync("list disk", ct);
        foreach (string raw in output.Split('\n'))
        {
            string line = raw.Trim();
            if (!line.StartsWith("Disk ", StringComparison.OrdinalIgnoreCase)) continue;
            if (!line.Contains(diskIndex, StringComparison.Ordinal)) continue;

            // Confirm the disk number token matches, not just a substring of the size.
            var match = Regex.Match(line, @"^Disk\s+(\d+)\b");
            if (match.Success && match.Groups[1].Value == diskIndex)
                return line.Contains('*');
        }

        return true; // Modern systems fallback, same as the source.
    }

    /// <summary>A mounted EFI system partition, located by its boot manager file rather than WMI.</summary>
    private static string? FindEspDrive()
    {
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
            try
            {
                if (File.Exists(Path.Combine(drive.Name, "EFI", "Microsoft", "Boot", "bootmgfw.efi")))
                    return drive.Name;
            }
            catch { }
        }

        return null;
    }

    private static string? MatchNumber(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string Tail(string text, int maxLines = 3)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" | ", lines.TakeLast(maxLines).Select(l => l.Trim())).Trim();
    }
}
