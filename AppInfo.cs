using System.Diagnostics;
using System.Reflection;

namespace PC_Medic;

internal sealed record Credit(string Name, string Licence, string Url);

/// <summary>
/// One place for the app's identity. The About window and the update checker both
/// read from here, so they can never disagree about what shipped.
/// </summary>
internal static class AppInfo
{
    public const string Name = "PC Medic";
    public const string Tagline = "Portable maintenance suite with a built-in software updater";
    public const string Publisher = "PC Medic";

    public const string Description =
        "Scan everything installed on this PC, see what is out of date, and update it in one " +
        "run. Browse a catalogue of common Windows software and install it silently — no " +
        "hunting for download links, no next-next-finish.";

    public const string LicenceLine =
        "Free to use, including at work. No paid tier, no subscription, no upsell. Nothing " +
        "about your machine is uploaded anywhere. If it saved you time, a donation is welcome " +
        "but never expected.";

    public const string LicenceName = "GNU General Public License v3.0";
    public const string LicenceUrl = "https://www.gnu.org/licenses/gpl-3.0.html";

    /// <summary>
    /// The GPL asks that a program with a graphical interface show this notice
    /// somewhere prominent — the About box is the conventional place.
    /// </summary>
    public const string LicenceNotice =
        "PC Medic is free software: you can redistribute it and modify it under the terms of " +
        "the GNU General Public License version 3, as published by the Free Software " +
        "Foundation. It comes with ABSOLUTELY NO WARRANTY.";

    public const string GitHubOwner = "DwainSnickles";
    public const string GitHubRepo = "PCMedicSuite";

    public const string WebsiteUrl = "https://github.com/DwainSnickles/PCMedicSuite";
    public const string ProductUrl = "https://github.com/DwainSnickles/PCMedicSuite/releases";
    public const string SourceUrl = "https://github.com/DwainSnickles/PCMedicSuite";
    public const string IssuesUrl = "https://github.com/DwainSnickles/PCMedicSuite/issues";

    public static readonly Credit Licence = new(LicenceName, "GPL-3.0", LicenceUrl);

    /// <summary>Read from the assembly, so it always matches the binary that shipped.</summary>
    public static string CurrentVersionText =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public static string VersionLine => $"Version {CurrentVersionText}  ·  {Publisher}";

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not open '{url}': {ex.Message}");
        }
    }
}
