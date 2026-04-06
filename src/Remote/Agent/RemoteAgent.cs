using System.Net.WebSockets;
using System.Text;
using Photobooth.Remote.Protocol;

namespace Photobooth.Remote.Agent;

/// <summary>
/// Background service that maintains a WebSocket connection to the remote monitoring server.
/// <list type="bullet">
///   <item>Sends a <see cref="HeartbeatMessage"/> on every <see cref="RemoteAgentOptions.HeartbeatInterval"/>.</item>
///   <item>Receives <see cref="RemoteCommand"/> messages and dispatches them via <see cref="CommandDispatcher"/>.</item>
///   <item>Reconnects with exponential backoff when the connection drops.</item>
///   <item>If the server is unreachable, the kiosk continues to operate normally.</item>
/// </list>
/// </summary>
public sealed class RemoteAgent : IAsyncDisposable
{
    private readonly RemoteAgentOptions _options;
    private readonly IHeartbeatSource _heartbeatSource;
    private readonly CommandDispatcher _dispatcher;
    private CancellationTokenSource? _cts;
    private Task? _runTask;

    public RemoteAgent(
        RemoteAgentOptions options,
        IHeartbeatSource heartbeatSource,
        CommandDispatcher dispatcher)
    {
        _options = options;
        _heartbeatSource = heartbeatSource;
        _dispatcher = dispatcher;
    }

    /// <summary>Starts the agent loop in the background.</summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _runTask = RunAsync(_cts.Token);
    }

    /// <summary>Stops the agent and waits for clean shutdown.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            if (_runTask is not null)
            {
                try { await _runTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            _cts.Dispose();
        }
    }

    // -----------------------------------------------------------------------
    // Core loop
    // -----------------------------------------------------------------------

    private async Task RunAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_options.ServerUrl))
        {
            Console.Error.WriteLine("[RemoteAgent] No server URL configured — agent disabled.");
            return;
        }

        var delay = _options.InitialReconnectDelay;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndRunAsync(ct).ConfigureAwait(false);
                // Clean disconnect — reset backoff
                delay = _options.InitialReconnectDelay;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[RemoteAgent] Connection lost: {ex.Message}. Reconnecting in {delay.TotalSeconds:F0}s.");
            }

            try
            {
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return; }

            // Exponential backoff, capped at MaxReconnectDelay
            delay = delay * 2 < _options.MaxReconnectDelay ? delay * 2 : _options.MaxReconnectDelay;
        }
    }

    private async Task ConnectAndRunAsync(CancellationToken ct)
    {
        using var ws = new ClientWebSocket();

        if (!string.IsNullOrEmpty(_options.ApiKey))
            ws.Options.SetRequestHeader("X-Api-Key", _options.ApiKey);

        Console.Error.WriteLine($"[RemoteAgent] Connecting to {_options.ServerUrl}…");
        await ws.ConnectAsync(new Uri(_options.ServerUrl), ct).ConfigureAwait(false);
        Console.Error.WriteLine("[RemoteAgent] Connected.");

        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var sendTask    = HeartbeatLoopAsync(ws, heartbeatCts.Token);
        var receiveTask = ReceiveLoopAsync(ws, heartbeatCts.Token);

        // Stop both loops when either completes
        var completed = await Task.WhenAny(sendTask, receiveTask).ConfigureAwait(false);
        await heartbeatCts.CancelAsync().ConfigureAwait(false);

        try { await Task.WhenAll(sendTask, receiveTask).ConfigureAwait(false); }
        catch (OperationCanceledException) { }

        // Re-throw if there was a real error
        await completed.ConfigureAwait(false);
    }

    private async Task HeartbeatLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var heartbeat = await _heartbeatSource.GetHeartbeatAsync(ct).ConfigureAwait(false);
            heartbeat = heartbeat with { KioskId = _options.KioskId };

            var bytes = ProtocolSerializer.Serialize(heartbeat);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct)
                .ConfigureAwait(false);

            await Task.Delay(_options.HeartbeatInterval, ct).ConfigureAwait(false);
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];

        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct)
                    .ConfigureAwait(false);
                return;
            }

            if (result.MessageType != WebSocketMessageType.Text) continue;

            var payload = buffer.AsSpan(0, result.Count);
            var command = ProtocolSerializer.Deserialize<RemoteCommand>(payload);
            if (command is null) continue;

            var ack = await _dispatcher.DispatchAsync(command, ct).ConfigureAwait(false);

            var ackBytes = ProtocolSerializer.Serialize(ack);
            await ws.SendAsync(ackBytes, WebSocketMessageType.Text, endOfMessage: true, ct)
                .ConfigureAwait(false);
        }
    }
}
