namespace PC_Medic;

internal enum RebootChoice { Later, RestartNow, Never }

/// <summary>
/// The end-of-run restart question. "Restart later" is a first-class answer, not an implied
/// No: the pending-restart flag stays set, so the next clean run asks again. "Never" is not
/// remembered here — the caller maps it onto the Settings REBOOT policy, which is the one
/// place that decision is documented and editable.
/// </summary>
internal sealed partial class RebootPromptForm : Form
{
    private RebootChoice _choice = RebootChoice.Later;

    private RebootPromptForm(string reason)
    {
        InitializeComponent();

        _body.Text = $"A restart is needed: {reason}.\n\nRestart now, or keep working and do it later?";

        // RightToLeft flow: the first control added sits rightmost, so the affirmative
        // action lands in the corner a Yes/No dialog would put it.
        WireChoice(_btnNow, RebootChoice.RestartNow);
        WireChoice(_btnLater, RebootChoice.Later);
        WireChoice(_btnNever, RebootChoice.Never);

        AcceptButton = _btnNow;
        CancelButton = _btnLater;
    }

    private void WireChoice(NeonButton button, RebootChoice choice)
    {
        button.Click += (_, _) =>
        {
            _choice = choice;
            DialogResult = DialogResult.OK;
        };
    }

    /// <summary>Shows the prompt; closing the window counts as "later".</summary>
    public static RebootChoice Ask(IWin32Window? owner, string reason)
    {
        using var form = new RebootPromptForm(reason);
        return form.ShowDialog(owner) == DialogResult.OK ? form._choice : RebootChoice.Later;
    }
}
