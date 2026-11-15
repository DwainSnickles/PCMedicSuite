using System.Diagnostics;
using System.Text;

namespace PC_Medic;

/// <summary>
/// wimlib-imagex disk imaging ported from the legacy Backup &amp; Restore tool:
/// image capture with an exclusion config, apply with integrity check, and
/// "wimlib-imagex info" parsing for multi-image archives.
/// The engine ships in tools\ and is copied to bin\ next to the exe; PATH is the fallback.
/// </summary>
internal static class ImagingService
{
    private const string ImageName = "WindowsBackup";
    private const string ImageDescription = "Created by PC Medic";

    /// <summary>One image inside a .wim/.esd archive.</summary>
    public sealed record WimInfo(int Index, string Name, string Description, string SizeText)
    {
        public override string ToString() => $"#{Index}  {Name}";
    }

    public sealed record BackupResult(bool Success, string Message, bool Aborted);

    public sealed record RestoreResult(
        bool ApplySucceeded,
        bool BootRepairSucceeded,
        string Message,
        bool Aborted,
        BootRepairService.BootRepairResult? BootRepair);

    private static readonly string BundledExe =
        Path.Combine(AppContext.BaseDirectory, "bin", "wimlib-imagex.exe");

    /// <summary>Bundled copy first, bare name (PATH lookup) as fallback — same order as the original tool.</summary>
    public static string WimlibPath => File.Exists(BundledExe) ? BundledExe : "wimlib-imagex.exe";

    /// <summary>True when the bundled exe exists or wimlib-imagex.exe resolves on PATH.</summary>
    public static bool IsAvailable => File.Exists(BundledExe) || OnPath();

    /// <summary>Human-readable location for the status line.</summary>
    public static string WimlibStatus =>
        File.Exists(BundledExe) ? "wimlib: bundled" : OnPath() ? "wimlib: PATH" : "wimlib: MISSING";

    private static bool OnPath()
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path)) return false;

        foreach (string dir in path.Split(Path.PathSeparator))
        {
            try
            {
                if (File.Exists(Path.Combine(dir, "wimlib-imagex.exe"))) return true;
            }
            catch { }
        }

        return false;
    }

    // ------------------------------------------------------------------
    // Image listing
    // ------------------------------------------------------------------

    /// <summary>Runs "wimlib-imagex info" and parses the per-image Index/Name/Description blocks.</summary>
    public static async Task<List<WimInfo>> ReadImagesAsync(string wimPath, CancellationToken ct = default)
    {
        var result = await ProcessRunner.RunAsync(
            WimlibPath,
            $"info \"{wimPath}\"",
            ct,
            timeout: TimeSpan.FromSeconds(90));

        if (result.ExitCode != 0)
        {
            string detail = Tail(result.Combined);
            throw new InvalidOperationException(
                string.IsNullOrEmpty(detail) ? $"wimlib-imagex info exited {result.ExitCode}." : detail);
        }

        return ParseWimInfo(result.StdOut);
    }

    private static List<WimInfo> ParseWimInfo(string output)
    {
        var images = new List<WimInfo>();
        int index = 0;
        string name = string.Empty;
        string description = string.Empty;
        string size = string.Empty;

        void Flush()
        {
            if (index > 0)
                images.Add(new WimInfo(index, name, description, size));
        }

        foreach (string raw in output.Split('\n'))
        {
            string line = raw.Trim();

            if (line.StartsWith("Index:", StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                index = int.TryParse(line.Substring(6).Trim(), out int parsed) ? parsed : 0;
                name = description = size = string.Empty;
            }
            else if (line.StartsWith("Name:", StringComparison.OrdinalIgnoreCase))
            {
                name = line.Substring(5).Trim();
            }
            else if (line.StartsWith("Description:", StringComparison.OrdinalIgnoreCase))
            {
                description = line.Substring(12).Trim();
            }
            else if (size.Length == 0 && line.StartsWith("Total Bytes:", StringComparison.OrdinalIgnoreCase))
            {
                string digits = new string(line.Substring(12).Where(char.IsDigit).ToArray());
                if (ulong.TryParse(digits, out ulong bytes)) size = Theme.Bytes(bytes);
            }
        }

        Flush();
        return images;
    }

    // ------------------------------------------------------------------
    // Compression and sizing
    //
    // The one mapping: what the engine will actually use, and how big that makes an image. The
    // backup page's room check and the wizard's verdict both go through these, so they can never
    // disagree about whether an image fits.
    // ------------------------------------------------------------------

    /// <summary>
    /// The compression the capture will really use. A .esd destination is always LZMS solid, whatever
    /// the picker says, because that is what the engine is told to write.
    /// </summary>
    public static string EffectiveCompression(string compression, string imagePath) =>
        imagePath.EndsWith(".esd", StringComparison.OrdinalIgnoreCase) ? "maximum" : compression;

    /// <summary>Share of a volume's used bytes an image is expected to take, by compression.</summary>
    private static double CompressionFactor(string compression) => compression switch
    {
        "none" => 1.0,
        "maximum" => 0.38,
        _ => 0.55,
    };

    /// <summary>Estimated image size for a volume's used bytes, honouring the .esd rule.</summary>
    public static long EstimateBytes(long usedBytes, string compression, string imagePath) =>
        (long)(Math.Max(1, usedBytes) * CompressionFactor(EffectiveCompression(compression, imagePath)));

    // ------------------------------------------------------------------
    // Capture
    // ------------------------------------------------------------------

    /// <summary>
    /// Captures a volume to a .wim/.esd with wimlib. NeverKill policy: imaging is never
    /// interrupted mid-write by the runner; an abort request kills wimlib-imagex via
    /// taskkill and the partial archive is deleted afterwards.
    /// </summary>
    public static async Task<BackupResult> CaptureAsync(
        string sourceRoot,
        string wimPath,
        string compression,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
            return new BackupResult(false, $"Source '{sourceRoot}' is not a readable folder.", false);
        if (string.IsNullOrWhiteSpace(wimPath))
            return new BackupResult(false, "No destination image path selected.", false);

        string exe = WimlibPath;
        string configPath = WriteExclusionConfig(sourceRoot);
        string safeSource = EscapeRoot(sourceRoot);

        string effective = EffectiveCompression(compression, wimPath);
        string compressArg = effective switch
        {
            "none" => "--compress=none",
            "maximum" => "--compress=LZMS",
            _ => "--compress=fast",
        };

        var extra = new StringBuilder(" --check --no-acls --snapshot");
        if (effective == "maximum") extra.Append(" --solid");
        extra.Append($" --config=\"{configPath}\"");

        string arguments =
            $"capture \"{safeSource}\" \"{wimPath}\" \"{ImageName}\" \"{ImageDescription}\" " +
            $"{compressArg}{extra}";

        ActivityLog.Info($"Disk image: capturing {sourceRoot} to {wimPath} ({compressArg.TrimStart('-')}).");

        // Capture can run for an hour: hold the machine awake, screen included, so nobody walks away
        // and comes back to a half-written image.
        using var awake = KeepAwake.Enter(keepDisplayAwake: true);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var registration = linked.Token.Register(KillImagingProcesses);

        var watch = Stopwatch.StartNew();
        try
        {
            var result = await ProcessRunner.RunAsync(
                exe,
                arguments,
                linked.Token,
                line => ReportPercent(line, progress),
                timeout: null,
                abortPolicy: ProcessAbortPolicy.NeverKill);

            bool aborted = ct.IsCancellationRequested;
            if (aborted || result.ExitCode != 0)
            {
                if (aborted)
                {
                    ActivityLog.Warn("Backup aborted by user; removing the partial image.");
                    await DeletePartialAsync(wimPath, ct);
                    return new BackupResult(false, "Backup aborted; partial image deleted.", true);
                }

                string detail = Tail(result.Combined);
                ActivityLog.Error($"wimlib-imagex capture failed (exit {result.ExitCode}){(detail.Length > 0 ? ": " + detail : ".")}");
                return new BackupResult(false, $"Capture failed: wimlib-imagex exited {result.ExitCode}.", false);
            }

            string elapsed = watch.Elapsed.TotalMinutes >= 1
                ? $"{watch.Elapsed.TotalMinutes:0.0} min"
                : $"{watch.Elapsed.TotalSeconds:0} s";
            ActivityLog.Success($"Disk image written to {wimPath} in {elapsed}.");
            return new BackupResult(true, $"Image written in {elapsed}.", false);
        }
        finally
        {
            TryDelete(configPath);
        }
    }

    // ------------------------------------------------------------------
    // Apply + boot repair
    // ------------------------------------------------------------------

    /// <summary>
    /// Applies an image index to a target root, then repairs boot files on it. uefi decides which
    /// boot files bcdboot writes first: the caller passes the machine's real mode (the FIRMWARE
    /// picker), because a legacy machine restored with the UEFI path only recovers through the
    /// /f ALL fallback.
    /// </summary>
    public static async Task<RestoreResult> ApplyAsync(
        string wimPath,
        int imageIndex,
        string targetRoot,
        bool uefi,
        IProgress<int>? progress,
        IProgress<string>? bootLog,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(wimPath) || !File.Exists(wimPath))
            return new RestoreResult(false, false, $"Image '{wimPath}' was not found.", false, null);
        if (string.IsNullOrWhiteSpace(targetRoot) || !Directory.Exists(targetRoot.TrimEnd('\\') + "\\"))
            return new RestoreResult(false, false, $"Target '{targetRoot}' is not a writable drive root.", false, null);

        string exe = WimlibPath;
        string safeTarget = EscapeRoot(targetRoot);
        string arguments = $"apply \"{wimPath}\" {imageIndex} \"{safeTarget}\" --check";

        ActivityLog.Info(
            $"Disk image: applying index {imageIndex} from {wimPath} to {targetRoot} ({(uefi ? "UEFI" : "BIOS")} boot files).");

        using var awake = KeepAwake.Enter(keepDisplayAwake: true);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var registration = linked.Token.Register(KillImagingProcesses);

        var result = await ProcessRunner.RunAsync(
            exe,
            arguments,
            linked.Token,
            line => ReportPercent(line, progress),
            timeout: null,
            abortPolicy: ProcessAbortPolicy.NeverKill);

        if (ct.IsCancellationRequested)
        {
            ActivityLog.Warn("Restore aborted by user.");
            return new RestoreResult(false, false, "Restore aborted.", true, null);
        }

        if (result.ExitCode != 0)
        {
            string detail = Tail(result.Combined);
            ActivityLog.Error($"wimlib-imagex apply failed (exit {result.ExitCode}){(detail.Length > 0 ? ": " + detail : ".")}");
            return new RestoreResult(false, false, $"Apply failed: wimlib-imagex exited {result.ExitCode}.", false, null);
        }

        ActivityLog.Success($"Image index {imageIndex} applied to {targetRoot}; repairing boot files.");

        BootRepairService.BootRepairResult boot =
            await BootRepairService.RepairAsync(targetRoot, uefi, bootLog, ct);

        bool ok = boot.Success;
        return new RestoreResult(
            ApplySucceeded: true,
            BootRepairSucceeded: ok,
            Message: ok
                ? "Restore and boot repair completed."
                : "Files restored, but boot repair reported problems — the target may not boot.",
            Aborted: false,
            BootRepair: boot);
    }

    /// <summary>
    /// Runs "wimlib-imagex verify", which reads the whole archive and checks every resource hash -
    /// the only check that catches a bad sector or a half-copied image. That is slow on a real image
    /// because it decompresses everything, so callers report progress rather than showing a spinner.
    /// </summary>
    public static async Task<(bool Ok, string Message)> VerifyAsync(
        string wimPath,
        IProgress<int>? progress,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(wimPath) || !File.Exists(wimPath))
            return (false, $"Image '{wimPath}' was not found.");

        var result = await ProcessRunner.RunAsync(
            WimlibPath,
            $"verify \"{wimPath}\"",
            ct,
            line => ReportPercent(line, progress),
            timeout: null);

        if (result.ExitCode == 0) return (true, "Archive verified.");

        string detail = Tail(result.Combined);
        return (false, detail.Length > 0 ? detail : $"wimlib-imagex verify exited {result.ExitCode}.");
    }

    // ------------------------------------------------------------------
    // Shared helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// wimlib-imagex parses its own argv: a trailing backslash inside quotes escapes the
    /// closing quote, so "D:\" must reach the process as "D:\\".
    /// </summary>
    private static string EscapeRoot(string root)
    {
        string trimmed = root.Trim().TrimEnd('\\');
        return trimmed + "\\\\";
    }

    /// <summary>Ported exclusion config: fixed system files plus per-user cloud sync and temp folders.</summary>
    private static string WriteExclusionConfig(string sourceRoot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[ExclusionList]");
        sb.AppendLine("\\System Volume Information");
        sb.AppendLine("\\$RECYCLE.BIN");
        sb.AppendLine("\\pagefile.sys");
        sb.AppendLine("\\hiberfil.sys");
        sb.AppendLine("\\swapfile.sys");

        try
        {
            string users = Path.Combine(sourceRoot.TrimEnd('\\'), "Users");
            if (Directory.Exists(users))
            {
                foreach (string userDir in Directory.GetDirectories(users))
                {
                    string user = Path.GetFileName(userDir);
                    foreach (string cloud in new[] { "OneDrive", "Dropbox", "Google Drive", "SkyDrive" })
                    {
                        if (Directory.Exists(Path.Combine(userDir, cloud)))
                            sb.AppendLine($"\\Users\\{user}\\{cloud}");
                    }

                    if (Directory.Exists(Path.Combine(userDir, "AppData", "Local", "Temp")))
                        sb.AppendLine($"\\Users\\{user}\\AppData\\Local\\Temp");
                }
            }
        }
        catch { }

        string path = Path.Combine(Path.GetTempPath(), $"wimlib_excl_{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }

    /// <summary>Ported KillProcesses: taskkill the imaging engine and its children.</summary>
    private static void KillImagingProcesses()
    {
        try
        {
            Process.Start(new ProcessStartInfo("taskkill.exe", "/F /IM wimlib-imagex.exe /T")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            });
        }
        catch { }
    }

    /// <summary>wimlib redraws progress with '\r'; ProcessRunner already splits those into fragments.</summary>
    private static void ReportPercent(string line, IProgress<int>? progress)
    {
        if (progress is null) return;
        int? pct = WingetText.ParsePercent(line);
        if (pct is > 0) progress.Report(pct.Value);
    }

    private static async Task DeletePartialAsync(string wimPath, CancellationToken ct)
    {
        for (int attempt = 0; attempt < 5 && File.Exists(wimPath); attempt++)
        {
            try
            {
                File.Delete(wimPath);
                break;
            }
            catch
            {
                try { await Task.Delay(1000, ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static string Tail(string text, int maxLines = 3)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" | ", lines.TakeLast(maxLines).Select(l => l.Trim())).Trim();
    }
}
