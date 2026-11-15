// ---------------------------------------------------------------------------------------------
// Ported from AppGeek (https://github.com/techygeekshome/AppGeek), Copyright (C) 2026
// TechyGeeksHome, and adapted for PC Medic.
//
// AppGeek is free software under the GNU General Public License v3.0, and PC Medic is too from
// this port onwards. See LICENSE.
// ---------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace PC_Medic;

/// <summary>
/// Works out whether an app is currently running, so an upgrade can say so instead of failing.
///
/// An open app is worth catching before the install starts: winget only discovers it after the
/// download and staging finished, and for an MSIX package it reports success even though the
/// deployment failed with 0x80073D02 ("the following apps need to be closed"). The row then
/// disappears from nothing and comes straight back on the next scan.
/// </summary>
internal static class RunningProcessDetector
{
    private static readonly Dictionary<string, string[]> KnownProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Google.Chrome"] = new[] { "chrome" },
        ["Mozilla.Firefox"] = new[] { "firefox" },
        ["Microsoft.Edge"] = new[] { "msedge" },
        ["Brave.Brave"] = new[] { "brave" },
        ["Adobe.Acrobat.Reader.64-bit"] = new[] { "AcroRd32", "Acrobat" },
        ["Notepad++.Notepad++"] = new[] { "notepad++" },
        ["VideoLAN.VLC"] = new[] { "vlc" },
        ["Zoom.Zoom"] = new[] { "Zoom" },
        ["SlackTechnologies.Slack"] = new[] { "slack" },
        ["Microsoft.VisualStudioCode"] = new[] { "Code" },
        ["Valve.Steam"] = new[] { "steam" },
        ["Discord.Discord"] = new[] { "Discord" },
        ["Spotify.Spotify"] = new[] { "Spotify" },
        ["7zip.7zip"] = new[] { "7zFM" },
        ["OpenAI.ChatGPT"] = new[] { "ChatGPT", "ChatGPT Classic" },

        // Executable names rarely resemble display names, which is why this table exists at
        // all: "OBS Studio" runs as "obs64", and the generic fallback below has no chance of
        // guessing that.
        ["OBSProject.OBSStudio"] = new[] { "obs64", "obs32", "obs" },
        ["HandBrake.HandBrake"] = new[] { "HandBrake" },
        ["GitHub.GitHubDesktop"] = new[] { "GitHubDesktop" },
        ["Microsoft.Teams"] = new[] { "ms-teams", "Teams" },
        ["Microsoft.Outlook"] = new[] { "olk", "OUTLOOK" },
        ["Telegram.TelegramDesktop"] = new[] { "Telegram" },
        ["Mozilla.Thunderbird"] = new[] { "thunderbird" },
        ["Audacity.Audacity"] = new[] { "audacity" },
        ["GIMP.GIMP"] = new[] { "gimp-2.10", "gimp" },
        ["BlenderFoundation.Blender"] = new[] { "blender" },
        ["qBittorrent.qBittorrent"] = new[] { "qbittorrent" },
        ["Dropbox.Dropbox"] = new[] { "Dropbox" },
        ["Docker.DockerDesktop"] = new[] { "Docker Desktop" },
        ["Postman.Postman"] = new[] { "Postman" },
        ["Obsidian.Obsidian"] = new[] { "Obsidian" },
        ["Plex.Plex"] = new[] { "Plex" },
        ["Plex.PlexMediaServer"] = new[] { "Plex Media Server" },
        ["64Gram.64Gram"] = new[] { "64Gram", "Telegram" },
        ["OpenWhisperSystems.Signal"] = new[] { "Signal" },
        ["JRSoftware.InnoSetup.7"] = new[] { "Compil32", "ISCC" },
        ["Rufus.Rufus"] = new[] { "rufus" }
    };

    /// <summary>Returns the process name if a matching process is running, else null.</summary>
    public static string? FindRunning(string? packageId, string? displayName)
    {
        if (!string.IsNullOrWhiteSpace(packageId) &&
            KnownProcesses.TryGetValue(packageId!, out var known))
        {
            foreach (var candidate in known)
            {
                try
                {
                    if (Process.GetProcessesByName(candidate).Length > 0) return candidate + ".exe";
                }
                catch
                {
                    // Access denied on some processes is fine; keep looking.
                }
            }
        }

        return FindByName(displayName);
    }

    /// <summary>
    /// Last resort for an app that is not in the table above. This looks for a running process
    /// whose name, once a trailing architecture digit is stripped, is a prefix of the display name
    /// with its spaces removed.
    ///
    /// It is allowed to be a little generous: a false positive costs the user one "close it and try
    /// again" message, while a false negative costs them a failed upgrade.
    /// </summary>
    private static string? FindByName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return null;

        var collapsed = new string(displayName!.Where(char.IsLetterOrDigit).ToArray());
        if (collapsed.Length < 4) return null;

        Process[] running;
        try { running = Process.GetProcesses(); }
        catch { return null; }

        try
        {
            foreach (var process in running)
            {
                string name;
                try { name = process.ProcessName; }
                catch { continue; }

                var stem = name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                if (stem.Length < 3) continue;

                var stemCollapsed = new string(stem.Where(char.IsLetterOrDigit).ToArray());
                if (stemCollapsed.Length < 3) continue;

                if (collapsed.StartsWith(stemCollapsed, StringComparison.OrdinalIgnoreCase))
                    return name + ".exe";
            }
        }
        finally
        {
            foreach (var process in running)
            {
                try { process.Dispose(); } catch { }
            }
        }

        return null;
    }
}
