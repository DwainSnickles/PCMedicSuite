// ---------------------------------------------------------------------------------------------
// Ported from Hexnest (https://github.com/ahmetcaglayan/Hexnest), Copyright (c) 2026
// Ahmet Çağlayan and Hexnest contributors, and adapted for PC Medic.
//
// Hexnest is MIT licensed, which is compatible with PC Medic's GPL-3.0. See LICENSE and the
// Licence section of README.md.
//
// Why this exists: the Windows Update agent only lists driver packages it has decided to offer.
// A device with no driver at all - the classic "yellow exclamation mark" - is not necessarily
// among them, so a driver page built only on that search can honestly report "no driver updates"
// while the machine still has bare devices. This enumerates the devices themselves and asks
// Windows for each one's Configuration Manager problem code, which is the answer that matters.
// ---------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace PC_Medic;

internal enum DeviceHealth
{
    /// <summary>Windows reports no problem with this device.</summary>
    Healthy,

    /// <summary>No driver installed (CM problem 28, 1 or 19).</summary>
    DriverMissing,

    /// <summary>Driver present but the device is in an error state.</summary>
    Faulty,

    /// <summary>Disabled by the user, by firmware or by policy.</summary>
    Disabled,

    /// <summary>Configured, but waiting for a restart to finish.</summary>
    RestartPending,
}

internal sealed record DriverDevice(
    string Name,
    string DeviceClass,
    string InstanceId,
    IReadOnlyList<string> HardwareIds,
    IReadOnlyList<string> CompatibleIds,
    DeviceHealth Health,
    int ProblemCode,
    string? ProblemText,
    string? DriverVersion,
    string? DriverProvider)
{
    public bool NeedsAttention => Health is DeviceHealth.DriverMissing or DeviceHealth.Faulty;

    /// <summary>The phrase a row shows, e.g. "No driver installed - restart required".</summary>
    public string StatusLine => Health switch
    {
        DeviceHealth.DriverMissing => "NO DRIVER INSTALLED",
        DeviceHealth.Faulty => ProblemText ?? "DEVICE REPORTS A PROBLEM",
        DeviceHealth.Disabled => "DISABLED",
        DeviceHealth.RestartPending => "RESTART REQUIRED",
        _ => "OK",
    };

    public string VersionLine =>
        string.IsNullOrWhiteSpace(DriverVersion)
            ? "no driver bound"
            : $"{DriverVersion}{(string.IsNullOrWhiteSpace(DriverProvider) ? string.Empty : $"  -  {DriverProvider}")}";
}

/// <summary>
/// Enumerates every present PnP device and the driver bound to it, through SetupAPI, CfgMgr32 and
/// the registry. No WMI, no COM, no package: every one of those DLLs ships with Windows, and WMI is
/// the part most likely to be broken on a machine that was just installed.
/// </summary>
internal static class DriverDevices
{
    private const string ClassKeyRoot = @"SYSTEM\CurrentControlSet\Control\Class";

    public static (List<DriverDevice> Devices, string? Error) Scan(CancellationToken token = default)
    {
        var devices = new List<DriverDevice>(512);

        IntPtr set = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
        if (set == InvalidHandle)
        {
            int code = Marshal.GetLastWin32Error();
            return (devices, $"The device tree could not be read (Windows error {code}).");
        }

        try
        {
            var data = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

            for (uint index = 0; SetupDiEnumDeviceInfo(set, index, ref data); index++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    DriverDevice? device = Read(set, ref data);
                    if (device is not null) devices.Add(device);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // One unreadable device must never abort the whole scan.
                }

                data.cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>();
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(set);
        }

        return (devices
            .OrderBy(device => device.Health == DeviceHealth.Healthy)
            .ThenBy(device => device.DeviceClass, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList(), null);
    }

    /// <summary>How many devices have no driver or report a problem.</summary>
    public static int CountNeedingAttention(IEnumerable<DriverDevice> devices)
        => devices.Count(device => device.NeedsAttention);

    private static DriverDevice? Read(IntPtr set, ref SP_DEVINFO_DATA data)
    {
        string deviceId = DeviceInstanceId(data.DevInst);
        if (string.IsNullOrWhiteSpace(deviceId)) return null;

        string name =
            StringProperty(set, ref data, SPDRP_FRIENDLYNAME)
            ?? StringProperty(set, ref data, SPDRP_DEVICEDESC)
            ?? deviceId;

        string className = StringProperty(set, ref data, SPDRP_CLASS) ?? string.Empty;
        string deviceClass = ResolveClassDescription(data.ClassGuid, className);

        // The instance id alone is not enough to match an INF: packages list hardware ids, and a
        // compatible id is the fallback Windows itself uses when nothing matches exactly.
        List<string> hardwareIds = StringListProperty(set, ref data, SPDRP_HARDWAREID);
        List<string> compatibleIds = StringListProperty(set, ref data, SPDRP_COMPATIBLEIDS);

        string? driverKey = StringProperty(set, ref data, SPDRP_DRIVER);
        (string? version, string? provider) = DriverRegistry(driverKey);

        (DeviceHealth health, int problem, string? text) = Status(data.DevInst);

        return new DriverDevice(
            name,
            deviceClass,
            deviceId,
            hardwareIds,
            compatibleIds,
            health,
            problem,
            text,
            version,
            provider);
    }

    private static string DeviceInstanceId(uint devInst)
    {
        if (CM_Get_Device_ID_Size(out int length, devInst, 0) != CrSuccess || length <= 0) length = 512;

        var buffer = new StringBuilder(length + 1);
        return CM_Get_Device_ID(devInst, buffer, buffer.Capacity, 0) == CrSuccess
            ? buffer.ToString()
            : string.Empty;
    }

    private static byte[]? RawProperty(IntPtr set, ref SP_DEVINFO_DATA data, uint property)
    {
        // The first call only sizes the buffer; a property that does not exist is normal.
        SetupDiGetDeviceRegistryProperty(set, ref data, property, out _, null, 0, out uint required);
        if (required == 0) return null;

        var buffer = new byte[required];
        return SetupDiGetDeviceRegistryProperty(set, ref data, property, out _, buffer, required, out _)
            ? buffer
            : null;
    }

    private static string? StringProperty(IntPtr set, ref SP_DEVINFO_DATA data, uint property)
    {
        byte[]? raw = RawProperty(set, ref data, property);
        if (raw is null) return null;

        string value = Encoding.Unicode.GetString(raw).TrimEnd('\0').Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>A REG_MULTI_SZ property, which is how hardware ids and compatible ids are stored.</summary>
    private static List<string> StringListProperty(IntPtr set, ref SP_DEVINFO_DATA data, uint property)
    {
        byte[]? raw = RawProperty(set, ref data, property);
        if (raw is null) return new List<string>();

        return Encoding.Unicode.GetString(raw)
            .TrimEnd('\0')
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToList();
    }

    /// <summary>The class name Windows itself uses, which is already in the user's language.</summary>
    private static string ResolveClassDescription(Guid classGuid, string className)
    {
        if (classGuid == Guid.Empty) return className;

        var buffer = new StringBuilder(256);
        var guid = classGuid;

        return SetupDiGetClassDescription(ref guid, buffer, (uint)buffer.Capacity, out _) && buffer.Length > 0
            ? buffer.ToString()
            : className;
    }

    private static (string? Version, string? Provider) DriverRegistry(string? driverKey)
    {
        if (string.IsNullOrWhiteSpace(driverKey)) return (null, null);

        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey($@"{ClassKeyRoot}\{driverKey}");
            if (key is null) return (null, null);

            string? version = key.GetValue("DriverVersion") as string;
            string? provider = key.GetValue("ProviderName") as string;

            return (
                string.IsNullOrWhiteSpace(version) ? null : version,
                string.IsNullOrWhiteSpace(provider) ? null : provider);
        }
        catch
        {
            // A driver key we cannot open simply means no version to show.
            return (null, null);
        }
    }

    private static (DeviceHealth Health, int Problem, string? Text) Status(uint devInst)
    {
        if (CM_Get_DevNode_Status(out uint status, out uint problem, devInst, 0) != CrSuccess)
        {
            // A node Windows cannot query is usually a ghost entry; calling it broken would be noise.
            return (DeviceHealth.Healthy, 0, null);
        }

        if ((status & DnHasProblem) == 0) return (DeviceHealth.Healthy, 0, null);

        int code = (int)problem;
        DeviceHealth health = problem switch
        {
            CmProbFailedInstall or CmProbNotConfigured or CmProbReinstall => DeviceHealth.DriverMissing,
            CmProbDisabled or CmProbHardwareDisabled or CmProbDisabledService => DeviceHealth.Disabled,
            CmProbNeedRestart => DeviceHealth.RestartPending,
            _ => DeviceHealth.Faulty,
        };

        return (health, code, DescribeProblem(problem));
    }

    // ---- SetupAPI / CfgMgr32 ---------------------------------------------------------------

    private const uint DIGCF_PRESENT = 0x00000002;
    private const uint DIGCF_ALLCLASSES = 0x00000004;

    private const uint SPDRP_DEVICEDESC = 0x00000000;
    private const uint SPDRP_HARDWAREID = 0x00000001;
    private const uint SPDRP_CLASS = 0x00000007;
    private const uint SPDRP_DRIVER = 0x00000009;
    private const uint SPDRP_FRIENDLYNAME = 0x0000000C;
    private const uint SPDRP_COMPATIBLEIDS = 0x0000000D;

    private const uint DnHasProblem = 0x00000400;
    private const int CrSuccess = 0x00000000;

    private const uint CmProbNotConfigured = 1;
    private const uint CmProbNeedRestart = 14;
    private const uint CmProbReinstall = 19;
    private const uint CmProbDisabled = 22;
    private const uint CmProbFailedInstall = 28;
    private const uint CmProbHardwareDisabled = 29;
    private const uint CmProbDisabledService = 32;

    private static readonly IntPtr InvalidHandle = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupDiGetClassDevsW")]
    private static extern IntPtr SetupDiGetClassDevs(IntPtr classGuid, string? enumerator, IntPtr parent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr set, uint index, ref SP_DEVINFO_DATA data);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupDiGetDeviceRegistryPropertyW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr set,
        ref SP_DEVINFO_DATA data,
        uint property,
        out uint propertyType,
        byte[]? buffer,
        uint bufferSize,
        out uint required);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SetupDiGetClassDescriptionW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetClassDescription(
        ref Guid classGuid,
        StringBuilder description,
        uint descriptionSize,
        out uint required);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, EntryPoint = "CM_Get_Device_IDW")]
    private static extern int CM_Get_Device_ID(uint devInst, StringBuilder buffer, int bufferLength, int flags);

    [DllImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_ID_Size")]
    private static extern int CM_Get_Device_ID_Size(out int length, uint devInst, int flags);

    [DllImport("cfgmgr32.dll", EntryPoint = "CM_Get_DevNode_Status")]
    private static extern int CM_Get_DevNode_Status(out uint status, out uint problem, uint devInst, int flags);

    private static string DescribeProblem(uint code) => code switch
    {
        CmProbNotConfigured => "Device is not configured correctly.",
        CmProbNeedRestart => "A restart is required to finish installing the driver.",
        CmProbReinstall => "The driver has to be reinstalled.",
        CmProbDisabled => "The device is disabled.",
        CmProbFailedInstall => "No driver is installed for this device.",
        CmProbHardwareDisabled => "The device is disabled by its firmware.",
        CmProbDisabledService => "The driver service is disabled.",
        10 => "The device cannot start.",
        12 => "The device cannot find enough free resources.",
        31 => "Windows could not load the driver for this device.",
        39 => "The driver is corrupted or missing.",
        43 => "The driver reported that the device failed.",
        52 => "Windows cannot verify the driver's digital signature.",
        _ => $"Windows reports problem code {code}.",
    };
}
