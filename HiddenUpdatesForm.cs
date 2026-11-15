namespace PC_Medic;

/// <summary>
/// The hidden-updates list: every package id the blacklist has excluded from scans, with the
/// choice to let selected entries - or all of them - be offered again. Unhiding only clears the
/// remembered id; the package comes back on the next scan like any other update.
/// </summary>
internal sealed partial class HiddenUpdatesForm : Form
{

    /// <summary>Raised when OPEN FILE is pressed; the page owns the file-launch plumbing.</summary>
    public event Action? OpenFileRequested;

    /// <summary>True when at least one package was unhidden, so the page knows to rescan.</summary>
    public bool Changed { get; private set; }

    public HiddenUpdatesForm()
    {
        Text = "Hidden updates";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Ui;
        ClientSize = new Size(620, 400);

        InitializeComponent();

        _unhide.Click += (_, _) => Unhide(SelectedIds());
        _unhideAll.Click += (_, _) => ConfirmUnhideAll();
        _openFile.Click += (_, _) => OpenFileRequested?.Invoke();
        _help.Click += (_, _) => HelpForm.Open(this, "hidden-updates");
        _close.Click += (_, _) => Close();

        Reload();
    }

    private void Reload()
    {
        _list.Items.Clear();

        foreach (string id in UpdatesService.IgnoredIds) _list.Items.Add(id);

        _status.Text = _list.Items.Count == 0
            ? "NOTHING IS HIDDEN"
            : $"{_list.Items.Count} HIDDEN PACKAGE{(_list.Items.Count == 1 ? string.Empty : "S")}";
    }

    private List<string> SelectedIds() =>
        _list.SelectedItems.Cast<string>().ToList();

    private void Unhide(IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
        {
            _status.Text = "SELECT A PACKAGE FIRST";
            _status.ForeColor = Theme.Warn;
            return;
        }

        int unhidden = 0;
        foreach (string id in ids)
        {
            if (!UpdatesService.Unignore(id)) continue;

            unhidden++;
            _list.Items.Remove(id);
            ActivityLog.Info($"Hidden update restored: {id}");
        }

        if (unhidden == 0)
        {
            _status.Text = "COULD NOT UNHIDE";
            _status.ForeColor = Theme.Bad;
            return;
        }

        Changed = true;
        _status.Text = $"{unhidden} UNHIDDEN — THEY RETURN ON THE NEXT SCAN";
        _status.ForeColor = Theme.Good;
    }

    private void ConfirmUnhideAll()
    {
        if (_list.Items.Count == 0) return;

        DialogResult answer = MessageBox.Show(
            this,
            $"Let all {_list.Items.Count} hidden package(s) be offered by scans again?",
            "Unhide all",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes) return;

        int unhidden = 0;
        foreach (string id in UpdatesService.IgnoredIds.ToList())
        {
            if (!UpdatesService.Unignore(id)) continue;

            unhidden++;
            ActivityLog.Info($"Hidden update restored: {id}");
        }

        Reload();
        Changed = unhidden > 0;
        _status.Text = $"{unhidden} UNHIDDEN — THEY RETURN ON THE NEXT SCAN";
        _status.ForeColor = Theme.Good;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) Close();
    }
}
