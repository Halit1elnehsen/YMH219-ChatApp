using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace YMH219_ChatApp;

public sealed class ChatServer : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ClientSession> _clients = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _stop;
    public event Action<ChatMessage>? MessageReceived;
    public event Action<IReadOnlyCollection<string>>? ClientsChanged;
    public bool IsRunning => _listener is not null;

    public async Task StartAsync(int port)
    {
        if (IsRunning) return;
        _stop = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _ = AcceptLoopAsync(_stop.Token);
        await Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var socket = await _listener!.AcceptTcpClientAsync(token);
                var id = Guid.NewGuid().ToString("N");
                var session = new ClientSession(id, socket);
                _clients[id] = session;
                NotifyClients();
                _ = HandleClientAsync(session, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (SocketException) when (token.IsCancellationRequested) { }
    }

    private async Task HandleClientAsync(ClientSession session, CancellationToken token)
    {
        try
        {
            using var reader = new StreamReader(session.Client.GetStream(), Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(session.Client.GetStream(), new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            session.Writer = writer;
            string? line;
            while (!token.IsCancellationRequested && (line = await reader.ReadLineAsync(token)) is not null)
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(line);
                if (message is null) continue;
                if (message.Type == "join") { session.User = message.User.Trim(); NotifyClients(); continue; }
                if (message.Type != "message" || string.IsNullOrWhiteSpace(message.Text)) continue;
                var clean = message with { User = session.User, Text = message.Text.Trim(), Timestamp = DateTime.Now };
                MessageReceived?.Invoke(clean);
                await BroadcastAsync(clean, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (SocketException) { }
        catch (JsonException) { }
        finally
        {
            _clients.TryRemove(session.Id, out _);
            session.Client.Dispose();
            NotifyClients();
        }
    }

    private async Task BroadcastAsync(ChatMessage message, CancellationToken token)
    {
        var json = JsonSerializer.Serialize(message);
        foreach (var client in _clients.Values)
        {
            try
            {
                await client.WriteLock.WaitAsync(token);
                try { if (client.Writer is not null) await client.Writer.WriteLineAsync(json.AsMemory(), token); }
                finally { client.WriteLock.Release(); }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }
    }

    private void NotifyClients() => ClientsChanged?.Invoke(_clients.Values.Select(c => string.IsNullOrWhiteSpace(c.User) ? c.Client.Client.RemoteEndPoint?.ToString() ?? "Yeni istemci" : c.User).ToArray());

    public async ValueTask DisposeAsync()
    {
        if (_stop is not null) { await _stop.CancelAsync(); _stop.Dispose(); }
        _listener?.Stop(); _listener = null;
        foreach (var client in _clients.Values) client.Client.Dispose();
        _clients.Clear(); NotifyClients();
    }

    private sealed class ClientSession(string id, TcpClient client)
    {
        public string Id { get; } = id;
        public TcpClient Client { get; } = client;
        public string User { get; set; } = "";
        public StreamWriter? Writer { get; set; }
        public SemaphoreSlim WriteLock { get; } = new(1, 1);
    }
}
