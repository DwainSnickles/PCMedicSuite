using System.Reflection;
using System.Text;

namespace PC_Medic;

/// <summary>
/// The part of a recovery stick that works when Windows cannot boot: the wimlib binaries and the cmd
/// menu, stamped into PCMedicRecovery\ beside the pointer.
///
/// Windows RE has no .NET, so the menu is a batch file driving the same wimlib flags the page uses.
/// Nothing here can be an ordinary PC Medic component - it all has to run without Windows.
/// </summary>
internal static class RecoveryPayload
{
    public const string MenuFile = "restore.cmd";

    /// <summary>The engine and its DLL; the exe loads the DLL from beside itself, so both travel.</summary>
    private static readonly string[] Binaries = { "wimlib-imagex.exe", "libwim-15.dll" };

    /// <summary>
    /// Copies the engine and writes the menu into the stick's pointer folder. Returns null on
    /// success or the reason it failed, which the caller logs: the pointer still works without a
    /// menu, so a failure here must not fail the stamp.
    /// </summary>
    public static string? Install(string folder)
    {
        try
        {
            foreach (string name in Binaries)
            {
                string source = Path.Combine(AppContext.BaseDirectory, "bin", name);
                if (!File.Exists(source)) return $"the bundled {name} was not found in bin\\";

                File.Copy(source, Path.Combine(folder, name), overwrite: true);
            }

            File.WriteAllText(Path.Combine(folder, MenuFile), MenuText(), new UTF8Encoding(false));
            return null;
        }
        catch (Exception ex)
        {
            Log.Warn("Recovery menu could not be written: " + ex.Message);
            return ex.Message;
        }
    }

    /// <summary>
    /// The embedded menu, normalised to CRLF. cmd.exe mishandles a batch file with bare LF endings,
    /// and the file is edited on machines and in tools that write either.
    /// </summary>
    private static string MenuText()
    {
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("PC_Medic.Assets.recovery.restore.cmd")
            ?? throw new InvalidOperationException("restore.cmd is not embedded in the build.");

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string text = reader.ReadToEnd().Replace("\r\n", "\n").Replace('\r', '\n');
        return text.Replace("\n", "\r\n");
    }
}
