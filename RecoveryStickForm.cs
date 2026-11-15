using System.Drawing.Drawing2D;

namespace PC_Medic;

/// <summary>
/// Drive-picker dialog for the recovery stick builder. Lists removable drives,
/// warns that formatting erases everything, then runs RecoveryStickService with
/// a scrolling step log so a stuck diskpart or a slow USB copy is visible.
/// </summary>
internal sealed partial class RecoveryStickForm : Form
{
    private const int WmNcHitTest = 0x0084;
    private const int HtClient = 1;
    private const int HtCaption = 2;

    private readonly List<DriveInfo> _drives = RecoveryStickService.UsableRemovableDrives();
    private int _driveIndex;
    private readonly CancellationTokenSource _cancelCts = new();
    private bool _running;

    public RecoveryStickForm()
    {
        InitializeComponent();

        if (_drives.Count == 0)
        {
            _drivePick.Text = "NO USB DRIVE";
            _drivePick.Enabled = false;
            _action.Enabled = false;
            SetStatus("INSERT A USB STICK AND REOPEN THIS DIALOG", Theme.Warn);
        }
        else
        {
            RefreshDrivePick();
        }

        if (!Native.IsElevated)
        {
            _action.Enabled = false;
            SetStatus("NEEDS AN ELEVATED RUN — RESTART PC MEDIC AS ADMINISTRATOR", Theme.Warn);
        }

        _drivePick.Click += (_, _) => CycleDrive();
        _action.Click += (_, _) => StartBuild();
        _cancel.Click += (_, _) =>
        {
            if (_running) { _cancelCts.Cancel(); SetStatus("CANCELLING…", Theme.Warn); }
            else Close();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        AppendLog("Recovery stick builder ready.");
        // C:\Recovery\WindowsRE is ACL'd to administrators, so an unelevated probe always misses it —
        // only warn once the run can actually read it.
        if (Native.IsElevated && RecoveryStickService.FindWinreImage() is null)
            AppendLog("WARNING: no Winre.wim found — check reagentc /info.");
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape && !_running) Close();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WmNcHitTest && (int)m.Result == HtClient) m.Result = HtCaption;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Theme.PaintSurface(g, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 12f, Theme.Surface, Theme.Alpha(Theme.Accent, 170), Theme.Accent);
    }

    private void CycleDrive()
    {
        if (_drives.Count == 0) return;
        _driveIndex = (_driveIndex + 1) % _drives.Count;
        RefreshDrivePick();
    }

    private void RefreshDrivePick()
    {
        DriveInfo d = _drives[_driveIndex];
        string size = d.TotalSize >= 1L << 30
            ? $"{d.TotalSize / (double)(1L << 30):F1} GB"
            : $"{d.TotalSize / (double)(1L << 20):F0} MB";
        _drivePick.Text = $"USB: {d.Name.TrimEnd('\\')}  ({size})";
    }

    private void StartBuild()
    {
        if (_running || _drives.Count == 0) return;

        DriveInfo target = _drives[_driveIndex];
        DialogResult confirm = MessageBox.Show(
            $"This will FORMAT {target.Name.TrimEnd('\\')} and erase everything on it.\n\nContinue?",
            "PC Medic — Recovery USB",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        _running = true;
        _action.Enabled = false;
        _drivePick.Enabled = false;
        _cancel.Text = "CANCEL";
        SetStatus("BUILDING…", Theme.Accent);

        var progress = new Progress<string>(line =>
        {
            if (!IsDisposed) AppendLog(line);
        });

        _ = Task.Run(async () =>
        {
            RecoveryStickService.Result result;
            try
            {
                result = await RecoveryStickService.CreateAsync(target.Name, progress, _cancelCts.Token);
            }
            catch (OperationCanceledException)
            {
                result = new RecoveryStickService.Result(false, "Cancelled.");
            }
            catch (Exception ex)
            {
                result = new RecoveryStickService.Result(false, ex.Message);
            }

            if (IsDisposed) return;

            BeginInvoke(() =>
            {
                _running = false;
                _action.Enabled = true;
                _drivePick.Enabled = _drives.Count > 0;
                _cancel.Text = "CLOSE";
                SetStatus(result.Message, result.Ok ? Theme.Good : Theme.Bad);
                if (result.Ok) ActivityLog.Success("Recovery USB stick built.");
                else ActivityLog.Error($"Recovery USB build failed: {result.Message}");
            });
        });
    }

    private void AppendLog(string line)
    {
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
    }

    private void SetStatus(string text, Color tone)
    {
        _status.Text = text.ToUpperInvariant();
        _status.ForeColor = tone;
    }
}
