namespace PC_Medic;

internal sealed partial class MaintenancePage : PageBase
{
    private sealed record ToolSpec(string Title, string Subtitle, string Glyph, string Status, Color Tone, Action Run);

    public MaintenancePage()
        : base("MAINTENANCE", "The Windows recovery and cleanup tools, one click away.")
    {
        InitializeComponent();

        ToolTile[] tiles =
        {
            _tileCleanup, _tileDefrag, _tileRestorePoint, _tileWindowsUpdate, _tileUninstallApps,
            _tileStartupApps, _tileAnimation, _tileSfc, _tileMemoryDiag, _tileSystemConfig,
            _tilePowerPlan, _tileUltimatePlan, _tileStorageSense, _tileTaskManager, _tileEventViewer,
        };

        List<ToolSpec> specs = BuildSpecs();
        for (int i = 0; i < tiles.Length && i < specs.Count; i++)
        {
            ToolSpec spec = specs[i];
            ToolTile tile = tiles[i];

            tile.Glyph = spec.Glyph;
            tile.Title = spec.Title;
            tile.Subtitle = spec.Subtitle;
            tile.Status = spec.Status;
            tile.Tone = spec.Tone;
            tile.StatusTone = spec.Status == "ADMIN" ? Theme.Warn : spec.Status == "TWEAK" ? Theme.Violet : Theme.TextDim;
            tile.Tag = spec;

            tile.Click += (sender, _) =>
            {
                if (sender is ToolTile clicked && clicked.Tag is ToolSpec tool) tool.Run();
            };

            _tips.SetToolTip(tile, $"{spec.Title}\r\n{spec.Subtitle}");
        }

        _status.Text = Native.IsElevated
            ? "ELEVATED TOKEN — ADMIN TOOLS RUN WITHOUT AN EXTRA PROMPT"
            : "STANDARD USER — ADMIN TOOLS WILL ASK FOR ELEVATION";
    }

    private List<ToolSpec> BuildSpecs()
    {
        string sys = Environment.SystemDirectory;

        return new List<ToolSpec>
        {
            new("DISK CLEANUP", "cleanmgr — temp + update leftovers", "", "SYSTEM", Theme.Good,
                () => Tools.LaunchTool("Disk Cleanup", Path.Combine(sys, "cleanmgr.exe"))),

            new("DISK DEFRAG", "Analyse, defragment and optimize volumes", "", "TOOL", Theme.Accent,
                () => NavigateTo?.Invoke("defrag")),

            new("RESTORE POINT", "Create a rollback point first", "", "SYSTEM", Theme.Accent,
                () => Tools.LaunchTool("System Protection", Path.Combine(sys, "SystemPropertiesProtection.exe"))),

            new("WINDOWS UPDATE", "Install updates and optional drivers", "", "SYSTEM", Theme.Accent,
                () => Tools.LaunchTool("Windows Update", Path.Combine(sys, "control.exe"), "/name Microsoft.WindowsUpdate")),

            new("UNINSTALL APPS", "Installed apps in Settings", "", "SYSTEM", Theme.TextDim,
                () => Tools.LaunchTool("Installed Apps", "ms-settings:appsfeatures-app")),

            new("STARTUP APPS", "Task Manager startup tab", "", "SYSTEM", Theme.Accent,
                () => Tools.LaunchTool("Startup Apps", Path.Combine(sys, "Taskmgr.exe"), "/startup")),

            new("ANIMATION SPEED", $"MenuShowDelay — now {Tools.MenuShowDelay} ms", "", "TWEAK", Theme.Violet,
                () => Tools.SetMenuShowDelay(Tools.MenuShowDelay > 200 ? 200 : 400)),

            new("SFC SCAN", "Repair protected system files", "", "ADMIN", Theme.Warn,
                () => Tools.LaunchTool("SFC", "cmd.exe", "/k sfc /scannow", elevated: true)),

            new("MEMORY DIAG", "MdSched — test RAM health", "", "SYSTEM", Theme.Accent,
                () => Tools.LaunchTool("Memory Diagnostic", Path.Combine(sys, "MdSched.exe"))),

            new("SYSTEM CONFIG", "msconfig — boot and services", "", "SYSTEM", Theme.TextDim,
                () => Tools.LaunchTool("System Configuration", Path.Combine(sys, "msconfig.exe"))),

            new("POWER PLAN", "Choose an active power plan", "", "SYSTEM", Theme.Accent,
                () => Tools.LaunchTool("Power Options", Path.Combine(sys, "powercfg.cpl"))),

            new("ULTIMATE PLAN", "Unlock Ultimate Performance", "", "ADMIN", Theme.Warn,
                () => Tools.LaunchTool("Ultimate Performance", Path.Combine(sys, "powercfg.exe"), "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61", elevated: true)),

            new("STORAGE SENSE", "Automatic temp + download cleanup", "", "SYSTEM", Theme.TextDim,
                () => Tools.LaunchTool("Storage Sense", "ms-settings:storagesense")),

            new("TASK MANAGER", "Live processes, performance, startup", "", "SYSTEM", Theme.Accent,
                () => Tools.LaunchTool("Task Manager", Path.Combine(sys, "Taskmgr.exe"))),

            new("EVENT VIEWER", "Memory test results and boot logs", "", "SYSTEM", Theme.TextDim,
                () => Tools.LaunchTool("Event Viewer", Path.Combine(sys, "eventvwr.msc"))),
        };
    }
}
