namespace PC_Medic;

/// <summary>
/// The two restarts that change how Windows starts: safe mode (minimal drivers and services) and the
/// repair environment (Advanced startup, where Startup Repair, System Restore and Reset this PC live).
///
/// Safe mode is sticky - bcdedit keeps the safeboot flag until something clears it - so the flag can
/// be read back and cleared from the same place that sets it. Nothing here restarts on its own: the
/// caller asks first, because unsaved work goes with it.
/// </summary>
internal static class BootModes
{
    /// <summary>The running install's boot entry; PC Medic is not a boot-manager editor.</summary>
    private const string Entry = "{current}";

    /// <summary>
    /// true when the next start is safe mode, false for a normal boot, null when the boot store
    /// cannot be read - which is what happens without administrator rights.
    /// </summary>
    public static bool? SafeModeArmed()
    {
        CmdResult result = Cmd.Run("bcdedit.exe", $"/enum {Entry}", 8000);
        if (!result.Ok) return null;

        return result.AllText.Contains("safeboot", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Sets safe mode for the next start. Does not restart.</summary>
    public static (bool Ok, string Message) ArmSafeMode()
    {
        CmdResult result = Cmd.Run("bcdedit.exe", $"/set {Entry} safeboot minimal", 8000);

        return result.Ok
            ? (true, "Safe mode is set for the next start")
            : (false, Reason(result, "bcdedit could not set safe mode"));
    }

    /// <summary>Clears the safeboot flag, so the following start is a normal one.</summary>
    public static (bool Ok, string Message) ClearSafeMode()
    {
        CmdResult result = Cmd.Run("bcdedit.exe", $"/deletevalue {Entry} safeboot", 8000);

        return result.Ok
            ? (true, "Safe mode cleared; the next start is a normal boot")
            : (false, Reason(result, "bcdedit could not clear safe mode"));
    }

    /// <summary>
    /// Restarts into Advanced startup (Windows Recovery): Startup Repair, System Restore, Reset this
    /// PC and Command Prompt. /o is the documented switch and needs the recovery environment enabled.
    /// </summary>
    public static (bool Ok, string Message) RestartIntoRepair() =>
        Restart("/r /o /t 10", "restarting into Windows Recovery");

    /// <summary>Plain restart, used to enter the safe mode that was just armed.</summary>
    public static (bool Ok, string Message) RestartNow() =>
        Restart("/r /t 10", "restarting");

    private static (bool Ok, string Message) Restart(string arguments, string what)
    {
        CmdResult result = Cmd.Run(
            "shutdown.exe",
            $"{arguments} /c \"PC Medic: {what}\"",
            8000);

        return result.Ok ? (true, what) : (false, Reason(result, "the restart was refused"));
    }

    /// <summary>The last meaningful line of the tool's output, which is where its complaint lives.</summary>
    private static string Reason(CmdResult result, string fallback)
    {
        string detail = result.AllText
            .Split('\n')
            .Select(line => line.Trim())
            .LastOrDefault(line => line.Length > 0) ?? string.Empty;

        return detail.Length == 0 ? fallback : $"{fallback}: {detail}";
    }
}
