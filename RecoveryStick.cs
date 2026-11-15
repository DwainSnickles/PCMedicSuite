using System.Diagnostics;

namespace PC_Medic;

/// <summary>
/// The Windows half of a recovery stick: a stick made by Windows' own recovery drive tool boots into
/// Windows Recovery, where the standard repairs live. PC Medic cannot build that environment, so it
/// reports whether the chosen stick has one and opens the tool that makes it - one stick then carries
/// both the Windows repairs and PC Medic's image restore.
/// </summary>
internal static class RecoveryStick
{
    // The wording below is shared with the manual, so the wizard's verdict and the help topic cannot
    // describe different things.
    public const string PresentVerdict = "WINDOWS REPAIR PRESENT";
    public const string MissingVerdict = "NO WINDOWS REPAIR ON THIS STICK";
    public const string StandardRepairs =
        "Startup Repair, System Restore, Command Prompt and Reset this PC (recover OS)";

    /// <summary>
    /// True when the stick carries a Windows boot image - a recovery drive or Windows media, both of
    /// which offer "Repair your computer". Without one the stick holds only PC Medic's pointer.
    /// </summary>
    public static bool HasWindowsRepair(string root)
    {
        try
        {
            return File.Exists(Path.Combine(root, "sources", "boot.wim")) ||
                   File.Exists(Path.Combine(root, "Recovery", "WindowsRE", "winre.wim"));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Runs Windows' recovery drive tool to completion. Returns null when it closed normally, or the
    /// reason it could not be started, for the caller to show.
    /// </summary>
    public static async Task<string?> OpenBuilderAsync()
    {
        string tool = Path.Combine(Environment.SystemDirectory, "RecoveryDrive.exe");

        try
        {
            if (!File.Exists(tool)) return "This build of Windows does not include the recovery drive tool.";

            using Process? process = Process.Start(new ProcessStartInfo(tool) { UseShellExecute = true });
            if (process is null) return "The recovery drive tool did not start.";

            await process.WaitForExitAsync();
            Log.Info("Windows recovery drive tool closed.");
            return null;
        }
        catch (Exception ex)
        {
            Log.Warn("Recovery drive tool: " + ex.Message);
            return ex.Message;
        }
    }
}
