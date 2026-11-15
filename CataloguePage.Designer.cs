namespace PC_Medic;

partial class CataloguePage
{
    private System.ComponentModel.IContainer components = null;

    private Card _card;
    private BufferedPanel _list;
    private Panel _footerPanel;
    private Label _footer;
    private Panel _toolbar;
    private Label _status;
    private Panel _chipsBar;
    private FlowLayoutPanel _chips;
    private Panel _searchHost;
    private TextBox _search;
    private NeonButton _refreshList;
    private NeonButton _refreshCatalogue;
    private NeonButton _install;
    private NeonButton _cancel;
    private System.Windows.Forms.Timer _sweep;
    private ToolTip _tips;

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _card = new Card();
        _list = new BufferedPanel();
        _footerPanel = new Panel();
        _footer = new Label();
        _toolbar = new Panel();
        _status = new Label();
        _chipsBar = new Panel();
        _chips = new FlowLayoutPanel();
        _searchHost = new Panel();
        _search = new TextBox();
        _refreshList = new NeonButton();
        _refreshCatalogue = new NeonButton();
        _install = new NeonButton();
        _cancel = new NeonButton();
        _sweep = new System.Windows.Forms.Timer(components);
        _tips = new ToolTip(components);

        SuspendLayout();

        _card.Controls.Add(_list);
        _card.Dock = DockStyle.Fill;
        _card.Header = "COMMON APPS";
        _card.Kicker = "CATALOGUE — TICK & INSTALL";

        _list.AutoScroll = true;
        _list.BackColor = Theme.Surface;
        _list.Dock = DockStyle.Fill;

        _footerPanel.BackColor = Theme.Bg;
        _footerPanel.Controls.Add(_footer);
        _footerPanel.Dock = DockStyle.Bottom;
        _footerPanel.Height = 26;

        _footer.AutoSize = true;
        _footer.BackColor = Color.Transparent;
        _footer.Font = Theme.MonoSmall;
        _footer.ForeColor = Theme.TextDim;
        _footer.Location = new Point(2, 6);
        _footer.Text = "SOURCE — • SYNCED — • 0 APPS";

        _toolbar.BackColor = Theme.Bg;
        _toolbar.Controls.Add(_status);
        _toolbar.Controls.Add(_searchHost);
        _toolbar.Controls.Add(_refreshList);
        _toolbar.Controls.Add(_refreshCatalogue);
        _toolbar.Controls.Add(_install);
        _toolbar.Controls.Add(_cancel);
        _toolbar.Dock = DockStyle.Top;
        _toolbar.Height = 40;

        _status.AutoSize = true;
        _status.BackColor = Color.Transparent;
        _status.Font = Theme.MonoSmall;
        _status.ForeColor = Theme.TextDim;
        _status.Location = new Point(2, 13);
        _status.Text = "NOT LOADED";

        _chipsBar.BackColor = Theme.Bg;
        _chipsBar.Controls.Add(_chips);
        _chipsBar.Dock = DockStyle.Top;
        _chipsBar.Height = 36;

        _chips.AutoScroll = true;
        _chips.Dock = DockStyle.Fill;
        _chips.FlowDirection = FlowDirection.LeftToRight;
        _chips.WrapContents = false;

        _searchHost.BackColor = Theme.Surface2;
        _searchHost.Controls.Add(_search);
        _searchHost.Height = 26;
        _searchHost.Location = new Point(700, 7);
        _searchHost.Size = new Size(190, 26);

        _search.BackColor = Theme.Surface2;
        _search.BorderStyle = BorderStyle.None;
        _search.Dock = DockStyle.Fill;
        _search.Font = Theme.Ui;
        _search.ForeColor = Theme.TextFaint;
        _search.Location = new Point(0, 0);
        _search.Size = new Size(190, 26);
        _search.TabIndex = 0;
        _search.Text = "SEARCH";

        _refreshList.Height = 30;
        _refreshList.Location = new Point(560, 5);
        _refreshList.Size = new Size(116, 30);
        _refreshList.Text = "REFRESH LIST";

        _refreshCatalogue.Height = 30;
        _refreshCatalogue.Location = new Point(402, 5);
        _refreshCatalogue.Size = new Size(152, 30);
        _refreshCatalogue.Text = "REFRESH CATALOGUE";

        _install.Height = 30;
        _install.Location = new Point(220, 5);
        _install.Primary = true;
        _install.Size = new Size(176, 30);
        _install.Text = "INSTALL SELECTED";

        _cancel.Height = 30;
        _cancel.Location = new Point(120, 5);
        _cancel.Size = new Size(92, 30);
        _cancel.Text = "CANCEL";
        _cancel.Tone = Theme.Bad;
        _cancel.Visible = false;

        _sweep.Interval = 40;

        Controls.Add(_card);
        Controls.Add(_footerPanel);
        Controls.Add(_chipsBar);
        Controls.Add(_toolbar);

        ResumeLayout(false);
    }

    #endregion
}
