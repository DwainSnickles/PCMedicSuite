namespace PC_Medic;

/// <summary>
/// Reads winget's own diagnostic log for the invocation that just failed.
///
/// The CLI hands back an exit code and little else, but the reason is always in the log it writes
/// under LocalState\DiagOutputDir: which deployment call returned 0x80073D02, which download came
/// back 404, which installer Windows refused to launch. Without this the page can only say "installer
/// returned exit code 0x8A150006", which tells nobody anything they can act on.
/// </summary>
internal static class WingetDiagnostics
{
    private static string LogFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        @"Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\DiagOutputDir");

    /// <summary>
    /// A sentence describing why the run failed, or null when the log says nothing useful.
    /// <paramref name="since"/> scopes the search to the run that just happened.
    /// </summary>
    public static string? Explain(string packageId, DateTime since)
    {
        List<string> errors = ErrorLines(packageId, since);
        if (errors.Count == 0) return null;

        foreach (string line in errors)
        {
            // The application is open, so its package cannot be replaced in place. winget finds this
            // out only after the download and staging, which is why it looked like a success.
            if (line.Contains("0x80073D02", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("need to be closed", StringComparison.OrdinalIgnoreCase))
                return "the app is running, so its files are in use. Close it and try again.";

            // The manifest points at a URL the publisher no longer serves. Nothing local can fix it.
            if (line.Contains("Returned status: 404", StringComparison.OrdinalIgnoreCase))
                return "the publisher's download link in winget's manifest returns 404, so there is nothing to install.";

            if (line.Contains("Returned status: 403", StringComparison.OrdinalIgnoreCase))
                return "the publisher's download link refuses the request (403).";

            if (line.Contains("ShellExecute installer failed", StringComparison.OrdinalIgnoreCase))
                return "the downloaded installer could not be started by Windows.";

            if (line.Contains("0x80070005", StringComparison.OrdinalIgnoreCase))
                return "access denied - the upgrade needs administrator rights.";

            if (line.Contains("0x8A150014", StringComparison.OrdinalIgnoreCase))
                return "another installation is already in progress.";
        }

        return Theme.Truncate(errors[^1], 160);
    }

    private static List<string> ErrorLines(string packageId, DateTime since)
    {
        var found = new List<string>();

        try
        {
            if (!Directory.Exists(LogFolder)) return found;

            string needle = $"--id \"{packageId}\"";

            foreach (FileInfo file in new DirectoryInfo(LogFolder)
                         .GetFiles("WinGet-*.log")
                         .OrderByDescending(candidate => candidate.LastWriteTimeUtc))
            {
                // Logs older than the run cannot describe it; stop once they are clearly past it.
                if (file.LastWriteTimeUtc < since.AddMinutes(-3)) break;

                string text;
                try { text = File.ReadAllText(file.FullName); }
                catch { continue; }

                if (!text.Contains(needle, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (string line in text.Split('\n'))
                {
                    if (!line.Contains("<E>", StringComparison.Ordinal)) continue;
                    found.Add(Strip(line));
                }

                // The newest matching log belongs to this run, so nothing older can add to it.
                break;
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Warn($"winget's log could not be read: {ex.Message}");
        }

        return found;
    }

    /// <summary>Turns "2026-11-10 08:04:18.399 &lt;E&gt; [CLI ] message" into "message".</summary>
    private static string Strip(string raw)
    {
        int at = raw.IndexOf("<E>", StringComparison.Ordinal);
        string rest = at < 0 ? raw.Trim() : raw[(at + 3)..].Trim();

        if (rest.StartsWith('['))
        {
            int close = rest.IndexOf(']');
            if (close > 0) rest = rest[(close + 1)..].Trim();
        }

        return rest;
    }
}
