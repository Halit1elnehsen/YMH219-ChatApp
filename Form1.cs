using System.Net.Sockets;

namespace YMH219_ChatApp;

public partial class Form1 : Form
{
    private readonly ChatServer _server = new();
    private readonly ChatClient _client = new();
    private TextBox _host = null!, _name = null!, _port = null!, _message = null!;
    private FlowLayoutPanel _transcript = null!;
    private Label _connection = null!, _serverState = null!;
    private ListBox _clients = null!;
    private Button _send = null!;
    private Label _emptyState = null!;
    private bool _closing;

    public Form1()
    {
        InitializeComponent(); BuildInterface();
        _client.MessageReceived += message => Ui(() => AddMessage(message));
        _client.ConnectionChanged += (ok, text) => Ui(() => SetConnection(ok, text));
        _server.ClientsChanged += clients => Ui(() => UpdateClients(clients));
        FormClosing += async (_, _) => { _closing = true; await _client.DisposeAsync(); await _server.DisposeAsync(); };
    }

    private void BuildInterface()
    {
        Text = "Kıyı Sohbeti — YMH219"; StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(900, 660); ClientSize = new Size(1200, 800); BackColor = Color.FromArgb(232, 239, 239); Font = new Font("Segoe UI", 9.5f);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(root);
        var side = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(25, 57, 66), Padding = new Padding(18) }; root.Controls.Add(side, 0, 0);
        var sideLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 12, ColumnCount = 1, BackColor = Color.Transparent };
        sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); side.Controls.Add(sideLayout);
        var title = new Label { Text = "Kıyı Sohbeti", Font = new Font("Segoe UI", 19, FontStyle.Bold), ForeColor = Color.FromArgb(241, 249, 245), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        sideLayout.Controls.Add(title, 0, 0);
        sideLayout.Controls.Add(Label("SUNUCU ADRESİ"), 0, 1); _host = Input("127.0.0.1"); sideLayout.Controls.Add(_host, 0, 2);
        sideLayout.Controls.Add(Label("BAĞLANTI NOKTASI"), 0, 3); _port = Input("5055"); sideLayout.Controls.Add(_port, 0, 4);
        sideLayout.Controls.Add(Label("KULLANICI ADI"), 0, 5); _name = Input("Deniz"); sideLayout.Controls.Add(_name, 0, 6);
        var connectionActions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        connectionActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); connectionActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var startServer = SideButton("Sunucu aç"); startServer.Margin = new Padding(0, 0, 5, 0); startServer.Click += async (_, _) => await StartServerAsync(); connectionActions.Controls.Add(startServer, 0, 0);
        var connect = MainButton("Bağlan", Color.FromArgb(27, 139, 126)); connect.Margin = new Padding(5, 0, 0, 0); connect.Click += async (_, _) => await ConnectAsync(); connectionActions.Controls.Add(connect, 1, 0);
        sideLayout.Controls.Add(connectionActions, 0, 7);
        _serverState = new Label { Text = "●  Sunucu kapalı", ForeColor = Color.FromArgb(171, 196, 194), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
        sideLayout.Controls.Add(Label("KATILIMCILAR"), 0, 9);
        _clients = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(33, 72, 79), ForeColor = Color.FromArgb(236, 247, 241), Font = new Font("Segoe UI", 9.5f), IntegralHeight = false };
        sideLayout.Controls.Add(_clients, 0, 10);
        var serverFoot = new Panel { Dock = DockStyle.Fill }; serverFoot.Controls.Add(_serverState); sideLayout.Controls.Add(serverFoot, 0, 11);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 8, 4, 4), RowCount = 3, ColumnCount = 1 }; main.RowStyles.Add(new RowStyle(SizeType.Absolute, 72)); main.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); main.RowStyles.Add(new RowStyle(SizeType.Absolute, 84)); root.Controls.Add(main, 1, 0);
        var top = new Panel { Dock = DockStyle.Fill };
        top.Controls.Add(new Label { Text = "Sohbet alanı", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(31, 65, 72), AutoSize = true, Location = new Point(4, 2) });
        top.Controls.Add(new Label { Text = "Yerel ağ üzerinde, doğrudan ve sade mesajlaşma", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(86, 112, 115), AutoSize = true, Location = new Point(7, 37) });
        _connection = new Label { Text = "○  Bağlı değil", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(93, 111, 116), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(600, 23) }; top.Controls.Add(_connection);
        top.Resize += (_, _) => _connection.Left = top.ClientSize.Width - _connection.Width - 5; main.Controls.Add(top, 0, 0);

        var transcriptCard = new SurfacePanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(250, 252, 250), Padding = new Padding(8), CornerRadius = 17, OutlineColor = Color.FromArgb(211, 224, 220) };
        _transcript = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(12), BackColor = Color.Transparent };
        _transcript.SizeChanged += (_, _) => ResizeBubbles();
        _emptyState = new Label { Text = "Sohbet henüz başlamadı\nBağlandığınızda mesajlar burada görünür.", Size = new Size(440, 220), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(107, 132, 132), BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 12) };
        _transcript.Controls.Add(_emptyState); transcriptCard.Controls.Add(_transcript); main.Controls.Add(transcriptCard, 0, 1);

        var composer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 14, 0, 0) }; composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112)); composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        _message = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f), PlaceholderText = "Mesajınızı yazın…", BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(250, 252, 250), Margin = new Padding(0, 0, 8, 0), AccessibleName = "Mesaj metni" }; _message.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SendAsync(); } };
        _send = MainButton("Gönder", Color.FromArgb(28, 139, 125)); _send.Click += async (_, _) => await SendAsync(); _send.Enabled = false;
        var disconnect = MainButton("Ayrıl", Color.FromArgb(217, 228, 224), Color.FromArgb(42, 73, 74)); disconnect.Click += async (_, _) => { await _client.DisposeAsync(); SetConnection(false, "Bağlantı kapalı"); };
        composer.Controls.Add(_message, 0, 0); composer.Controls.Add(_send, 1, 0); composer.Controls.Add(disconnect, 2, 0); main.Controls.Add(composer, 0, 2);
    }

    private static Label Label(string text) => new() { Text = text, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(171, 196, 194), Font = new Font("Segoe UI", 8, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
    private static TextBox Input(string text) => new() { Text = text, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(242, 247, 244), BorderStyle = BorderStyle.FixedSingle };
    private static DepthButton SideButton(string text) => new() { Text = text, Dock = DockStyle.Fill, BackColor = Color.FromArgb(55, 93, 96), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
    private static DepthButton MainButton(string text, Color back, Color? fore = null) => new() { Text = text, Dock = DockStyle.Fill, BackColor = back, ForeColor = fore ?? Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

    private async Task StartServerAsync()
    {
        if (!int.TryParse(_port.Text, out var port) || port is < 1024 or > 65535) { ShowError("Bağlantı noktası 1024–65535 arasında bir sayı olmalı."); return; }
        try { await _server.StartAsync(port); _serverState.Text = $"● Sunucu açık · {port}"; _serverState.ForeColor = Color.FromArgb(129, 222, 179); }
        catch (SocketException ex) { ShowError($"Sunucu başlatılamadı. Bu bağlantı noktası kullanımda olabilir.\n\n{ex.Message}"); }
    }
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(_host.Text) || string.IsNullOrWhiteSpace(_name.Text)) { ShowError("Sunucu adresini ve kullanıcı adını girin."); return; }
        if (!int.TryParse(_port.Text, out var port) || port is < 1 or > 65535) { ShowError("Bağlantı noktası 1–65535 arasında bir sayı olmalı."); return; }
        try { await _client.ConnectAsync(_host.Text.Trim(), port, _name.Text.Trim()); _message.Focus(); }
        catch (SocketException ex) { SetConnection(false, "Bağlantı kurulamadı."); ShowError($"Sunucuya bağlanılamadı. Sunucunun açık olduğunu ve adresi kontrol edin.\n\n{ex.Message}"); }
        catch (IOException ex) { ShowError($"Bağlantı sırasında ağ hatası oluştu.\n\n{ex.Message}"); }
    }
    private async Task SendAsync()
    {
        try { await _client.SendAsync(_message.Text); _message.Clear(); _message.Focus(); }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or SocketException) { ShowError($"Mesaj gönderilemedi: {ex.Message}"); }
    }
    private void AddMessage(ChatMessage message)
    {
        if (_closing || IsDisposed) return;
        var localName = string.IsNullOrWhiteSpace(_client.UserName) ? _name.Text.Trim() : _client.UserName;
        var own = string.Equals(message.User, localName, StringComparison.OrdinalIgnoreCase);
        if (_emptyState.Parent == _transcript) _transcript.Controls.Remove(_emptyState);
        var bubble = new MessageBubble(message, own) { Width = Math.Max(260, _transcript.ClientSize.Width - 32), Margin = new Padding(0, 0, 0, 10) };
        _transcript.Controls.Add(bubble); _transcript.ScrollControlIntoView(bubble);
    }
    private void ResizeBubbles()
    {
        if (_transcript is null) return;
        foreach (Control item in _transcript.Controls) if (item is MessageBubble bubble) bubble.Width = Math.Max(260, _transcript.ClientSize.Width - 34);
        if (_emptyState is not null && _emptyState.Parent == _transcript) _emptyState.Width = Math.Max(320, _transcript.ClientSize.Width - 34);
    }
    private void SetConnection(bool connected, string text) { _connection.Text = connected ? $"● {text}" : $"○ {text}"; _connection.ForeColor = connected ? Color.FromArgb(44, 145, 111) : Color.FromArgb(126, 139, 143); _send.Enabled = connected; }
    private void UpdateClients(IReadOnlyCollection<string> values) { if (_closing || IsDisposed) return; _clients.BeginUpdate(); _clients.Items.Clear(); foreach (var value in values) _clients.Items.Add(value); _clients.EndUpdate(); }
    private void Ui(Action action) { if (_closing || IsDisposed || !IsHandleCreated) return; try { BeginInvoke(action); } catch (InvalidOperationException) { } }
    private void ShowError(string text) => MessageBox.Show(this, text, "Bağlantı bilgisi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
