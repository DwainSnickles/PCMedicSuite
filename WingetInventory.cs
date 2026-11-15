// ---------------------------------------------------------------------------------------------
// Ported from AppGeek (https://github.com/techygeekshome/AppGeek), Copyright (C) 2026
// TechyGeeksHome, and adapted for PC Medic.
//
// AppGeek is free software under the GNU General Public License v3.0, and PC Medic is too from
// this port onwards. See LICENSE.
// ---------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Win32;

namespace PC_Medic;

/// <summary>One application discovered on this PC, with the scope it was installed at.</summary>
internal sealed class InstalledProgram
{
    public string DisplayName { get; set; } = "";
    public string? DisplayVersion { get; set; }
    public string? Publisher { get; set; }
    public DateTime? InstallDate { get; set; }
    public long EstimatedSizeBytes { get; set; }
    public string? InstallLocation { get; set; }
    public string? UninstallString { get; set; }
    public string? QuietUninstallString { get; set; }
    public string? RegistryKey { get; set; }
    public InstallScope Scope { get; set; } = InstallScope.Machine;

    /// <summary>Package identifier from a source (e.g. "Google.Chrome"), when matched.</summary>
    public string? PackageId { get; set; }
    public string? SourceName { get; set; }

    public bool IsTracked => !string.IsNullOrWhiteSpace(PackageId);

    /// <summary>Stable key used for exclusions and de-duplication.</summary>
    public string Key => !string.IsNullOrWhiteSpace(PackageId)
        ? "id:" + PackageId!.ToLowerInvariant()
        : "name:" + DisplayName.ToLowerInvariant();

    public override string ToString() => $"{DisplayName} {DisplayVersion}";
}

/// <summary>
/// Tidying for the strings read out of Windows' uninstall registry keys.
///
/// Kept separate from the inventory reader, which depends on Microsoft.Win32.Registry and
/// therefore cannot be exercised off Windows. The parsing decision is pure, so it lives here.
/// </summary>
internal static class RegistryText
{
    /// <summary>
    /// Cleans a value written into the registry by an installer.
    ///
    /// Installers write these by hand and plenty get it wrong. Stray wrapping quotes are the
    /// most common fault, which is how an application ends up displaying a publisher of
    /// <c>"Anthropic</c> rather than <c>Anthropic</c>.
    ///
    /// Whitespace is trimmed, a quote pair wrapping the whole value is removed, and a single
    /// unbalanced quote at either end is dropped. A value that legitimately contains balanced
    /// quotes is left alone - <c>Bob's Software</c> and <c>The "Fast" Installer</c> both
    /// survive intact.
    /// </summary>
    public static string? Clean(string? raw)
    {
        if (raw is null) return null;

        var v = raw.Trim();
        if (v.Length == 0) return null;

        // A matched pair wrapping the entire value. Loop, because doubled wrapping happens.
        while (v.Length >= 2 &&
               ((v[0] == '"' && v[^1] == '"') || (v[0] == '\'' && v[^1] == '\'')))
        {
            v = v[1..^1].Trim();
            if (v.Length == 0) return null;
        }

        // A lone unbalanced quote at one end - the case actually seen in the wild.
        if ((v[0] == '"' || v[0] == '\'') && Count(v, v[0]) == 1)
        {
            v = v[1..].Trim();
            if (v.Length == 0) return null;
        }

        if ((v[^1] == '"' || v[^1] == '\'') && Count(v, v[^1]) == 1)
        {
            v = v[..^1].Trim();
            if (v.Length == 0) return null;
        }

        return v.Length == 0 ? null : v;
    }

    private static int Count(string s, char c)
    {
        int n = 0;
        foreach (var ch in s) if (ch == c) n++;
        return n;
    }
}

/// <summary>
/// Reads the Add/Remove Programs data straight out of the uninstall keys.
///
/// Deliberately does NOT use WMI's Win32_Product: querying that class triggers an
/// MSI consistency check (self-repair) on every installed product, which is slow
/// and can silently reconfigure the user's software. The registry is the correct
/// source and is what Windows itself displays in Apps and Features.
///
/// The scope recorded here is what the upgrade is pinned to, so it is the most important
/// thing this scan produces.
/// </summary>
internal sealed class RegistryInventoryService
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    public Task<List<InstalledProgram>> ScanAsync(CancellationToken ct = default) =>
        Task.Run(() => Scan(ct), ct);

    public List<InstalledProgram> Scan(CancellationToken ct = default)
    {
        var results = new List<InstalledProgram>();

        var sources = new (RegistryHive Hive, RegistryView View, InstallScope Scope)[]
        {
            (RegistryHive.LocalMachine, RegistryView.Registry64, InstallScope.Machine),
            (RegistryHive.LocalMachine, RegistryView.Registry32, InstallScope.Machine),
            (RegistryHive.CurrentUser,  RegistryView.Registry64, InstallScope.User),
            (RegistryHive.CurrentUser,  RegistryView.Registry32, InstallScope.User),
        };

        foreach (var (hive, view, scope) in sources)
            Read(hive, view, scope, results, ct);

        // The same product can appear under both registry views; keep one of each.
        return results
            .GroupBy(a => (a.DisplayName.ToLowerInvariant(), a.DisplayVersion ?? "", a.Scope))
            .Select(g => g.First())
            .OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void Read(RegistryHive hive, RegistryView view, InstallScope scope,
                             List<InstalledProgram> into, CancellationToken ct)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstall = baseKey.OpenSubKey(UninstallPath);
            if (uninstall is null) return;

            foreach (var subName in uninstall.GetSubKeyNames())
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    using var key = uninstall.OpenSubKey(subName);
                    if (key is null) continue;

                    InstalledProgram? app = FromKey(key, subName, scope, hive, view);
                    if (app is not null) into.Add(app);
                }
                catch (Exception ex)
                {
                    ActivityLog.Warn($"Skipped uninstall key {subName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"Could not read {hive}\\{UninstallPath} ({view}): {ex.Message}");
        }
    }

    private static InstalledProgram? FromKey(RegistryKey key, string subName, InstallScope scope,
                                             RegistryHive hive, RegistryView view)
    {
        var name = key.GetValue("DisplayName") as string;
        if (string.IsNullOrWhiteSpace(name)) return null;

        // Hidden plumbing, and Windows/driver updates, are not "applications".
        if (ToInt(key.GetValue("SystemComponent")) == 1) return null;
        var releaseType = key.GetValue("ReleaseType") as string;
        if (!string.IsNullOrWhiteSpace(releaseType) &&
            releaseType.Contains("update", StringComparison.OrdinalIgnoreCase)) return null;
        if (key.GetValue("ParentKeyName") is string p && !string.IsNullOrWhiteSpace(p)) return null;

        var uninstallString = key.GetValue("UninstallString") as string;
        var quiet = key.GetValue("QuietUninstallString") as string;
        if (string.IsNullOrWhiteSpace(uninstallString) && string.IsNullOrWhiteSpace(quiet)) return null;

        return new InstalledProgram
        {
            DisplayName = name!.Trim(),
            DisplayVersion = RegistryText.Clean(key.GetValue("DisplayVersion") as string),
            Publisher = RegistryText.Clean(key.GetValue("Publisher") as string),
            InstallDate = ParseInstallDate(key.GetValue("InstallDate") as string),
            // EstimatedSize is stored in KB.
            EstimatedSizeBytes = ToInt(key.GetValue("EstimatedSize")) * 1024L,
            InstallLocation = RegistryText.Clean(key.GetValue("InstallLocation") as string),
            UninstallString = uninstallString,
            QuietUninstallString = quiet,
            RegistryKey = $"{hive}\\{UninstallPath}\\{subName} ({view})",
            Scope = scope
        };
    }

    private static int ToInt(object? o)
    {
        if (o is null) return 0;
        try { return Convert.ToInt32(o, CultureInfo.InvariantCulture); }
        catch { return 0; }
    }

    private static DateTime? ParseInstallDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        // Almost always yyyyMMdd.
        if (DateTime.TryParseExact(raw.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d)) return d;
        return DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out var d2) ? d2 : null;
    }
}
