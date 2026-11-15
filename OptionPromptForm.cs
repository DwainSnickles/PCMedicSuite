namespace PC_Medic;

/// <summary>
/// A small themed picker for choices MessageBox cannot label: each option gets its own button
/// and the result is the index picked, or -1 when the window is closed. The default index is
/// the safe answer — it takes Enter, and closing the window behaves the same way.
/// </summary>
internal sealed partial class OptionPromptForm : Form
{
    private readonly List<NeonButton> _buttons = new();
    private int _choice = -1;

    private OptionPromptForm(string title, string message, string[] options, int defaultIndex)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        // options.Length * (158 + 10 margin) + 28 padding + slack.
        ClientSize = new Size(44 + options.Length * 168, 236);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;

        InitializeComponent();
        _body.Text = message;

        // RightToLeft flow puts the first-added control rightmost, so adding in reverse
        // leaves options[0] on the left and the default answer in the Enter corner.
        for (int i = options.Length - 1; i >= 0; i--)
        {
            int index = i;
            var button = new NeonButton
            {
                Text = options[i],
                Width = 158,
                Height = 34,
                Primary = i == defaultIndex,
                Margin = new Padding(0, 0, 10, 0),
            };
            button.Click += (_, _) =>
            {
                _choice = index;
                DialogResult = DialogResult.OK;
            };
            _buttonsPanel.Controls.Add(button);
            _buttons.Add(button);
        }

        if (defaultIndex >= 0 && defaultIndex < _buttons.Count)
        {
            // _buttons was filled right-to-left, so the default sits at Count-1-defaultIndex.
            AcceptButton = _buttons[_buttons.Count - 1 - defaultIndex];
            CancelButton = AcceptButton;
        }
    }

    /// <summary>Shows the picker; returns the option index, or -1 when dismissed.</summary>
    public static int Ask(IWin32Window? owner, string title, string message,
                          string[] options, int defaultIndex = -1)
    {
        using var form = new OptionPromptForm(title, message, options, defaultIndex);
        return form.ShowDialog(owner) == DialogResult.OK ? form._choice : -1;
    }
}
