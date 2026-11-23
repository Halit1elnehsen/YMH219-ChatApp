using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace YMH219_ChatApp;

public sealed class ChatClient : IAsyncDisposable
{
    private TcpClient? _client;
    private StreamWriter? _writer;
    private CancellationTokenSource? _stop;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    public string UserName { get; private set; } = "";
    public event Action<ChatMessage>? MessageReceived;
    public event Action<bool, string>? ConnectionChanged;
    public bool IsConnected => _client?.Connected == true;

    public async Task ConnectAsync(string host, int port, string userName)
    {
        if (IsConnected) return;
        var socket = new TcpClient();
        await socket.ConnectAsync(host, port);
        _client = socket;
        UserName = userName.Trim();
        _stop = new CancellationTokenSource();
        _writer = new StreamWriter(socket.GetStream(), new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        await SendEnvelopeAsync(ChatMessage.Create("join", UserName, ""), _stop.Token);
        ConnectionChanged?.Invoke(true, $"Bağlandı · {host}:{port}");
        _ = ReadLoopAsync(_stop.Token);
    }

    public async Task SendAsync(string text)
    {
        if (!IsConnected || _stop is null) throw new InvalidOperationException("Önce sunucuya bağlanın.");
        if (string.IsNullOrWhiteSpace(text)) return;
        await SendEnvelopeAsync(ChatMessage.Create("message", UserName, text.Trim()), _stop.Token);
    }

    private async Task SendEnvelopeAsync(ChatMessage message, CancellationToken token)
    {
        await _sendLock.WaitAsync(token);
        try { await _writer!.WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), token); }
        finally { _sendLock.Release(); }
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        try
        {
            using var reader = new StreamReader(_client!.GetStream(), Encoding.UTF8, leaveOpen: true);
            string? line;
            while (!token.IsCancellationRequested && (line = await reader.ReadLineAsync(token)) is not null)
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(line);
                if (message is not null && message.Type == "message") MessageReceived?.Invoke(message);
            }
            if (!token.IsCancellationRequested) ConnectionChanged?.Invoke(false, "Sunucu bağlantıyı kapattı.");
        }
        catch (OperationCanceledException) { }
        catch (IOException ex) { if (!token.IsCancellationRequested) ConnectionChanged?.Invoke(false, $"Bağlantı kesildi: {ex.Message}"); }
        catch (SocketException ex) { if (!token.IsCancellationRequested) ConnectionChanged?.Invoke(false, $"Ağ hatası: {ex.Message}"); }
        catch (JsonException ex) { if (!token.IsCancellationRequested) ConnectionChanged?.Invoke(false, $"Geçersiz sunucu yanıtı: {ex.Message}"); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_stop is not null) { await _stop.CancelAsync(); _stop.Dispose(); _stop = null; }
        _writer?.Dispose(); _writer = null; _client?.Dispose(); _client = null;
        ConnectionChanged?.Invoke(false, "Bağlantı kesildi.");
    }
}
