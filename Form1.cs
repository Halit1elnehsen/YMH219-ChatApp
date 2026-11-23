using System.Net.Sockets;

namespace YMH219_ChatApp;

public partial class Form1 : Form
{
    private readonly ChatServer _server = new();
    private readonly ChatClient _client = new();
    private TextBox _host = null!, _name = null!, _port = null!, _message = null!;
    private RichTextBox _transcript = null!;
    private Label _connection = null!, _serverState = null!;
    private ListBox _clients = null!;
    private Button _send = null!;
    private bool _closing;

    public Form1()
    {
        InitializeComponent(); BuildInterface();
        _client.MessageReceived += message => Ui(() => AddMessage(message));
        _client.ConnectionChanged += (ok, text) => Ui(() => SetConnection(ok, text));
        _server.MessageReceived += message => Ui(() => AddMessage(message));
        _server.ClientsChanged += clients => Ui(() => UpdateClients(clients));
        FormClosing += async (_, _) => { _closing = true; await _client.DisposeAsync(); await _server.DisposeAsync(); };
    }

    private void BuildInterface()
    {
        Text = "Kıyı Sohbeti — YMH219"; StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(860, 620); ClientSize = new Size(1120, 760); BackColor = Color.FromArgb(241, 244, 245);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(root);
        var side = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(31, 58, 67), Padding = new Padding(20) }; root.Controls.Add(side, 0, 0);
        var sideLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 9, ColumnCount = 1, BackColor = Color.Transparent };
        sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); sideLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); side.Controls.Add(sideLayout);
        var title = new Label { Text = "KIYI\nSOHBETİ", Font = new Font("Segoe UI", 19, FontStyle.Bold), ForeColor = Color.FromArgb(227, 244, 234), Dock = DockStyle.Fill };
        sideLayout.Controls.Add(title, 0, 0);
        sideLayout.Controls.Add(Label("SUNUCU ADRESİ"), 0, 1); _host = Input("127.0.0.1"); sideLayout.Controls.Add(_host, 0, 2);
        sideLayout.Controls.Add(Label("BAĞLANTI NOKTASI"), 0, 3); _port = Input("5055"); sideLayout.Controls.Add(_port, 0, 4);
        sideLayout.Controls.Add(Label("KULLANICI ADI"), 0, 5); _name = Input(Environment.UserName); sideLayout.Controls.Add(_name, 0, 6);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 5, 0, 0) };
        var startServer = SideButton("SUNUCUYU AÇ"); startServer.Click += async (_, _) => await StartServerAsync();
        buttons.Controls.Add(startServer); sideLayout.Controls.Add(buttons, 0, 7);
        var clientGroup = new Panel { Dock = DockStyle.Fill };
        _serverState = new Label { Text = "● Sunucu kapalı", ForeColor = Color.FromArgb(174, 196, 198), Font = new Font("Segoe UI", 9, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
        var listTitle = new Label { Text = "BAĞLI İSTEMCİLER", ForeColor = Color.FromArgb(174, 196, 198), Font = new Font("Segoe UI", 8, FontStyle.Bold), Dock = DockStyle.Top, Height = 28 };
        _clients = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(40, 72, 81), ForeColor = Color.FromArgb(232, 246, 239), Font = new Font("Segoe UI", 9) };
        clientGroup.Controls.Add(_clients); clientGroup.Controls.Add(listTitle); clientGroup.Controls.Add(_serverState); sideLayout.Controls.Add(clientGroup, 0, 8);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24, 8, 4, 4), RowCount = 3, ColumnCount = 1 }; main.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); main.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); main.RowStyles.Add(new RowStyle(SizeType.Absolute, 76)); root.Controls.Add(main, 1, 0);
        var top = new Panel { Dock = DockStyle.Fill };
        top.Controls.Add(new Label { Text = "Sohbet odası", Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.FromArgb(36, 67, 73), AutoSize = true, Location = new Point(0, 5) });
        _connection = new Label { Text = "● Bağlı değil", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(126, 139, 143), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(550, 15) }; top.Controls.Add(_connection);
        top.Resize += (_, _) => _connection.Left = top.ClientSize.Width - _connection.Width; main.Controls.Add(top, 0, 0);
        _transcript = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = new Font("Segoe UI", 10), DetectUrls = false, Padding = new Padding(15) }; main.Controls.Add(_transcript, 0, 1);
        var composer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 14, 0, 0) }; composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118)); composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        _message = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11), PlaceholderText = "Bir mesaj yaz…", BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 10, 0) }; _message.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SendAsync(); } };
        _send = MainButton("GÖNDER", Color.FromArgb(49, 125, 104)); _send.Click += async (_, _) => await SendAsync();
        _send.Enabled = false;
        var disconnect = MainButton("AYRIL", Color.FromArgb(226, 232, 230), Color.FromArgb(57, 80, 78)); disconnect.Click += async (_, _) => await _client.DisposeAsync();
        composer.Controls.Add(_message, 0, 0); composer.Controls.Add(_send, 1, 0); composer.Controls.Add(disconnect, 2, 0); main.Controls.Add(composer, 0, 2);
        var connect = MainButton("BAĞLAN", Color.FromArgb(41, 100, 116)); connect.Click += async (_, _) => await ConnectAsync(); side.Controls.Add(connect); connect.Dock = DockStyle.Bottom; connect.Height = 46;
    }

    private static Label Label(string text) => new() { Text = text, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(174, 196, 198), Font = new Font("Segoe UI", 8, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft };
    private static TextBox Input(string text) => new() { Text = text, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(245, 248, 245), BorderStyle = BorderStyle.FixedSingle };
    private static Button SideButton(string text) => new() { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(55, 103, 100), ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold), Cursor = Cursors.Hand };
    private static Button MainButton(string text, Color back, Color? fore = null) => new() { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore ?? Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };

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
        var own = string.Equals(message.User, _client.UserName, StringComparison.OrdinalIgnoreCase);
        _transcript.SelectionStart = _transcript.TextLength; _transcript.SelectionColor = own ? Color.FromArgb(38, 125, 102) : Color.FromArgb(54, 89, 125);
        _transcript.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold); _transcript.AppendText($"{message.User}  ·  {message.Timestamp:HH:mm:ss}\n");
        _transcript.SelectionColor = Color.FromArgb(44, 59, 64); _transcript.SelectionFont = new Font("Segoe UI", 10); _transcript.AppendText($"{message.Text}\n\n"); _transcript.ScrollToCaret();
    }
    private void SetConnection(bool connected, string text) { _connection.Text = connected ? $"● {text}" : $"○ {text}"; _connection.ForeColor = connected ? Color.FromArgb(44, 145, 111) : Color.FromArgb(126, 139, 143); _send.Enabled = connected; }
    private void UpdateClients(IReadOnlyCollection<string> values) { if (_closing || IsDisposed) return; _clients.BeginUpdate(); _clients.Items.Clear(); foreach (var value in values) _clients.Items.Add(value); _clients.EndUpdate(); }
    private void Ui(Action action) { if (_closing || IsDisposed || !IsHandleCreated) return; try { BeginInvoke(action); } catch (InvalidOperationException) { } }
    private void ShowError(string text) => MessageBox.Show(this, text, "Bağlantı bilgisi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
