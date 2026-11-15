// ---------------------------------------------------------------------------------------------
// Ported from AppGeek (https://github.com/techygeekshome/AppGeek), Copyright (C) 2026
// TechyGeeksHome, and adapted for PC Medic.
//
// AppGeek is free software under the GNU General Public License v3.0, and PC Medic is too from
// this port onwards. See LICENSE.
// ---------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace PC_Medic;

/// <summary>
/// Loose version comparison. Real-world DisplayVersion values are messy
/// ("8u421", "25.001.20458", "1.2.3-beta"), so this normalises to a numeric
/// tuple and compares component by component.
/// </summary>
internal static partial class VersionCompare
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex Numbers();

    public static int Compare(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b)) return 0;
        if (string.IsNullOrWhiteSpace(a)) return -1;
        if (string.IsNullOrWhiteSpace(b)) return 1;

        if (string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase)) return 0;

        var na = Parts(a);
        var nb = Parts(b);
        int len = Math.Max(na.Count, nb.Count);
        for (int i = 0; i < len; i++)
        {
            long va = i < na.Count ? na[i] : 0;
            long vb = i < nb.Count ? nb[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }

        // Numerically identical ("1.2" vs "1.2.0", "3.1" vs "3.1-beta"): treat as equal.
        // Falling back to a string comparison here would invent phantom updates.
        return 0;
    }

    public static bool IsNewer(string? candidate, string? current) => Compare(candidate, current) > 0;

    /// <summary>
    /// True when winget has not actually told us a version. `winget list` prints things like
    /// "Unknown" and "&gt; 3.13.5" (meaning "newer than anything I know about"), neither of which
    /// can be compared with anything.
    /// </summary>
    public static bool IsIndefinite(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return true;
        var v = version.Trim();
        return v.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
               v.StartsWith('>') || v.StartsWith('<');
    }

    /// <summary>
    /// True when two version strings plausibly describe the same installed release.
    ///
    /// Exact equality is too strict to use as corroboration for a name match. winget reports
    /// "10.0.11" for a runtime the registry calls "10.0.11.50000", and "3.14.7" for a Python
    /// the registry calls "3.14.7150.0". Both are the same product, and exact matching threw
    /// both away.
    ///
    /// Comparing only major and minor keeps the useful signal - a genuinely different release
    /// such as 1.51 against 1.52 is still rejected - without demanding that two sources agree
    /// on a build number they format differently.
    /// </summary>
    public static bool ProbablySameRelease(string? a, string? b)
    {
        if (IsIndefinite(a) || IsIndefinite(b)) return true;

        var na = Parts(a);
        var nb = Parts(b);
        if (na.Count == 0 || nb.Count == 0) return true;

        // Only one component to go on: it has to match exactly.
        if (na.Count == 1 || nb.Count == 1) return na[0] == nb[0];

        return na[0] == nb[0] && na[1] == nb[1];
    }

    /// <summary>True when the leading component differs, e.g. 24.09 -> 25.01.</summary>
    public static bool IsMajorChange(string? from, string? to)
    {
        var a = Parts(from);
        var b = Parts(to);
        if (a.Count == 0 || b.Count == 0) return false;
        return a[0] != b[0];
    }

    private static List<long> Parts(string? s)
    {
        var list = new List<long>();
        if (string.IsNullOrWhiteSpace(s)) return list;
        foreach (Match m in Numbers().Matches(s))
        {
            if (long.TryParse(m.Value, out var v)) list.Add(v);
            if (list.Count >= 6) break;
        }
        return list;
    }
}

/// <summary>
/// Decides which winget package, if any, an installed application corresponds to.
///
/// Deliberately kept free of any Windows-only dependency so it can be exercised directly by
/// tests - this is the piece where a wrong answer damages someone's machine.
/// </summary>
internal static class PackageMatcher
{
    /// <summary>
    /// Attaches winget package IDs to registry entries.
    ///
    /// This is the most dangerous piece of matching in the app: whatever ID ends up on an
    /// InstalledProgram is what gets handed to "winget upgrade --id". Bind the wrong ID and the
    /// app cheerfully installs unrelated software over something that was working.
    ///
    /// So the rules are deliberately strict, and an unmatched app is a far better outcome
    /// than a wrongly matched one - it just means PC Medic does not offer to update it.
    /// </summary>
    public static void MatchPackageIds(List<InstalledProgram> installed, List<WingetRow> listed)
    {
        if (listed.Count == 0) return;

        // A name that appears more than once cannot identify anything. Drop those outright
        // rather than letting the last row silently win.
        var byName = listed
            .Where(r => !string.IsNullOrWhiteSpace(r.Get(WingetColumn.Name)))
            .GroupBy(r => r.Get(WingetColumn.Name), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single(), StringComparer.OrdinalIgnoreCase);

        foreach (var app in installed)
        {
            if (app.PackageId is not null) continue;

            var row = FindRow(app, byName);
            if (row is null) continue;

            var id = row.Get(WingetColumn.Id);

            if (string.IsNullOrWhiteSpace(id) ||
                id.StartsWith("MSIX", StringComparison.OrdinalIgnoreCase))
                continue;

            // winget truncates over-long values with an ellipsis. A truncated ID is not an
            // ID - using it would either fail or, worse, resolve to something else.
            if (ContainsEllipsis(id))
            {
                ActivityLog.Warn($"Ignoring truncated package ID '{id}' for '{app.DisplayName}'.");
                continue;
            }

            app.PackageId = id;

            // Only record a source winget actually named. Defaulting to "winget" made every
            // matched app claim a source it might not have, which is misleading in the
            // installed list and when an upgrade is pinned to a scope.
            var src = row.Get(WingetColumn.Source);
            app.SourceName = string.IsNullOrWhiteSpace(src) ? null : src;
        }
    }

    private static WingetRow? FindRow(InstalledProgram app, Dictionary<string, WingetRow> byName)
    {
        // 1. An exact name match is the only match we fully trust.
        if (byName.TryGetValue(app.DisplayName, out var exact))
            return VersionAgrees(app, exact, strict: false) ? exact : null;

        // 2. Otherwise the only acceptable case is a name winget itself truncated, where the
        //    surviving prefix is long enough to be meaningful. "Plex" must never match
        //    "Plex Media Server", and "Git" must never match "GitHub Desktop".
        const int minimumPrefix = 12;

        var candidates = byName.Keys
            .Where(ContainsEllipsis)
            .Select(k => (Key: k, Prefix: TrimEllipsis(k)))
            .Where(x => x.Prefix.Length >= minimumPrefix &&
                        app.DisplayName.StartsWith(x.Prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count != 1) return null;

        // A truncated prefix is a weaker signal than an exact name, so the version has to
        // agree properly rather than merely being the same release line.
        var row = byName[candidates[0].Key];
        return VersionAgrees(app, row, strict: true) ? row : null;
    }

    /// <summary>
    /// winget reads its "installed version" from the same Add/Remove Programs entry we do,
    /// so when both sides report a version they must agree. A disagreement means the row is
    /// describing a different product, and the match is thrown away.
    /// </summary>
    private static bool VersionAgrees(InstalledProgram app, WingetRow row, bool strict)
    {
        var theirs = row.Get(WingetColumn.Version);
        var ours = app.DisplayVersion;

        if (string.IsNullOrWhiteSpace(theirs) || string.IsNullOrWhiteSpace(ours)) return true;
        if (ContainsEllipsis(theirs)) return true;

        // "Unknown", and winget's "> 3.13.5" form, tell us nothing to compare against.
        if (VersionCompare.IsIndefinite(theirs)) return true;

        if (strict
            ? VersionCompare.Compare(theirs, ours) == 0
            : VersionCompare.ProbablySameRelease(theirs, ours))
            return true;

        return false;
    }

    public static bool ContainsEllipsis(string s) => s.Contains('\u2026') || s.Contains("...");

    private static string TrimEllipsis(string s) =>
        s.Replace("\u2026", "").Replace("...", "").TrimEnd();
}
