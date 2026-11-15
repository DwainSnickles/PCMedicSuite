namespace PC_Medic;

partial class SplashForm
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Timer _timer;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _timer = new System.Windows.Forms.Timer(components);

        SuspendLayout();

        _timer.Interval = 15;

        ResumeLayout(false);
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }
}
