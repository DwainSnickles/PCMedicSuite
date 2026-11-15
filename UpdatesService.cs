using System.Text.Json;
using System.Runtime.InteropServices;

namespace PC_Medic;

/// <summary>One package winget can move to a newer version.</summary>
internal sealed record AppUpgrade(
    string Name,
    string Id,
    string Current,
    string Available,
    string Source,
    InstallScope? Scope = null,
    string? Location = null,
    bool ScopeAmbiguous = false)
{
    public string VersionLine => $"{Current} \u2192 {Available}";

    /// <summary>Where the app lives now, which is what an upgrade is pinned to.</summary>
    public string ScopeLine => ScopeAmbiguous
    ? "multiple installations detected"
    : Scope switch
    {
        InstallScope.User => "per-user",
        InstallScope.Machine => "machine-wide",
        InstallScope.Store => "Store",
        _ => "scope unknown",
    };
}

/// <summary>A driver update offered by the Windows Update agent.</summary>
internal sealed record DriverUpdate(string Title, string Manufacturer, DateTime? Released, long SizeBytes, object Payload)
{
    public string DetailLine
    {
        get
        {
            string date = Released is { } released ? released.ToString("yyyy-MM-dd") : "unknown date";
            ulong size = SizeBytes > 0 ? (ulong)SizeBytes : 0;
            return $"{Theme.Truncate(Manufacturer, 24)} \u2022 {date} \u2022 {Theme.Bytes(size)}";
        }
    }
}

/// <summary>
/// Two read/write surfaces for keeping a machine current, using what Windows already ships:
/// winget for applications and the Windows Update agent (COM) for drivers. Nothing is
/// downloaded from a third-party server and no package is added to the project.
/// </summary>
internal static class UpdatesService
{
    public const string WingetDownloadPage = "https://aka.ms/getwinget";

    private const int OperationSucceeded = 2;

    /// <summary>An operation that stops moving for this long is given up on, so a wedged agent cannot hold the queue.</summary>
    private static readonly TimeSpan StallLimit = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions IgnoreJson = new() { WriteIndented = true };

    /// <summary>
    /// Packages the user has told PC Medic to stop offering. An app that cannot be upgraded or removed
    /// comes back on every scan, so the id is remembered and filtered out of the list instead.
    /// </summary>
    private static readonly HashSet<string> Ignored = LoadIgnored();

    /// <summary>Where hidden package ids are persisted, exposed so the UI can open the file for review.</summary>
    public static string IgnoreFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PC Medic",
        "updates-ignored.json");

    private static string IgnoreFile => IgnoreFilePath;

    /// <summary>True when this package is hidden from every scan.</summary>
    public static bool IsIgnored(string id)
    {
        lock (Ignored) return Ignored.Contains(id);
    }

    /// <summary>A snapshot of every hidden package id, for the management dialog.</summary>
    public static IReadOnlyList<string> IgnoredIds
    {
        get { lock (Ignored) return Ignored.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(); }
    }

    /// <summary>Hides a package from future scans and remembers the decision across restarts.</summary>
    public static bool Ignore(string id)
    {
        lock (Ignored)
        {
            if (!Ignored.Add(id)) return true;
            return SaveIgnored();
        }
    }

    /// <summary>Lets a hidden package be offered by scans again and forgets the decision.</summary>
    public static bool Unignore(string id)
    {
        lock (Ignored)
        {
            if (!Ignored.Remove(id)) return true;
            return SaveIgnored();
        }
    }

    private static bool SaveIgnored()
    {
        try
        {
            string? folder = Path.GetDirectoryName(IgnoreFile);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            string temp = IgnoreFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Ignored.ToArray(), IgnoreJson));
            File.Move(temp, IgnoreFile, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"Hidden updates could not be saved: {ex.Message}");
            return false;
        }
    }

    private static HashSet<string> LoadIgnored()
    {
        var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!File.Exists(IgnoreFile)) return ignored;

            string[]? ids = JsonSerializer.Deserialize<string[]>(File.ReadAllText(IgnoreFile));
            if (ids is null) return ignored;

            foreach (string id in ids) ignored.Add(id);
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"Hidden updates unreadable: {ex.Message}");
        }

        return ignored;
    }

    /// <summary>winget.exe from the WindowsApps alias, or the first match on PATH.</summary>
    public static string? WingetPath
    {
        get
        {
            string alias = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\WindowsApps\winget.exe");

            try
            {
                if (File.Exists(alias)) return alias;
            }
            catch
            {
                // Unreadable profile path; fall through to PATH.
            }

            string? path = Environment.GetEnvironmentVariable("PATH");
            if (path is null) return null;

            foreach (string folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(folder.Trim(), "winget.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch
                {
                    // Malformed PATH entry; keep looking.
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Reads what can be upgraded, and how each of those apps is installed.
    ///
    /// winget's own table says nothing about install scope, and an elevated upgrade that is not pinned
    /// to the existing scope does not upgrade a per-user app - it installs a second, machine-wide copy
    /// and strands the shortcuts pointing into the profile. So Add or Remove Programs is read first,
    /// its entries are matched to winget's package IDs, and that scope travels with every row.
    /// </summary>
    public static async Task<(List<AppUpgrade> Apps, string? Error)> ScanAppsAsync(CancellationToken token = default)
    {
        ActivityLog.Warn("=== APP UPDATE SCAN STARTED ===");

        var winget = new WingetClient();
        if (!winget.IsAvailable)
        {
            ActivityLog.Warn("APP SCAN FAILED: winget is not installed.");
            return (new List<AppUpgrade>(), "winget is not installed.");
        }

        ActivityLog.Warn("APP SCAN: Querying Winget upgrade list.");

        List<WingetRow> rows = await winget.UpgradesAsync(includeUnknown: false, ct: token).ConfigureAwait(false);

        ActivityLog.Warn($"APP SCAN: Winget returned {rows.Count} upgrade candidates.");

        var installed = new List<InstalledProgram>();

        if (rows.Count > 0)
        {
            ActivityLog.Warn("APP SCAN: Reading installed applications from registry.");

            installed = await new RegistryInventoryService().ScanAsync(token).ConfigureAwait(false);

            ActivityLog.Warn($"APP SCAN: Registry returned {installed.Count} installations.");

            List<WingetRow> listed = await winget.ListAsync(ct: token).ConfigureAwait(false);

            ActivityLog.Warn($"APP SCAN: Winget list returned {listed.Count} installed packages.");

            PackageMatcher.MatchPackageIds(installed, listed);

            int matched = installed.Count(p => !string.IsNullOrWhiteSpace(p.PackageId));

            ActivityLog.Warn(
                $"APP SCAN: Package matching completed. {matched} registry installations matched Winget IDs.");
        }

        var apps = new List<AppUpgrade>();

        foreach (WingetRow row in rows)
        {
            string id = row.Get(WingetColumn.Id);
            string name = row.Get(WingetColumn.Name);
            string current = row.Get(WingetColumn.Version);
            string available = row.Get(WingetColumn.Available);
            string source = row.Get(WingetColumn.Source);

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(available)) continue;

            // winget prints "Unknown" when it cannot read what is installed, which makes the package look
            // out of date no matter how many times it is upgraded. Such a package is left out entirely.
            if (VersionCompare.IsIndefinite(current) || VersionCompare.IsIndefinite(available)) continue;

            // A truncated ID is not an ID. Never hand one to "winget upgrade".
            if (PackageMatcher.ContainsEllipsis(id))
            {
                ActivityLog.Warn($"Skipping '{name}': winget truncated its package ID to '{id}'.");
                continue;
            }

            // Hidden packages are dropped here, so the page, the counts and the nav badge all agree.
            if (IsIgnored(id)) continue;

            var matches = installed.Where(program =>
    string.Equals(program.PackageId, id, StringComparison.OrdinalIgnoreCase))
    .ToList();

            bool scopeAmbiguous = matches.Count > 1;

            InstalledProgram? match = matches.Count == 1
                ? matches[0]
                : null;
            ActivityLog.Warn(
                $"APP MATCH: {name} | ID: {id} | " +
                $"Matching installations: {matches.Count} | " +
                $"Ambiguous: {scopeAmbiguous}");

            if (scopeAmbiguous)
            {
                foreach (var duplicate in matches)
                {
                    ActivityLog.Warn(
                        $"DUPLICATE INSTALLATION: {name} | " +
                        $"Scope: {duplicate.Scope} | " +
                        $"Location: {duplicate.InstallLocation}");
                }

                ActivityLog.Warn(
                    $"UPGRADE PROTECTION: {name} blocked because multiple installations were detected.");
            }
            apps.Add(new AppUpgrade(
                string.IsNullOrWhiteSpace(name) ? id : name,
                id,
                current,
                available,
                source,
                match?.Scope,
                match?.InstallLocation,
                scopeAmbiguous));
        }

        ActivityLog.Warn(
    $"=== APP UPDATE SCAN FINISHED === " +
    $"Upgrade candidates: {rows.Count} | " +
    $"Displayed updates: {apps.Count} | " +
    $"Ambiguous updates: {apps.Count(a => a.ScopeAmbiguous)}");

        return apps.Count > 0
            ? (apps, null)
            : (apps, winget.LastError);
    }

    /// <summary>
    /// Streams winget's upgrade so the page can fill that row's own bar from the transfer it is
    /// actually doing. The upgrade is pinned to the scope the app is installed at, and the installer
    /// is never killed part-way through: stopping the pass stops the next package, not this one.
    /// </summary>
    public static async Task<(bool Ok, string Message)> UpgradeAsync(
        AppUpgrade app,
        IProgress<int>? percent = null,
        CancellationToken token = default)
    {
        var winget = new WingetClient();
        if (!winget.IsAvailable) return (false, "winget is not installed.");

        if (app.ScopeAmbiguous)
        {
            return (false,
                $"{app.Name}: multiple installations were detected. " +
                "Automatic upgrade blocked to prevent modifying the wrong installation.");
        }

        // An open app cannot have its files replaced. winget only finds that out after downloading and
        // staging - and for an MSIX package it still reports success while nothing was deployed - so ask
        // the question before spending the download.
        string? open = RunningProcessDetector.FindRunning(app.Id, app.Name);
        if (open is not null)
        {
            return (false, $"{app.Name}: {open} is running. Close it and try again.");
        }

        DateTime started = DateTime.UtcNow;

        ProcessResult run = await winget.ExecuteAsync(
            RunAction.Update,
            app.Id,
            app.Source,
            app.Scope,
            (_, value) =>
            {
                // winget prints 100% before the install phase, so the bar stops short until it exits.
                if (value is { } reported) percent?.Report(Math.Min(reported, 97));
            },
            ct: token).ConfigureAwait(false);

        // The upgrade table is a snapshot: the package may already be current, or a
        // side-by-side install can leave winget unable to pick a copy. Exit 43 /
        // "No available upgrade found" means there was nothing to do — report success
        // so the row verifies away instead of offering to uninstall a working app.
        if (WingetClient.IsNoUpgradeAvailable(run))
            return (true, $"{app.Name} is already up to date.");

        if (run.ExitCode == 0) return (true, $"{app.Name} upgraded to {app.Available}.");

        // An installer that asks for a restart has still done its job.
        if (WingetClient.NeedsRestart(run.ExitCode))
            return (true, $"{app.Name} upgraded to {app.Available} \u2014 restart to finish.");

        // winget's own log knows why: a 404 in the manifest, an installer Windows would not launch,
        // a package that could not be registered. The exit code alone names none of them.
        string reason = WingetDiagnostics.Explain(app.Id, started)
                        ?? WingetClient.DescribeExitCode(run.ExitCode, app.Scope);
        string? said = WingetClient.OutputSummary(run);
        if (said is not null) reason += $" — {said}";

        return (false, $"{app.Name}: {reason}");
    }

    /// <summary>Removes a package for good, for apps that stopped being wanted rather than out of date.</summary>
    public static (bool Ok, string Message) Uninstall(AppUpgrade app)
    {
        string? winget = WingetPath;
        if (winget is null) return (false, "winget is not installed.");

        CmdResult result = Cmd.Run(
            winget,
            $"uninstall --id \"{app.Id}\" --exact --silent " +
            "--accept-source-agreements --disable-interactivity",
            600000);

        return result.Ok
            ? (true, $"{app.Name} uninstalled.")
            : (false, $"{app.Name}: {result.Reason}");
    }

    /// <summary>Reads the driver updates the Windows Update agent is offering. Read-only.</summary>
    public static (List<DriverUpdate> Drivers, string? Error) ScanDrivers()
    {
        var drivers = new List<DriverUpdate>();

        try
        {
            Type? sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
            if (sessionType is null) return (drivers, "The Windows Update agent is unavailable.");

            dynamic session = Activator.CreateInstance(sessionType)!;
            dynamic searcher = session.CreateUpdateSearcher();
            searcher.Online = true;
            searcher.SearchScope = 1;      // sosMachine
            searcher.ServerSelection = 3;  // ssOthers — includes third-party driver offers

            dynamic result = searcher.Search("IsInstalled=0 and Type='Driver' and IsHidden=0");
            int count = (int)result.Updates.Count;

            for (int index = 0; index < count; index++)
            {
                try
                {
                    dynamic update = result.Updates.Item(index);

                    string title = (string)update.Title;
                    string manufacturer = "Unknown";
                    DateTime? released = null;
                    long size = 0;

                    try
                    {
                        string? name = (string?)update.DriverManufacturer;
                        if (!string.IsNullOrWhiteSpace(name)) manufacturer = name;
                    }
                    catch
                    {
                        // Some updates carry no manufacturer property.
                    }

                    try
                    {
                        released = (DateTime)update.DriverVerDate;
                    }
                    catch
                    {
                        // Optional metadata; leave the date blank.
                    }

                    try
                    {
                        size = Convert.ToInt64(update.MaxDownloadSize);
                    }
                    catch
                    {
                        // Size is cosmetic.
                    }

                    drivers.Add(new DriverUpdate(title, manufacturer, released, size, (object)update));
                }
                catch
                {
                    // Skip updates whose metadata cannot be read at all.
                }
            }

            return (drivers.OrderBy(driver => driver.Title, StringComparer.OrdinalIgnoreCase).ToList(), null);
        }
        catch (Exception ex)
        {
            return (drivers, ex.Message);
        }
    }

    /// <summary>
    /// Downloads and installs the given drivers one at a time rather than as one batch: a batch hands the
    /// whole transfer to Windows Update and cannot be stopped. The agent's asynchronous calls replace its
    /// blocking ones, so a row moves with the bytes that are actually arriving, CANCEL reaches the transfer
    /// in flight instead of waiting for it, and the outcome is read per update rather than guessed.
    /// </summary>
    public static (bool Ok, string Message) Install(
        IReadOnlyList<DriverUpdate> drivers,
        CancellationToken token = default,
        IProgress<int>? percent = null)
    {
        if (drivers.Count == 0) return (false, "Nothing to install.");
        if (!Native.IsElevated) return (false, "Driver installs need an elevated run \u2014 use RESTART AS ADMIN.");

        try
        {
            Type? sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
            Type? collectionType = Type.GetTypeFromProgID("Microsoft.Update.UpdateColl");
            if (sessionType is null || collectionType is null) return (false, "The Windows Update agent is unavailable.");

            object session = Activator.CreateInstance(sessionType)!;

            int installed = 0;
            bool reboot = false;
            string? failure = null;

            for (int index = 0; index < drivers.Count; index++)
            {
                token.ThrowIfCancellationRequested();

                // Each driver owns an equal slice of the row, so a queue of them still fills once.
                int span = 100 / drivers.Count;
                int from = index * span;
                int to = index == drivers.Count - 1 ? 100 : from + span;

                (bool ok, bool needsReboot, string? why) =
                    InstallOne(session, collectionType, drivers[index], percent, from, to, token);

                reboot |= needsReboot;

                if (ok) installed++;
                else failure ??= why;
            }

            if (installed == 0 && failure is not null) return (false, failure);

            string message = $"{installed} driver update(s) installed" + (reboot ? " \u2014 reboot required." : ".");
            if (failure is not null) message += " " + failure;

            return (failure is null, message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// One driver's transfer and installation, reported as a slice of its row: the download owns the first
    /// four fifths and the installation the rest, so the number never walks backwards mid-way.
    /// </summary>
    private static (bool Ok, bool Reboot, string? Why) InstallOne(
        object session,
        Type collectionType,
        DriverUpdate driver,
        IProgress<int>? percent,
        int from,
        int to,
        CancellationToken token)
    {
        dynamic update = driver.Payload;
        object collection = Activator.CreateInstance(collectionType)!;
        dynamic bag = collection;

        try
        {
            if (!(bool)update.EulaAccepted) update.AcceptEula();
        }
        catch
        {
            // Not every driver update exposes a EULA flag.
        }

        bag.Add(update);

        int midpoint = from + ((to - from) * 4 / 5);

        // The agent reports through a callback object, not an event; the sinks below satisfy that and the
        // percentages are polled from the job, which keeps everything on this one thread.
        object progressSink = new WuProgressSink();
        object completedSink = new WuCompletedSink();

        dynamic agent = session;
        dynamic downloader = agent.CreateUpdateDownloader();
        downloader.Updates = bag;

        object downloadJob;

        try
        {
            downloadJob = downloader.BeginDownload(progressSink, completedSink, null);
        }
        catch (Exception ex)
        {
            // An agent that will not take an automation sink can still be used: the blocking call moves the
            // same bytes, only without a percentage, so a machine that refuses the callback still updates.
            ActivityLog.Warn($"{driver.Title}: no transfer progress ({ex.Message}); downloading in one step.");

            object moved = downloader.Download();
            (int fallbackCode, int fallbackHResult) = ReadResult(moved);

            if (fallbackCode != OperationSucceeded)
            {
                return (false, false, Fault(driver.Title, "download", fallbackCode, fallbackHResult));
            }

            percent?.Report(midpoint);
            return InstallPhase(session, collection, driver, percent, midpoint, to, token, progressSink, completedSink);
        }
        JobEnd downloadEnd = Await(downloadJob, () => ReadPercent(downloadJob, download: true), percent, from, midpoint, token);

        if (downloadEnd != JobEnd.Completed)
        {
            Stop(downloader, downloadJob, true);

            if (downloadEnd == JobEnd.Aborted) throw new OperationCanceledException(token);

            return (false, false, $"{driver.Title}: the download stalled and was stopped.");
        }

        object download = downloader.EndDownload(downloadJob);
        CleanUp(downloadJob);

        (int code, int hresult) = ReadResult(download);
        if (code != OperationSucceeded) return (false, false, Fault(driver.Title, "download", code, hresult));

        token.ThrowIfCancellationRequested();

        return InstallPhase(session, collection, driver, percent, midpoint, to, token, progressSink, completedSink);
    }

    /// <summary>The installation half of a driver's row, asynchronous like the transfer that precedes it.</summary>
    private static (bool Ok, bool Reboot, string? Why) InstallPhase(
        object session,
        object collection,
        DriverUpdate driver,
        IProgress<int>? percent,
        int from,
        int to,
        CancellationToken token,
        object progressSink,
        object completedSink)
    {
        dynamic bag = collection;
        dynamic agent = session;
        dynamic installer = agent.CreateUpdateInstaller();
        installer.Updates = bag;

        object installJob;

        try
        {
            installJob = installer.BeginInstall(progressSink, completedSink, null);
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"{driver.Title}: no install progress ({ex.Message}); installing in one step.");

            object applied = installer.Install();
            (int fallbackCode, int fallbackHResult) = ReadResult(applied);

            if (fallbackCode != OperationSucceeded)
            {
                return (false, false, Fault(driver.Title, "install", fallbackCode, fallbackHResult));
            }

            percent?.Report(to);
            return (true, RebootWanted(applied), null);
        }

        JobEnd installEnd = Await(installJob, () => ReadPercent(installJob, download: false), percent, from, to, token);

        if (installEnd != JobEnd.Completed)
        {
            Stop(installer, installJob, false);

            if (installEnd == JobEnd.Aborted) throw new OperationCanceledException(token);

            return (false, false, $"{driver.Title}: the installation stalled and was stopped.");
        }

        object install = installer.EndInstall(installJob);
        CleanUp(installJob);

        (int code, int hresult) = ReadResult(install);
        if (code != OperationSucceeded) return (false, false, Fault(driver.Title, "install", code, hresult));

        percent?.Report(to);
        return (true, RebootWanted(install), null);
    }

    /// <summary>How an asynchronous operation ended, so the caller can end the job and report in one place.</summary>
    private enum JobEnd
    {
        Completed,
        Aborted,
        Stalled,
    }

    /// <summary>
    /// Follows a job, reporting its percentage, and gives up on an agent that stops moving. Cancelling is a
    /// request to the agent rather than a kill, so the job is left for the caller to end.
    /// </summary>
    private static JobEnd Await(object job, Func<int> measure, IProgress<int>? percent, int from, int to, CancellationToken token)
    {
        DateTime moved = DateTime.UtcNow;
        int last = -1;

        while (true)
        {
            if (token.IsCancellationRequested) return JobEnd.Aborted;

            int value = measure();
            if (value != last)
            {
                last = value;
                moved = DateTime.UtcNow;
                percent?.Report(from + ((to - from) * Math.Clamp(value, 0, 100) / 100));
            }

            if (Completed(job)) return JobEnd.Completed;
            if (DateTime.UtcNow - moved > StallLimit) return JobEnd.Stalled;

            Thread.Sleep(200);
        }
    }

    /// <summary>The job's own percentage: bytes moved for a download, updates applied for an installation.</summary>
    private static int ReadPercent(object job, bool download)
    {
        try
        {
            dynamic handle = job;
            dynamic state = handle.GetProgress();

            if (download)
            {
                try
                {
                    long total = Convert.ToInt64(state.TotalBytesToDownload);
                    long done = Convert.ToInt64(state.TotalBytesDownloaded);
                    if (total > 0) return (int)Math.Clamp(done * 100 / total, 0, 100);
                }
                catch
                {
                    // Fall through to the percentage the agent reports itself.
                }
            }

            return Convert.ToInt32(state.PercentComplete);
        }
        catch
        {
            return 0;
        }
    }

    private static bool Completed(object job)
    {
        try
        {
            dynamic handle = job;
            return (bool)handle.IsCompleted;
        }
        catch
        {
            // A job that will not answer is treated as finished; ending it surfaces the real fault.
            return true;
        }
    }

    /// <summary>Asks the agent to stop, waits for it to acknowledge, and only then ends the job.</summary>
    private static void Stop(dynamic agent, object job, bool download)
    {
        try
        {
            dynamic handle = job;
            handle.RequestAbort();
        }
        catch
        {
            // Nothing left to stop.
        }

        DateTime until = DateTime.UtcNow.AddSeconds(30);
        while (!Completed(job) && DateTime.UtcNow < until) Thread.Sleep(150);

        try
        {
            if (download) agent.EndDownload(job);
            else agent.EndInstall(job);
        }
        catch
        {
            // An aborted job often refuses to be ended; it is being discarded anyway.
        }

        CleanUp(job);
    }

    private static void CleanUp(object job)
    {
        try
        {
            dynamic handle = job;
            handle.CleanUp();
        }
        catch
        {
            // Only the asynchronous contract requires it, and older agents do not implement it.
        }
    }

    /// <summary>The operation's code and, where the agent gives one, the per-update HRESULT behind it.</summary>
    private static (int Code, int HResult) ReadResult(object result)
    {
        dynamic handle = result;
        int code = Convert.ToInt32(handle.ResultCode);
        int hresult = 0;

        try
        {
            dynamic first = handle.GetUpdateResult(0);
            hresult = Convert.ToInt32(first.HResult);
        }
        catch
        {
            // The collection-wide code still answers whether the update went on.
        }

        return (code, hresult);
    }

    private static bool RebootWanted(object result)
    {
        try
        {
            dynamic handle = result;
            return (bool)handle.RebootRequired;
        }
        catch
        {
            return false;
        }
    }

    private static string Fault(string title, string phase, int code, int hresult) =>
        hresult != 0
            ? $"{title}: {phase} failed with code {code} (0x{hresult:X8})."
            : $"{title}: {phase} failed with code {code}.";

}
/// <summary>
/// Windows Update announces progress by calling a dispatch identifier 0 method on an automation object,
/// so a sink is required even though the percentages are polled from the job instead. It has to be public
/// and COM-visible for the agent to reach it; nothing in it runs app code.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class WuProgressSink : IWuCallback
{
    public void Invoke(object job, object progress)
    {
    }
}

/// <summary>The completion half of the same contract: the job's own state is what the caller reads.</summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class WuCompletedSink : IWuCallback
{
    public void Invoke(object job, object state)
    {
    }
}

[ComVisible(true)]
[Guid("5f3a0c21-9b7e-4c2d-9e4a-8d1f6b2c7a10")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IWuCallback
{
    [DispId(0)]
    void Invoke(object job, object state);
}
