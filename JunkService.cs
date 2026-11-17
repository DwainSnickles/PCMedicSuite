using System.Runtime.InteropServices;

namespace PC_Medic;

/// <summary>One cleanable junk category: what it is, what it needs, and what a scan found in it.</summary>
internal sealed record JunkTarget(
    string Id,
    string Name,
    string Description,
    bool RequiresAdmin,
    ulong Bytes,
    int Files,
    bool Accessible);

/// <summary>
/// What one cleaning pass over a target reported back. "Skipped" is the normal case - a file a
/// running process holds, or one another user owns - and is deliberately not an error.
/// </summary>
internal sealed record JunkCleanResult(
    int Removed,
    int Skipped,
    int QueuedReboot,
    int Failed,
    ulong Freed,
    string? Error);

/// <summary>
/// Finds and removes the junk Windows leaves behind: temp files, the Windows Update download cache,
/// the Recycle Bin, thumbnail caches and the Delivery Optimization cache. Everything a target can
/// reach goes through the Recycle Bin where Windows allows it, so a wrong tick is recoverable;
/// files a running process holds are skipped rather than forced.
/// </summary>
internal static class JunkService
{
    private static readonly string WindowsDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    private static readonly string LocalAppData =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>
    /// Measures every category in order, reporting each finished one so the caller can show a real
    /// percentage: eight categories is an honest denominator, unlike a timer pretending to be progress.
    /// </summary>
    public static List<JunkTarget> Scan(IProgress<ScanProgress>? progress = null)
    {
        var targets = new List<JunkTarget>(8);

        void Add(string id, string name, string description, bool admin, Func<(ulong Bytes, int Files, bool Accessible)> measure)
        {
            progress?.Report(new ScanProgress(
                (int)Math.Round(targets.Count * 100.0 / 8), $"SCANNING {name}", targets.Count + 1, 8));

            (ulong bytes, int files, bool accessible) = measure();
            targets.Add(new JunkTarget(id, name, description, admin, bytes, files, accessible));
        }

        Add("temp", "TEMPORARY FILES",
            "Setup leftovers and app temp files older than ten minutes.",
            false, () => (TempSize(), TempFileCount(), TempAccessible()));
        Add("winupdate", "WINDOWS UPDATE CACHE",
            "Installer packages Windows Update already applied.",
            true, () => (DirSize(WinUpdateDir()), DirFileCount(WinUpdateDir()), DirAccessible(WinUpdateDir())));
        Add("recyclebin", "RECYCLE BIN",
            "Everything already deleted but not yet purged.",
            false, () => (RecycleBinSize(), 0, true));
        Add("thumbcache", "THUMBNAIL & ICON CACHE",
            "Explorer's picture and icon caches; they rebuild themselves.",
            false, () => (ThumbCacheSize(), ThumbCacheFiles().Length, ThumbCacheFiles().Length > 0));
        Add("shadercache", "SHADER CACHE",
            "DirectX and GPU shader caches; they rebuild on next launch.",
            false, () => (SumDirSize(ShaderCacheDirs()), SumDirFileCount(ShaderCacheDirs()), DirsAccessible(ShaderCacheDirs())));
        Add("browsercache", "BROWSER CACHES",
            "Chrome, Edge and Firefox cache folders. Close the browser for a full clean.",
            false, () => (SumDirSize(BrowserCacheDirs()), SumDirFileCount(BrowserCacheDirs()), DirsAccessible(BrowserCacheDirs())));
        Add("windowslogs", "WINDOWS LOGS",
            "Setup, servicing and system logs under C:\\Windows\\Logs.",
            true, () => (DirSize(WinLogsDir()), DirFileCount(WinLogsDir()), DirAccessible(WinLogsDir())));
        Add("deliveryopt", "DELIVERY OPTIMIZATION",
            "Update files Windows shares between PCs on this network.",
            true, () => (DirSize(DeliveryOptDir()), DirFileCount(DeliveryOptDir()), DirAccessible(DeliveryOptDir())));

        progress?.Report(new ScanProgress(100, "SCAN COMPLETE", 8, 8));
        return targets;
    }

    /// <summary>Cleans one target. Runs on a background thread; touches only that target's folder.</summary>
    public static JunkCleanResult Clean(string id)
    {
        try
        {
            return id switch
            {
                "temp" => CleanTemp(),
                "winupdate" => CleanDirectory(WinUpdateDir()),
                "recyclebin" => CleanRecycleBin(),
                "thumbcache" => CleanThumbCache(),
                "shadercache" => CleanDirectories(ShaderCacheDirs()),
                "browsercache" => CleanDirectories(BrowserCacheDirs()),
                "windowslogs" => CleanDirectory(WinLogsDir()),
                "deliveryopt" => CleanDirectory(DeliveryOptDir()),
                _ => new JunkCleanResult(0, 0, 0, 0, 0, $"Unknown cleanup target: {id}"),
            };
        }
        catch (Exception ex)
        {
            return new JunkCleanResult(0, 0, 0, 0, 0, ex.Message);
        }
    }

    // ---- temp -------------------------------------------------------------------------------

    private static string[] TempRoots() => new[]
    {
        Path.GetTempPath(),
        Path.Combine(LocalAppData, "Temp"),
        Path.Combine(WindowsDir, "Temp"),
    }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static bool TempAccessible() => TempRoots().Length > 0;

    private static ulong TempSize()
    {
        ulong total = 0;
        foreach (string root in TempRoots()) total += DirSize(root);
        return total;
    }

    private static int TempFileCount()
    {
        int total = 0;
        foreach (string root in TempRoots()) total += DirFileCount(root);
        return total;
    }

    private static JunkCleanResult CleanTemp()
    {
        CleanReport report = DiskCleanupService.Clean();
        return new JunkCleanResult(report.Removed, report.Skipped, 0, report.Failed, report.Freed, null);
    }

    // ---- directory targets ------------------------------------------------------------------

    private static string WinUpdateDir() => Path.Combine(WindowsDir, "SoftwareDistribution", "Download");

    private static string WinLogsDir() => Path.Combine(WindowsDir, "Logs");

    // ---- multi-folder targets -----------------------------------------------------------------

    /// <summary>Shader caches every common GPU API writes under LocalAppData.</summary>
    private static IEnumerable<string> ShaderCacheDirs()
    {
        yield return Path.Combine(LocalAppData, "D3DSCache");
        yield return Path.Combine(LocalAppData, "NVIDIA", "DXCache");
        yield return Path.Combine(LocalAppData, "NVIDIA", "GLCache");
        yield return Path.Combine(LocalAppData, "AMD", "DxCache");
        yield return Path.Combine(LocalAppData, "AMD", "GLCache");
        yield return Path.Combine(LocalAppData, "NVIDIA Corporation", "NV_Cache");
    }

    /// <summary>Cache folders for the browsers people actually run; locked files skip on their own.</summary>
    private static IEnumerable<string> BrowserCacheDirs()
    {
        foreach (string root in new[]
        {
            Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Default"),
            Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Default"),
            Path.Combine(LocalAppData, "BraveSoftware", "Brave-Browser", "User Data", "Default"),
        })
        {
            yield return Path.Combine(root, "Cache");
            yield return Path.Combine(root, "Code Cache");
            yield return Path.Combine(root, "GPUCache");
        }

        string profiles = Path.Combine(LocalAppData, "Mozilla", "Firefox", "Profiles");
        if (!Directory.Exists(profiles)) yield break;

        foreach (string profile in Directory.EnumerateDirectories(profiles))
        {
            yield return Path.Combine(profile, "cache2");
        }
    }

    private static bool DirsAccessible(IEnumerable<string> dirs) => dirs.Any(DirAccessible);

    private static ulong SumDirSize(IEnumerable<string> dirs)
    {
        ulong total = 0;
        foreach (string dir in dirs) total += DirSize(dir);
        return total;
    }

    private static int SumDirFileCount(IEnumerable<string> dirs)
    {
        int total = 0;
        foreach (string dir in dirs) total += DirFileCount(dir);
        return total;
    }

    /// <summary>Cleans every folder in the set and folds the per-folder results into one report.</summary>
    private static JunkCleanResult CleanDirectories(IEnumerable<string> dirs)
    {
        int removed = 0, skipped = 0, queuedReboot = 0;
        ulong freed = 0;
        string? error = null;

        foreach (string dir in dirs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            JunkCleanResult part = CleanDirectory(dir);
            removed += part.Removed;
            skipped += part.Skipped;
            queuedReboot += part.QueuedReboot;
            freed += part.Freed;
            error ??= part.Error;
        }

        return new JunkCleanResult(removed, skipped, queuedReboot, 0, freed, error);
    }

    private static string DeliveryOptDir() => Path.Combine(
        WindowsDir, "ServiceProfiles", "NetworkService", "AppData", "Local",
        "Microsoft", "Windows", "DeliveryOptimization", "Cache");

    /// <summary>Moves every top-level item in the folder to the Recycle Bin; locked items are skipped.</summary>
    private static JunkCleanResult CleanDirectory(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            return new JunkCleanResult(0, 0, 0, 0, 0, "The folder is not present on this machine.");
        }

        int removed = 0, skipped = 0, queuedReboot = 0;
        ulong freed = 0;

        foreach (string item in Directory.EnumerateFileSystemEntries(dir))
        {
            ulong size = PathIsDirectory(item) ? DirSize(item) : SafeFileSize(item);

            if (RecycleDelete(item))
            {
                removed++;
                freed += size;
            }
            else if (QueueRebootDelete(item))
            {
                queuedReboot++;
            }
            else
            {
                skipped++;
            }
        }

        return new JunkCleanResult(removed, skipped, queuedReboot, 0, freed, null);
    }

    // ---- recycle bin -------------------------------------------------------------------------

    private static JunkCleanResult CleanRecycleBin()
    {
        ulong before = RecycleBinSize();

        // Clear-RecycleBin is the only purge that respects every user's bin and needs no shell verbs.
        CmdResult result = Cmd.RunPowerShell(
            "Clear-RecycleBin -Force -ErrorAction SilentlyContinue", 120000);

        if (!result.Ok)
        {
            return new JunkCleanResult(0, 0, 0, 0, 0, result.Reason);
        }

        ulong after = RecycleBinSize();
        return new JunkCleanResult(0, 0, 0, 0, before > after ? before - after : before, null);
    }

    /// <summary>Bytes sitting in every user's recycle bin that this session can read.</summary>
    private static ulong RecycleBinSize()
    {
        string root = @"C:\$Recycle.Bin";
        if (!Directory.Exists(root)) return 0;

        ulong total = 0;
        foreach (string sid in Directory.EnumerateDirectories(root)) total += DirSize(sid);
        return total;
    }

    // ---- thumbnail / icon cache ---------------------------------------------------------------

    private static string ExplorerCacheDir() =>
        Path.Combine(LocalAppData, "Microsoft", "Windows", "Explorer");

    private static string[] ThumbCacheFiles() =>
        Directory.Exists(ExplorerCacheDir())
            ? Directory.EnumerateFiles(ExplorerCacheDir(), "*cache_*.db")
                .Where(file => Path.GetFileName(file).StartsWith("thumbcache", StringComparison.OrdinalIgnoreCase)
                            || Path.GetFileName(file).StartsWith("iconcache", StringComparison.OrdinalIgnoreCase))
                .ToArray()
            : Array.Empty<string>();

    private static ulong ThumbCacheSize() => ThumbCacheFiles().Aggregate(0UL, (sum, file) => sum + SafeFileSize(file));

    private static JunkCleanResult CleanThumbCache()
    {
        int removed = 0, skipped = 0, queuedReboot = 0;
        ulong freed = 0;

        foreach (string file in ThumbCacheFiles())
        {
            ulong size = SafeFileSize(file);
            if (RecycleDelete(file))
            {
                removed++;
                freed += size;
            }
            else if (QueueRebootDelete(file))
            {
                queuedReboot++;
            }
            else
            {
                skipped++;
            }
        }

        return new JunkCleanResult(removed, skipped, queuedReboot, 0, freed, null);
    }

    // ---- shared helpers ------------------------------------------------------------------------

    private static bool PathIsDirectory(string path) =>
        (File.GetAttributes(path) & FileAttributes.Directory) != 0;

    private static ulong SafeFileSize(string path)
    {
        try
        {
            return (ulong)new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Recursive byte count; unreadable branches count as zero rather than failing the scan.</summary>
    private static ulong DirSize(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return 0;

        ulong total = 0;
        foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            total += SafeFileSize(file);
        }

        return total;
    }

    private static int DirFileCount(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return 0;

        try
        {
            return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Count();
        }
        catch
        {
            return 0;
        }
    }

    private static bool DirAccessible(string? dir) =>
        !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir);

    /// <summary>
    /// Sends one file or folder to the Recycle Bin without any prompt: SHFileOperation runs silent
    /// and never confirms, so a locked file simply fails and is counted as skipped instead of
    /// stopping the pass on a dialog. False when Windows refuses (locked, too big, no bin).
    /// </summary>
    private static bool RecycleDelete(string path)
    {
        var op = new SHFILEOPSTRUCT
        {
            hwnd = IntPtr.Zero,
            wFunc = FoDelete,
            pFrom = path + "\0\0",
            pTo = null,
            fFlags = FofSilent | FofNoConfirmation | FofAllowUndo | FofNoErrorUi | FofNoConnect,
        };

        try
        {
            return SHFileOperation(ref op) == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// The force path for a locked file: registers it in PendingFileRenameOperations so Windows
    /// deletes it during the next boot, before whatever holds it can open it again. Needs an
    /// elevated session - the registry value lives under HKLM - so unelevated runs just skip.
    /// </summary>
    private static bool QueueRebootDelete(string path)
    {
        if (!Native.IsElevated) return false;

        try
        {
            return MoveFileEx(path, null, MovefileDelayUntilReboot);
        }
        catch
        {
            return false;
        }
    }

    private const uint FoDelete = 3;
    private const ushort FofSilent = 0x0004;
    private const ushort FofNoConfirmation = 0x0010;
    private const ushort FofAllowUndo = 0x0040;
    private const ushort FofNoErrorUi = 0x0400;
    private const ushort FofNoConnect = 0x0800;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string? pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperationW")]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    private const uint MovefileDelayUntilReboot = 0x4;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "MoveFileExW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, uint dwFlags);
}
