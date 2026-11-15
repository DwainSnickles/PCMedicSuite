using System.Diagnostics;

namespace PC_Medic;

internal enum BulkKind
{
    DellDup,
    Msi,
    Msu,
    Exe,
}

/// <summary>What one package reported, including whether it left a restart behind.</summary>
internal sealed record BulkOutcome(bool Ok, bool RestartNeeded, string Message);

/// <summary>
/// One installer file a bulk run can execute. The format picks the switch set: Dell update packages
/// have their own silent switch, MSI and MSU have documented quiet switches, and any other exe only
/// gets a guessed <c>/S</c> when silent mode is on.
/// </summary>
internal sealed class BulkPackage
{
    public BulkPackage(string path)
    {
        Path = path;
        Kind = DetectKind(path);

        try { Size = new FileInfo(path).Length; }
        catch { Size = 0; }
    }

    public string Path { get; }

    public BulkKind Kind { get; }

    public long Size { get; }

    public string Name => System.IO.Path.GetFileName(Path);

    public string Folder => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

    public string KindLabel => Kind switch
    {
        BulkKind.DellDup => "DUP",
        BulkKind.Msi => "MSI",
        BulkKind.Msu => "MSU",
        _ => "EXE",
    };

    public string SizeText => Size >= 1024 * 1024
        ? $"{Size / 1024d / 1024d:0.0} MB"
        : $"{Math.Max(1, Size / 1024)} KB";

    /// <summary>True when the switch set is documented for the format rather than a guess.</summary>
    public bool HasKnownSilentSwitch => Kind != BulkKind.Exe;

    /// <summary>The exact arguments the row shows before anything runs.</summary>
    public string Arguments(bool silent, string logFile) => Kind switch
    {
        // /norestart plus REBOOT=ReallySuppress: the package is told to hold its restart back.
        BulkKind.Msi => silent
            ? $"/i \"{Path}\" /qn /norestart REBOOT=ReallySuppress /l*v \"{logFile}\""
            : $"/i \"{Path}\" /norestart REBOOT=ReallySuppress /l*v \"{logFile}\"",
        BulkKind.Msu => $"\"{Path}\" /quiet /norestart",
        // Dell's own silent switch. /r is deliberately absent: /s alone installs without a reboot.
        BulkKind.DellDup => silent ? $"/s /l=\"{logFile}\"" : string.Empty,
        _ => silent ? "/S" : string.Empty,
    };

    /// <summary>The process to start: msiexec for an MSI, wusa for an MSU, the file itself otherwise.</summary>
    public string CommandFile => Kind switch
    {
        BulkKind.Msi => "msiexec.exe",
        BulkKind.Msu => "wusa.exe",
        _ => Path,
    };

    private static BulkKind DetectKind(string path)
    {
        string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();

        if (extension == ".msi") return BulkKind.Msi;
        if (extension == ".msu") return BulkKind.Msu;

        return IsDellPackage(path) ? BulkKind.DellDup : BulkKind.Exe;
    }

    /// <summary>Dell stamps its update packages with its company name; that is the reliable signal.</summary>
    private static bool IsDellPackage(string path)
    {
        try
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
            string blob = $"{info.CompanyName} {info.ProductName} {info.FileDescription} {info.OriginalFilename}"
                .ToUpperInvariant();

            return blob.Contains("DELL");
        }
        catch
        {
            return System.IO.Path.GetFileName(path).StartsWith("Dell-", StringComparison.OrdinalIgnoreCase);
        }
    }
}

/// <summary>
/// Runs a folder of vendor installers. Nothing is downloaded: the files are already on disk, so the
/// work is to name them, choose a switch set and wait — one at a time, because two Windows Installer
/// operations cannot run together and a queued batch is easier to read in a log than a fan-out.
/// </summary>
internal static class BulkInstaller
{
    private static readonly string[] Extensions = { ".exe", ".msi", ".msu" };

    /// <summary>ERROR_INSTALL_ALREADY_RUNNING — wait for the other installer instead of failing.</summary>
    private const int InstallerBusy = 1618;

    private const int BusyRetries = 20;
    private static readonly TimeSpan BusyWait = TimeSpan.FromSeconds(15);

    public static bool IsInstaller(string path) =>
        Extensions.Contains(System.IO.Path.GetExtension(path).ToLowerInvariant());

    public static List<BulkPackage> ScanFolder(string folder, bool recursive)
    {
        var found = new List<BulkPackage>();

        foreach (string file in Directory.EnumerateFiles(
                     folder, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
            if (IsInstaller(file)) found.Add(new BulkPackage(file));
        }

        found.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        return found;
    }

    /// <summary>
    /// Runs one package and reports what happened. A cancelled run stops the queue, never the
    /// installer in flight: killing Setup.exe halfway through leaves a machine half-updated, so the
    /// package is allowed to finish and the next one is skipped.
    /// </summary>
    public static async Task<BulkOutcome> RunAsync(
        BulkPackage package,
        bool silent,
        IProgress<int>? progress,
        CancellationToken token)
    {
        string logFile = LogFileFor(package);
        var watch = Stopwatch.StartNew();

        Log.Info($"Bulk install: {package.Name} ({package.KindLabel}, {(silent ? "silent" : "its own UI")})");
        Log.Info($"  {package.CommandFile} {package.Arguments(silent, logFile)}");

        try
        {
            int exitCode = 0;

            for (int attempt = 1; attempt <= BusyRetries; attempt++)
            {
                if (token.IsCancellationRequested) return new BulkOutcome(false, false, "Cancelled.");

                exitCode = await ExecuteAsync(package, silent, logFile, progress).ConfigureAwait(false);

                if (exitCode != InstallerBusy || attempt == BusyRetries) break;

                Log.Warn($"  another Windows Installer operation is running — waiting {BusyWait.TotalSeconds:0}s");
                await Task.Delay(BusyWait).ConfigureAwait(false);
            }

            watch.Stop();
            progress?.Report(100);

            BulkOutcome outcome = Judge(package, exitCode, watch.Elapsed);
            if (outcome.Ok) Log.Info($"  {outcome.Message}");
            else Log.Error($"  {outcome.Message}");

            return outcome;
        }
        catch (Exception ex)
        {
            Log.Error($"Bulk install of {package.Name} threw", ex);
            return new BulkOutcome(false, false, $"{package.Name}: {ex.Message}");
        }
    }

    private static async Task<int> ExecuteAsync(
        BulkPackage package,
        bool silent,
        string logFile,
        IProgress<int>? progress)
    {
        var start = new ProcessStartInfo
        {
            FileName = package.CommandFile,
            Arguments = package.Arguments(silent, logFile),
            WorkingDirectory = package.Folder,
            UseShellExecute = false,
            CreateNoWindow = silent,
        };

        using var process = new Process { StartInfo = start };

        if (!process.Start()) throw new InvalidOperationException("the installer could not be started");

        // An installer reports nothing measurable, so the bar eases toward its ceiling and resolves
        // when the process exits — the same rule the rest of the suite follows for opaque work.
        Task easing = EaseAsync(process, progress);

        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        await easing.ConfigureAwait(false);

        return process.ExitCode;
    }

    private static async Task EaseAsync(Process process, IProgress<int>? progress)
    {
        for (int percent = 10; percent < 90; percent += 10)
        {
            await Task.Delay(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
            if (process.HasExited) return;

            progress?.Report(percent);
        }
    }

    private static BulkOutcome Judge(BulkPackage package, int exitCode, TimeSpan elapsed)
    {
        string took = elapsed.TotalMinutes >= 1
            ? $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds:00}s"
            : $"{Math.Max(1, (int)elapsed.TotalSeconds)}s";

        if (package.Kind == BulkKind.DellDup) return JudgeDell(package, exitCode, took);

        if (exitCode == 0) return new BulkOutcome(true, false, $"Installed {package.Name} · {took}");

        // The install worked; Windows just wants a restart to finish it. That is held for the batch.
        if (RebootDecision.IsRebootExitCode(exitCode))
            return new BulkOutcome(true, true, $"Installed {package.Name} · restart held · {took}");

        // WU_S_ALREADY_INSTALLED: the package is simply newer than what this PC already has.
        if (unchecked((uint)exitCode) == 2359302)
            return new BulkOutcome(true, false, $"{package.Name} was already installed");

        return new BulkOutcome(false, false, $"{package.Name} failed — {Describe(exitCode)}");
    }

    /// <summary>
    /// Dell's packages do not use Windows Installer codes: 2, 6 and 14 all mean the update worked and
    /// wants a restart, and 3 and 13 mean there was simply nothing to do. Reading 2 as a failure would
    /// mark every successful BIOS and firmware update on a Dell as broken.
    /// </summary>
    private static BulkOutcome JudgeDell(BulkPackage package, int exitCode, string took) => exitCode switch
    {
        0 => new BulkOutcome(true, false, $"Installed {package.Name} · {took}"),
        2 => new BulkOutcome(true, true, $"Installed {package.Name} · restart held · {took}"),
        6 => new BulkOutcome(true, true, $"Installed {package.Name} · it restarted on its own · {took}"),
        14 => new BulkOutcome(true, true, $"Installed {package.Name} · restart held · a soft dependency is missing · {took}"),
        3 or 13 => new BulkOutcome(true, false, $"{package.Name}: nothing to do — already at this version"),
        4 => new BulkOutcome(false, false, $"{package.Name} failed — a required dependency is missing (4)"),
        5 => new BulkOutcome(false, false, $"{package.Name} failed — this package does not apply to this PC (5)"),
        _ => new BulkOutcome(false, false, $"{package.Name} failed — {Describe(exitCode)}"),
    };

    private static string Describe(int exitCode) => unchecked((uint)exitCode) switch
    {
        5 => "access denied — PC Medic needs administrator rights (5)",
        1602 => "the install was cancelled (1602)",
        1603 => "a fatal error during installation (1603)",
        1618 => "another installer was still running (1618)",
        1619 => "the package could not be opened (1619)",
        1620 => "the file is not a valid installer package (1620)",
        1633 => "this platform is not supported by the package (1633)",
        1638 => "another version is already installed (1638)",
        _ => $"exit code 0x{unchecked((uint)exitCode):X} ({exitCode})",
    };

    /// <summary>One log per package, so a failed install can be read back without hunting through output.</summary>
    private static string LogFileFor(BulkPackage package)
    {
        string folder = Path.Combine(AppPaths.LogDir, "bulk");

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            Log.Warn("Bulk installer log folder: " + ex.Message);
            return Path.Combine(AppPaths.LogDir, $"bulk-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        }

        string stem = new string(
            Path.GetFileNameWithoutExtension(package.Path)
                .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character)
                .ToArray());

        return Path.Combine(folder, $"{stem}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }
}
