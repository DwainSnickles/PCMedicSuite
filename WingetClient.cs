// ---------------------------------------------------------------------------------------------
// Ported from AppGeek (https://github.com/techygeekshome/AppGeek), Copyright (C) 2026
// TechyGeeksHome, and adapted for PC Medic.
//
// AppGeek is free software under the GNU General Public License v3.0, and PC Medic is too from
// this port onwards. See LICENSE.
// ---------------------------------------------------------------------------------------------

using System.Diagnostics;
namespace PC_Medic;

/// <summary>Whether a run is installing something new, moving an installed app forward, or removing one.</summary>
internal enum RunAction { Install, Update, Uninstall }

/// <summary>
/// Wraps the Windows Package Manager CLI.
///
/// The service boundary is deliberately narrow so it can be swapped for the COM API later without
/// the rest of the app noticing. Every argument the updater passes is explicit: --id with --exact,
/// so a name is never resolved to the wrong package, and --scope pinned to where the app already
/// lives, so an upgrade is an upgrade rather than a second copy somewhere else.
/// </summary>
internal sealed class WingetClient
{
    private const string CommonArgs = "--disable-interactivity --accept-source-agreements";

    private string? _exePath;
    private bool _probed;

    public string? ExePath
    {
        get { Probe(); return _exePath; }
    }

    public bool IsAvailable => ExePath is not null;

    /// <summary>Whatever <c>winget --version</c> printed, e.g. "v1.6.3482". Null until probed.</summary>
    public string? Version { get; private set; }

    /// <summary>The last failure winget reported in a readable sentence, for the page status line.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Clears the cached probe result and looks for winget again. Used after the
    /// bootstrapper repairs a missing App Installer, so the app notices immediately
    /// instead of asking the user to restart it.
    /// </summary>
    public void Reprobe()
    {
        _probed = false;
        _exePath = null;
        Version = null;
        Probe();
    }

    private void Probe()
    {
        if (_probed) return;
        _probed = true;

        // winget lives in the per-user WindowsApps alias folder, which is on PATH for
        // interactive sessions but not always for services or scheduled tasks.
        var candidates = new List<string>
        {
            "winget.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "Microsoft", "WindowsApps", "winget.exe")
        };

        foreach (var c in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo(c, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p is null) continue;
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(8000);
                if (p.ExitCode == 0)
                {
                    _exePath = c;
                    Version = WingetText.Clean(output).Trim();
                    ActivityLog.Info($"winget found at '{c}' ({Version})");
                    return;
                }
            }
            catch { /* try the next candidate */ }
        }

        ActivityLog.Warn("winget was not found on this PC. Install 'App Installer' from the Microsoft Store.");
    }

    /// <summary>Everything winget can see as installed, used to attach package IDs to registry entries.</summary>
    public async Task<List<WingetRow>> ListAsync(ScanPhase? phase = null, CancellationToken ct = default)
    {
        LastError = null;
        if (ExePath is null) return new List<WingetRow>();

        int lines = 0;
        ProcessResult r = await ProcessRunner.RunAsync(ExePath!, $"list {CommonArgs}", ct,
            onOutputLine: _ => phase?.ReportUncounted(++lines),
            timeout: TimeSpan.FromMinutes(4)).ConfigureAwait(false);

        if (!r.Success)
        {
            LastError = $"winget list: {DescribeExitCode(r.ExitCode)}";
            ActivityLog.Warn($"{LastError} {Trim(r.StdErr)}");
        }

        phase?.Complete();
        return WingetText.ParseTable(r.StdOut);
    }

    /// <summary>
    /// Packages with a newer version available. <paramref name="includePinned"/> keeps
    /// winget-pinned packages in the list (marked) instead of hiding them — the Software
    /// page shows pins rather than making them silently disappear.
    /// </summary>
    public async Task<List<WingetRow>> UpgradesAsync(bool includeUnknown, ScanPhase? phase = null,
                                                     bool includePinned = false,
                                                     CancellationToken ct = default)
    {
        LastError = null;
        if (ExePath is null) return new List<WingetRow>();

        string args = $"upgrade {CommonArgs}" +
                      (includeUnknown ? " --include-unknown" : "") +
                      (includePinned ? " --include-pinned" : "");
        int lines = 0;
        ProcessResult r = await ProcessRunner.RunAsync(ExePath!, args, ct,
            onOutputLine: _ => phase?.ReportUncounted(++lines),
            timeout: TimeSpan.FromMinutes(4)).ConfigureAwait(false);

        if (!r.Success)
        {
            LastError = $"winget upgrade: {DescribeExitCode(r.ExitCode)}";
            ActivityLog.Warn($"{LastError} {Trim(r.StdErr)}");
        }

        phase?.Complete();
        return WingetText.ParseTable(r.StdOut);
    }

    /// <summary>IDs winget itself has pinned via "winget pin add" — they never upgrade on their own.</summary>
    public async Task<HashSet<string>> PinListAsync(CancellationToken ct = default)
    {
        var pinned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ExePath is null) return pinned;

        ProcessResult r = await ProcessRunner.RunAsync(ExePath!, "pin list", ct,
            timeout: TimeSpan.FromMinutes(1)).ConfigureAwait(false);
        if (!r.Success && string.IsNullOrWhiteSpace(r.StdOut)) return pinned;

        foreach (var row in WingetText.ParseTable(r.StdOut))
        {
            var id = row.Get(WingetColumn.Id);
            if (!string.IsNullOrWhiteSpace(id) && !AppPackageMatcher.ContainsEllipsis(id))
                pinned.Add(id);
        }
        return pinned;
    }

    /// <summary>Pins or unpins a package through winget itself.</summary>
    public async Task<ProcessResult> SetPinAsync(string packageId, bool pin, CancellationToken ct = default)
    {
        if (ExePath is null)
            return new ProcessResult(-1, "", "winget is not available on this PC.");

        string args = pin
            ? $"pin add --id \"{packageId}\" --accept-source-agreements"
            : $"pin remove --id \"{packageId}\" --accept-source-agreements";

        return await ProcessRunner.RunAsync(ExePath!, args, ct,
            timeout: TimeSpan.FromMinutes(2)).ConfigureAwait(false);
    }

    /// <summary>Searches the winget catalogue for a package by name or ID.</summary>
    public async Task<List<WingetRow>> SearchAsync(string query, CancellationToken ct = default)
    {
        LastError = null;
        if (ExePath is null || string.IsNullOrWhiteSpace(query)) return new List<WingetRow>();

        ProcessResult r = await ProcessRunner.RunAsync(ExePath!, $"search \"{query}\" {CommonArgs}", ct,
            timeout: TimeSpan.FromMinutes(2)).ConfigureAwait(false);

        if (!r.Success)
        {
            LastError = $"winget search: {DescribeExitCode(r.ExitCode)}";
            ActivityLog.Warn($"{LastError} {Trim(r.StdErr)}");
        }

        return WingetText.ParseTable(r.StdOut);
    }

    /// <summary>
    /// winget's "the installed version could not be determined" failure. Retrying the same
    /// upgrade with --include-unknown gets past it — WinGet-GUI relies on this fallback.
    /// </summary>
    private const int VersionUndetermined = unchecked((int)0x8A15002B);

    /// <summary>Installs or upgrades a package silently, reporting progress as winget emits it.</summary>
    public async Task<ProcessResult> ExecuteAsync(
        RunAction action,
        string packageId,
        string source,
        InstallScope? installedScope,
        Action<string, int?> onProgress,
        bool includeUnknown = false,
        CancellationToken ct = default)
    {
        if (ExePath is null)
            return new ProcessResult(-1, "", "winget is not available on this PC.");

        string verb = action switch
        {
            RunAction.Update => "upgrade",
            RunAction.Uninstall => "uninstall",
            _ => "install"
        };
        // --accept-package-agreements is an install/upgrade option only; uninstall rejects it
        // with 0x8A150002 and dumps its own help text, which is how a failed uninstall once
        // looked like it had run.
        string args =
            $"{verb} --id \"{packageId}\" --exact --silent " +
            "--accept-source-agreements --disable-interactivity" +
            (action == RunAction.Uninstall ? string.Empty : " --accept-package-agreements") +
            (action == RunAction.Update && includeUnknown ? " --include-unknown" : "");

        if (!string.IsNullOrWhiteSpace(source))
            args += $" --source {source}";

        // ---------------------------------------------------------------------------------
        // Pin the install scope. Do not remove this.
        //
        // PC Medic runs elevated, so winget picks the machine-wide installer by default.
        // Run that against an app that is installed per-user and it is not an upgrade at
        // all: it installs a *second*, machine-wide copy under Program Files, and the
        // per-user copy under %LOCALAPPDATA% is left orphaned or removed. Every shortcut
        // the user already has still points into %LOCALAPPDATA%, so they lose their icon
        // and stop launching.
        //
        // Pinning --scope to whatever is already installed is what keeps an upgrade an
        // upgrade. If no installer exists at that scope, winget fails with 0x8A150011 and
        // we report it honestly - a refused update is always better than a moved app.
        // ---------------------------------------------------------------------------------
        if (action == RunAction.Update)
        {
            string? scopeFlag = InstallScopePolicy.ScopeFlag(installedScope, source);
            if (scopeFlag is not null)
            {
                args += $" --scope {scopeFlag}";
            }
            else if (installedScope is null &&
                     !string.Equals(source, "msstore", StringComparison.OrdinalIgnoreCase))
            {
                ActivityLog.Warn(
                    $"{packageId}: the installed scope could not be determined, so the upgrade runs " +
                    "unscoped. If this app turns out to be a per-user install, winget may relocate it.");
            }
        }

        ActivityLog.Info($"winget {args}");

        using var downloadWatch = new WingetDownloadMonitor(onProgress);

        // No cancellation token, and NeverKill: once an installer is running it is allowed
        // to finish. Stopping a run prevents the next package from starting; it must never
        // interrupt the one in flight.
        var result = await ProcessRunner.RunAsync(
            ExePath!, args, CancellationToken.None,
            onOutputLine: line =>
            {
                int? pct = WingetText.ParsePercent(line);
                if (pct is not null)
                {
                    onProgress(line, pct);
                }
                else
                {
                    // winget goes quiet during the download itself; the monitor reports
                    // real megabytes from the installer file growing in temp.
                    downloadWatch.HandleLine(line);
                }
            },
            timeout: TimeSpan.FromMinutes(30),
            abortPolicy: ProcessAbortPolicy.NeverKill).ConfigureAwait(false);

        // winget cannot always tell what version is installed; --include-unknown is the
        // documented way past that failure, so retry once with it before giving up.
        if (action == RunAction.Update && result.ExitCode == VersionUndetermined && !includeUnknown)
        {
            ActivityLog.Info($"{packageId}: installed version unreadable — retrying with --include-unknown.");
            onProgress("Retrying with --include-unknown…", null);
            return await ExecuteAsync(action, packageId, source, installedScope, onProgress,
                                      includeUnknown: true, ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Describes the failure winget reported, including the refusals the scope pin causes on
    /// purpose, so the user is told why an app was left alone.
    /// </summary>
    public static string DescribeExitCode(int code, InstallScope? scope) =>
        InstallScopePolicy.DescribeScopeRefusal(code, scope) ?? DescribeExitCode(code);

    /// <summary>Maps winget's documented exit codes onto something a human can act on.</summary>
    public static string DescribeExitCode(int code) => unchecked((uint)code) switch
    {
        0 => "Completed successfully.",
        0x8A150011 => "No applicable installer found for this system.",
        0x8A150014 => "Another installation is already in progress.",
        0x8A150056 => "The package is already installed and up to date.",
        0x8A15002B => "No package matched that identifier.",
        0x8A150006 => "The installer failed to run.",
        0x8A150109 => "The install needs a restart to finish.",
        // winget's own wording is "Files modified by the installer are currently in use":
        // the application was running and the installer refused rather than replacing files
        // underneath it. Common, and useless as a raw hex code.
        0x8A150111 => "The app is open, and its files are in use. Close it and try again.",
        0x80070005 => "Access denied - administrator rights are required.",
        0x80070652 => "Another Windows Installer operation is in progress.",
        _ => code == 0 ? "Completed successfully." : $"Installer returned exit code 0x{code:X8}."
    };

    /// <summary>An installer that asks for a restart has still done its job.</summary>
    public static bool NeedsRestart(int code) => unchecked((uint)code) == 0x8A150109;

    /// <summary>
    /// winget's "there was nothing to do" result: exit code 43, or the wording itself when the
    /// code varies between winget versions. The upgrade table is a snapshot — by the time a row
    /// is acted on the package may already be current, or a side-by-side install (a second
    /// VCRedist, another Unity version) can leave winget unable to pick which copy to move.
    /// That is a stale listing, not a broken installer, and must not read as a failure.
    /// </summary>
    public static bool IsNoUpgradeAvailable(ProcessResult result)
    {
        if (result.ExitCode == 43) return true;

        var text = result.Combined;
        return text.Contains("No available upgrade found", StringComparison.OrdinalIgnoreCase)
            || text.Contains("No newer package versions", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The last thing winget actually said, minus its boilerplate, so a failure reads
    /// "0x80190194 : Not found (404)" instead of only a hex exit code. Null when the
    /// output says nothing beyond the usual preamble.
    /// </summary>
    public static string? OutputSummary(ProcessResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Combined)) return null;

        string[] boilerplate =
        {
            "This application is licensed to you by its owner.",
            "Microsoft is not responsible for, nor does it grant any licenses to, third-party packages.",
            "Successfully verified installer hash",
            "Starting package install",
            "Successfully installed",
        };

        var lines = result.Combined.Replace("\r\n", "\n").Split('\n')
            .Select(WingetText.Clean)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !l.StartsWith("Downloading http", StringComparison.OrdinalIgnoreCase))
            .Where(l => !boilerplate.Any(l.Contains))
            .ToList();

        if (lines.Count == 0) return null;

        // The reason sits at the end; keep up to three lines so a wrapped HRESULT survives.
        var tail = lines.Skip(Math.Max(0, lines.Count - 3)).ToList();
        var summary = string.Join(" ", tail).Trim();
        return summary.Length <= 220 ? summary : summary[..220] + "…";
    }

    private static string Trim(string s) =>
        s.Length <= 300 ? s.Trim() : s[..300].Trim() + "…";
}
