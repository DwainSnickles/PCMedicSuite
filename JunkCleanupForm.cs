namespace PC_Medic;

/// <summary>
/// The junk finder: scans the cleanable categories on open, shows what each one weighs, and removes
/// only what is ticked. Cleaning runs on a background thread so the dialog stays responsive, and
/// every target goes through JunkService, which sends files to the Recycle Bin where it can.
/// </summary>
internal sealed partial class JunkCleanupForm : Form
{
    private readonly List<JunkTarget> _targets = new();
    private bool _scanning;

    public JunkCleanupForm()
    {
        Text = "Disk cleanup";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        ClientSize = new Size(640, 430);

        InitializeComponent();

        _clean.Click += async (_, _) => await CleanSelectedAsync();
        _help.Click += (_, _) => HelpForm.Open(this, "junk-cleanup");
        _close.Click += (_, _) => Close();

        _ = ScanAsync();
    }

    private async Task ScanAsync()
    {
        _scanning = true;
        _status.Text = "SCANNING FOR JUNK…";
        _status.ForeColor = Theme.Accent;
        _clean.Enabled = false;

        List<JunkTarget> targets;
        try
        {
            targets = await Task.Run(JunkService.Scan);
        }
        catch (Exception ex)
        {
            _status.Text = $"SCAN FAILED — {ex.Message.ToUpperInvariant()}";
            _status.ForeColor = Theme.Bad;
            return;
        }

        if (IsDisposed) return;

        _targets.Clear();
        _list.Items.Clear();

        foreach (JunkTarget target in targets)
        {
            _targets.Add(target);
            string label = $"{target.Name} — {Theme.Bytes(target.Bytes)}" +
                           (target.RequiresAdmin && !Native.IsElevated ? "  (NEEDS ADMIN)" : string.Empty);
            int index = _list.Items.Add(label);
            _list.SetItemChecked(index, target.Bytes > 0);
        }

        ulong total = targets.Aggregate(0UL, (sum, target) => sum + target.Bytes);
        _status.Text = total == 0
            ? "NOTHING FOUND — THE USUAL SPOTS ARE CLEAN"
            : $"{targets.Count(t => t.Bytes > 0)} CATEGORY(IES) • {Theme.Bytes(total)} FOUND";
        _status.ForeColor = total == 0 ? Theme.Good : Theme.Text;

        _scanning = false;
        _clean.Enabled = true;
    }

    private async Task CleanSelectedAsync()
    {
        var selected = _list.CheckedIndices
            .Cast<int>()
            .Where(index => index < _targets.Count)
            .Select(index => _targets[index])
            .ToList();

        if (selected.Count == 0)
        {
            _status.Text = "TICK A CATEGORY FIRST";
            _status.ForeColor = Theme.Warn;
            return;
        }

        _clean.Enabled = false;
        _list.Enabled = false;

        int removed = 0, skipped = 0, queuedReboot = 0, failed = 0;
        ulong freed = 0;
        string? lastError = null;

        foreach (JunkTarget target in selected)
        {
            if (IsDisposed) return;

            _status.Text = $"CLEANING {target.Name}…";
            _status.ForeColor = Theme.Accent;

            JunkCleanResult result = await Task.Run(() => JunkService.Clean(target.Id));
            if (IsDisposed) return;

            removed += result.Removed;
            skipped += result.Skipped;
            queuedReboot += result.QueuedReboot;
            failed += result.Failed;
            freed += result.Freed;
            if (result.Error is not null) lastError = result.Error;

            ActivityLog.Write(
                result.Error is null ? LogLevel.Success : LogLevel.Warn,
                $"Junk cleanup '{target.Name}': {result.Removed} removed, {Theme.Bytes(result.Freed)} freed" +
                (result.QueuedReboot > 0 ? $", {result.QueuedReboot} queued for reboot" : string.Empty) +
                (result.Skipped > 0 ? $", {result.Skipped} skipped (in use)" : string.Empty) +
                (result.Error is null ? string.Empty : $" — {result.Error}"));
        }

        // Sizes changed, so the scan runs again rather than showing stale numbers.
        await ScanAsync();
        if (IsDisposed) return;

        ActivityLog.Info(
            $"Junk cleanup finished: {Theme.Bytes(freed)} freed, {removed} removed, " +
            $"{skipped} skipped (in use), {queuedReboot} queued for reboot, {failed} failed.");

        string summary = freed > 0 || removed > 0
            ? $"FREED {Theme.Bytes(freed)} • {removed} ITEM(S) REMOVED"
            : "NOTHING WAS OLD ENOUGH TO REMOVE";
        if (skipped > 0) summary += $" • {skipped} SKIPPED (IN USE)";
        if (queuedReboot > 0) summary += $" • {queuedReboot} DELETED ON REBOOT";
        if (failed > 0) summary += $" • {failed} FAILED";
        if (lastError is not null) summary += $" • {lastError.ToUpperInvariant()}";

        _status.Text = summary;
        _status.ForeColor = freed > 0 ? Theme.Good : Theme.Warn;
        _clean.Enabled = true;
        _list.Enabled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape && !_scanning) Close();
    }
}
