// ---------------------------------------------------------------------------------------------
// Adapted from Hexnest (https://github.com/ahmetcaglayan/Hexnest), Copyright (c) 2026
// Ahmet Çağlayan and Hexnest contributors, and adapted for PC Medic.
//
// Hexnest is MIT licensed, which is compatible with PC Medic's GPL-3.0. See LICENSE and the
// Licence section of README.md.
//
// Why this exists: Windows Update cannot install a driver it never published, and it cannot help at
// all on a machine whose network adapter is the device with no driver. A folder of .inf packages - a
// vendor's USB stick, a pnputil export, or a backup made here - is the only source that still works
// then. The packages are matched against the device tree so that only what this machine can use is
// offered, and pnputil, which ships with Windows, does the install.
// ---------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace PC_Medic;

/// <summary>One .inf package on disk that matches a device on this machine.</summary>
internal sealed record OfflineDriverPackage(
    string InfPath,
    string Provider,
    string ClassName,
    string Version,
    DateTime? Date,
    IReadOnlyList<string> HardwareIds,
    IReadOnlyList<string> Devices)
{
    public string FileName => Path.GetFileName(InfPath);

    public string Title
    {
        get
        {
            string who = Provider.Length > 0 ? Provider : "Driver package";
            string what = ClassName.Length > 0 ? $" {ClassName}" : string.Empty;
            string version = Version.Length > 0 ? $" {Version}" : string.Empty;
            return $"{who}{what}{version}".Trim();
        }
    }

    /// <summary>The line a download row shows: which devices the package was matched to, and the file.</summary>
    public string DetailLine
    {
        get
        {
            string date = Date is { } value
                ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "  \u2022  "
                : string.Empty;

            string devices = Devices.Count == 0
                ? "no matching device"
                : Devices.Count > 2
                    ? $"{string.Join(", ", Devices.Take(2))} and {Devices.Count - 2} more"
                    : string.Join(", ", Devices);

            return $"{date}{FileName}  \u2022  {devices}";
        }
    }
}

/// <summary>
/// Driver packages from a folder on disk, matched to this machine, installed with the in-box
/// <c>pnputil.exe</c>. No package, no service, no network.
/// </summary>
internal static class DriverRepository
{
    private static readonly TimeSpan AddTimeout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ScanTimeout = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Every .inf under <paramref name="folder"/> whose hardware ids match a device here. Packages
    /// that match nothing are left out: offering a driver for hardware this machine does not have is
    /// how a driver tool ends up installing something that breaks it.
    /// </summary>
    public static (List<OfflineDriverPackage> Packages, int InfCount, string? Error) ScanFolder(
        string folder,
        IReadOnlyList<DriverDevice> devices,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return (new List<OfflineDriverPackage>(), 0, "That folder could not be read.");
        }

        string[] files;

        try
        {
            files = Directory.GetFiles(folder, "*.inf", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            return (new List<OfflineDriverPackage>(), 0, $"The folder could not be listed: {ex.Message}");
        }

        var packages = new List<OfflineDriverPackage>();

        foreach (string file in files)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                OfflineDriverPackage? package = ReadPackage(file, devices);
                if (package is not null) packages.Add(package);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // One unreadable or malformed INF must not abandon the folder.
            }
        }

        return (packages
            .OrderBy(package => package.Provider, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(package => package.ClassName, StringComparer.CurrentCultureIgnoreCase)
            .ToList(), files.Length, null);
    }

    /// <summary>Adds one package to the driver store and binds it to its devices.</summary>
    public static (bool Ok, string Message) Install(
        OfflineDriverPackage package,
        IProgress<int>? progress = null,
        CancellationToken token = default)
    {
        if (!File.Exists(package.InfPath))
        {
            return (false, $"{package.FileName}: the package is no longer there.");
        }

        progress?.Report(10);

        (int code, string output) = Run(
            "pnputil.exe", new[] { "/add-driver", package.InfPath, "/install" }, AddTimeout, token);

        if (code is not (0 or 3010))
        {
            return (false, $"{package.Title}: {DescribeExitCode(code, output)}");
        }

        // Newly added packages are only bound after Windows re-enumerates; a failure here is harmless
        // because the next restart does the same work.
        progress?.Report(80);
        Run("pnputil.exe", new[] { "/scan-devices" }, ScanTimeout, token);

        progress?.Report(100);

        return (true, code == 3010
            ? $"{package.Title}: installed - a restart is required."
            : $"{package.Title}: installed.");
    }

    /// <summary>
    /// Exports every third-party package in the driver store to a folder: the pre-format backup, and
    /// the folder that later feeds <see cref="ScanFolder"/> on a machine with no network.
    /// </summary>
    public static (bool Ok, string Message) Export(
        string folder,
        IProgress<int>? progress = null,
        CancellationToken token = default)
    {
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            return (false, $"The backup folder could not be created: {ex.Message}");
        }

        progress?.Report(5);

        (int code, string output) = Run(
            "pnputil.exe", new[] { "/export-driver", "*", folder }, ExportTimeout, token);

        if (code != 0) return (false, $"Driver backup failed. {DescribeExitCode(code, output)}");

        progress?.Report(100);
        return (true, $"Driver backup written to {folder}.");
    }

    /// <summary>pnputil's exit codes, with its own words kept when they explain more.</summary>
    private static string DescribeExitCode(int code, string output)
    {
        string hint = code switch
        {
            5 => "Access denied - PC Medic needs to run as administrator.",
            87 => "pnputil rejected the command line.",
            259 => "No matching driver package was found.",
            3010 => "A restart is required to finish.",
            _ => $"pnputil exited with code {code}.",
        };

        string? detail = LastLine(output);
        return detail is null ? hint : $"{hint} {detail}";
    }

    private static string? LastLine(string output)
    {
        foreach (string line in output.Split('\n').Reverse())
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            return trimmed.Length > 160 ? trimmed[..160] : trimmed;
        }

        return null;
    }

    /// <summary>Runs pnputil and waits, killing the process tree if the user cancels.</summary>
    private static (int Code, string Output) Run(
        string exe,
        string[] arguments,
        TimeSpan timeout,
        CancellationToken token)
    {
        if (token.IsCancellationRequested) return (-1, "Cancelled.");

        var start = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (string argument in arguments) start.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = start };

        try
        {
            if (!process.Start()) return (-1, $"{exe} could not be started.");
        }
        catch (Exception ex)
        {
            return (-1, $"{exe} could not be started: {ex.Message}");
        }

        Task<string> output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        Task<string> errors = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            using CancellationTokenRegistration registration = token.Register(() =>
            {
                try
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // The process may have exited between the check and the kill.
                }
            });

            if (!process.WaitForExit(timeout))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Already gone.
                }

                return (-1, $"{exe} did not finish within {timeout.TotalMinutes:0} minutes.");
            }

            token.ThrowIfCancellationRequested();

            string text = $"{output.GetAwaiter().GetResult()}\n{errors.GetAwaiter().GetResult()}".Trim();
            return (process.ExitCode, text);
        }
        catch (OperationCanceledException)
        {
            return (-1, "Cancelled.");
        }
    }

    // ---- INF reading ------------------------------------------------------------------------

    private static OfflineDriverPackage? ReadPackage(string infPath, IReadOnlyList<DriverDevice> devices)
    {
        Dictionary<string, List<string>> sections = ParseSections(ReadText(infPath));

        List<string> ids = HardwareIds(sections);
        if (ids.Count == 0) return null;

        List<string> matched = MatchDevices(devices, ids);
        if (matched.Count == 0) return null;

        (string provider, string className, string version, DateTime? date) = ReadVersion(sections);

        return new OfflineDriverPackage(infPath, provider, className, version, date, ids, matched);
    }

    /// <summary>
    /// INFs are plain text in whatever encoding the vendor saved them in; a BOM is honoured and
    /// everything else is read as Latin-1, which never throws and keeps ASCII intact.
    /// </summary>
    private static string ReadText(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        return Encoding.Latin1.GetString(bytes);
    }

    /// <summary>Splits an INF into sections, joining backslash continuations and dropping comments.</summary>
    private static Dictionary<string, List<string>> ParseSections(string text)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        List<string>? current = null;
        string pending = string.Empty;

        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();

            int comment = line.IndexOf(';', StringComparison.Ordinal);
            if (comment >= 0) line = line[..comment].Trim();
            if (line.Length == 0) continue;

            if (pending.Length > 0)
            {
                line = pending + line;
                pending = string.Empty;
            }

            if (line[0] == '[')
            {
                int close = line.IndexOf(']');
                if (close < 2) continue;

                string name = line[1..close].Trim();
                if (!sections.TryGetValue(name, out current)) sections[name] = current = new List<string>();

                line = line[(close + 1)..].Trim();
                if (line.Length == 0) continue;
            }

            if (current is null) continue;

            if (line.EndsWith('\\'))
            {
                pending = line[..^1];
                continue;
            }

            current.Add(line);
        }

        return sections;
    }

    private static (string Provider, string ClassName, string Version, DateTime? Date) ReadVersion(
        Dictionary<string, List<string>> sections)
    {
        string provider = string.Empty;
        string className = string.Empty;
        string version = string.Empty;
        DateTime? date = null;

        if (!sections.TryGetValue("Version", out List<string>? lines)) return (provider, className, version, date);

        foreach (string line in lines)
        {
            (string key, string value) = SplitPair(line);

            if (key.Equals("Provider", StringComparison.OrdinalIgnoreCase))
                provider = Resolve(value, sections);
            else if (key.Equals("Class", StringComparison.OrdinalIgnoreCase))
                className = Resolve(value, sections);
            else if (key.Equals("DriverVer", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length > 0 &&
                    DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                {
                    date = parsed;
                }

                if (parts.Length > 1) version = parts[1].Trim('"');
            }
        }

        return (provider, className, version, date);
    }

    /// <summary>
    /// A [Version] value may be a literal or a %token% defined in [Strings]. Intel's packages use the
    /// token form, and "%Intel%" is not a name anyone should have to read.
    /// </summary>
    private static string Resolve(string value, Dictionary<string, List<string>> sections)
    {
        string text = value.Trim().Trim('"');
        if (text.Length < 3 || !text.StartsWith('%') || !text.EndsWith('%')) return text;

        string key = text[1..^1];

        if (sections.TryGetValue("Strings", out List<string>? lines))
        {
            foreach (string line in lines)
            {
                (string name, string resolved) = SplitPair(line);
                if (name.Equals(key, StringComparison.OrdinalIgnoreCase)) return resolved.Trim('"');
            }
        }

        return key;
    }

    /// <summary>
    /// Hardware ids come from the models sections named by [Manufacturer], including the decorated
    /// <c>Model.NTamd64</c> variants. Walking every section instead would pick up file names from
    /// [SourceDisksFiles] and paths from [DestinationDirs].
    /// </summary>
    private static List<string> HardwareIds(Dictionary<string, List<string>> sections)
    {
        var ids = new List<string>();

        if (!sections.TryGetValue("Manufacturer", out List<string>? manufacturer)) return ids;

        foreach (string line in manufacturer)
        {
            (string key, string value) = SplitPair(line);
            if (key.Length == 0 || value.Length == 0) continue;

            string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
            string baseSection = parts[0];
            if (baseSection.Length == 0) continue;

            AddModels(sections, baseSection, ids);

            foreach (string decoration in parts.Skip(1))
            {
                if (decoration.Length > 0) AddModels(sections, $"{baseSection}.{decoration}", ids);
            }
        }

        return ids;
    }

    private static void AddModels(Dictionary<string, List<string>> sections, string name, List<string> ids)
    {
        if (!sections.TryGetValue(name, out List<string>? lines)) return;

        foreach (string line in lines)
        {
            (_, string value) = SplitPair(line);
            if (value.Length == 0) continue;

            // "InstallSection, HWID, HWID": the first field names the install section, not a device.
            foreach (string token in value.Split(',').Skip(1))
            {
                string id = token.Trim();
                if (LooksLikeHardwareId(id) && !ids.Contains(id, StringComparer.OrdinalIgnoreCase)) ids.Add(id);
            }
        }
    }

    private static bool LooksLikeHardwareId(string value)
    {
        if (value.Length is < 4 or > 260) return false;
        if (value.Contains('%') || value.Contains(':') || value.Contains('=')) return false;
        if (!value.Contains('\\')) return false;

        return !value.EndsWith(".sys", StringComparison.OrdinalIgnoreCase)
            && !value.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            && !value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            && !value.EndsWith(".inf", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> MatchDevices(IReadOnlyList<DriverDevice> devices, List<string> ids)
    {
        var wanted = new HashSet<string>(ids.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        var names = new List<string>();

        foreach (DriverDevice device in devices)
        {
            foreach (string id in device.HardwareIds.Concat(device.CompatibleIds))
            {
                if (!wanted.Contains(Normalize(id))) continue;

                if (!names.Contains(device.Name, StringComparer.OrdinalIgnoreCase)) names.Add(device.Name);
                break;
            }
        }

        return names;
    }

    private static string Normalize(string hardwareId) => hardwareId.Trim().ToUpperInvariant();

    private static (string Key, string Value) SplitPair(string line)
    {
        int equals = line.IndexOf('=');
        if (equals <= 0) return (string.Empty, string.Empty);

        return (line[..equals].Trim(), line[(equals + 1)..].Trim());
    }
}
