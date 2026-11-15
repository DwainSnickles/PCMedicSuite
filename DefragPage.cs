using DiskDefrag;

namespace PC_Medic;

/// <summary>Hosts the Disk Defrag tool inside the console instead of in a second process.</summary>
internal sealed partial class DefragPage : PageBase
{
    private Form? _host;

    public DefragPage()
        : base("DISK DEFRAG", "Analyse, defragment and optimize the volumes on this PC \u2014 live cluster map, inside the console.")
    {
        InitializeComponent();
    }

    /// <summary>True while the hosted tool is running an operation.</summary>
    public bool IsWorking => _host is not null && DefragHost.IsWorking(_host);

    public override void OnNavigated()
    {
        if (_host is not null) return;

        _host = DefragHost.CreateEmbedded();
        _host.Dock = DockStyle.Fill;

        // Disk Defrag lives on the Maintenance grid, so its CLOSE button has to lead back there rather
        // than closing a child form that has no window of its own.
        if (_host is DiskDefrag.MainForm tool)
            tool.CloseRequested += () => NavigateTo?.Invoke("maintenance");

        Controls.Add(_host);
        _host.Show();
    }
}
