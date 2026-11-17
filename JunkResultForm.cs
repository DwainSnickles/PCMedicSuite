namespace PC_Medic;

/// <summary>
/// What one cleanup pass added up to: the freed figure, the counts that explain where the rest went,
/// and a line per category so a row that did nothing is visible rather than averaged away.
///
/// The old summary was a single line in the dialog's status label, which ran past the window edge and
/// could not say which category failed. Here every category keeps its own row.
/// </summary>
internal sealed partial class JunkResultForm : Form
{
    private JunkResultForm(IReadOnlyList<(JunkTarget Target, JunkCleanResult Result)> results)
    {
        Text = "Disk cleanup \u2014 results";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(640, 500);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;

        InitializeComponent();

        ulong freed = results.Aggregate(0UL, (sum, item) => sum + item.Result.Freed);
        int removed = results.Sum(item => item.Result.Removed);
        int skipped = results.Sum(item => item.Result.Skipped);
        int queued = results.Sum(item => item.Result.QueuedReboot);
        int failed = results.Sum(item => item.Result.Failed);

        _freedTile.Value = Theme.Bytes(freed);
        _freedTile.Sub = freed > 0 ? "THIS PASS" : "NOTHING FREED";

        _removedTile.Value = removed.ToString("N0");
        _removedTile.Sub = "ITEMS GONE";

        _skippedTile.Value = skipped.ToString("N0");
        _skippedTile.Sub = "IN USE";

        _rebootTile.Value = queued.ToString("N0");
        _rebootTile.Sub = "DELETED AT BOOT";

        foreach ((JunkTarget target, JunkCleanResult result) in results)
        {
            _rows.Add(target.Name, Outcome(target, result), Tone(result));
        }

        _note.Text =
            "HELD OPEN FILES ARE IN USE BY A RUNNING APP \u2014 CLOSE IT AND RUN CLEAN SELECTED AGAIN." +
            (failed > 0 ? $"  {failed:N0} FAILED \u2014 SEE THE ACTIVITY FEED." : string.Empty);

        _help.Click += (_, _) => HelpForm.Open(this, "junk-cleanup");
        _close.Click += (_, _) => Close();
        CancelButton = _close;
    }

    /// <summary>Shows the pass; the caller owns nothing afterwards.</summary>
    public static void Show(IWin32Window? owner, IReadOnlyList<(JunkTarget Target, JunkCleanResult Result)> results)
    {
        using var form = new JunkResultForm(results);
        form.ShowDialog(owner);
    }

    /// <summary>One row's worth of detail, in the order the eye wants it: removed, freed, held back.</summary>
    private static string Outcome(JunkTarget target, JunkCleanResult result)
    {
        if (result.Error is not null && result.Removed == 0 && result.Freed == 0)
        {
            return result.Error.ToUpperInvariant();
        }

        // The bin is purged as a whole, so it reports bytes but never an item count.
        if (target.Id == "recyclebin")
        {
            return $"PURGED \u00b7 {Theme.Bytes(result.Freed)} FREED";
        }

        if (result.Removed == 0 && result.Freed == 0 && result.QueuedReboot == 0)
        {
            return "NOTHING TO REMOVE";
        }

        string text = $"{result.Removed:N0} REMOVED \u00b7 {Theme.Bytes(result.Freed)} FREED";
        if (result.Skipped > 0) text += $" \u00b7 {result.Skipped:N0} IN USE";
        if (result.QueuedReboot > 0) text += $" \u00b7 {result.QueuedReboot:N0} AT BOOT";
        return text;
    }

    /// <summary>A refusal is the one outcome worth colour: everything else is either done or idle.</summary>
    private static Color Tone(JunkCleanResult result) =>
        result.Error is not null && result.Removed == 0 && result.Freed == 0 ? Theme.Warn
        : result.Removed == 0 && result.Freed == 0 ? Theme.TextDim
        : Theme.Text;
}
