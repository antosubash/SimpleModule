using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SimpleModule.DevTools;

/// <summary>
/// Manages WebSocket connections from browsers and broadcasts reload signals
/// when Vite builds or Tailwind CSS compilation completes.
/// </summary>
public sealed partial class LiveReloadServer : IDisposable
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly ILogger<LiveReloadServer> _logger;

    public LiveReloadServer(ILogger<LiveReloadServer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Notifies all connected browsers to reload. Sends the build type
    /// so the client can decide between a full reload or CSS-only swap.
    /// </summary>
    public async Task NotifyReloadAsync(ReloadType type, string source)
    {
        if (_clients.IsEmpty)
        {
            return;
        }

        var message = JsonSerializer.Serialize(
            new ReloadMessage { Type = type, Source = source },
            LiveReloadJsonContext.Default.ReloadMessage
        );
        var buffer = Encoding.UTF8.GetBytes(message);

        LogBroadcasting(_logger, type, source, _clients.Count);

        List<string>? deadClients = null;

        foreach (var (id, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open)
            {
                (deadClients ??= []).Add(id);
                continue;
            }

            try
            {
                await socket
                    .SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None)
                    .ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogSendFailed(_logger, ex, id);
                (deadClients ??= []).Add(id);
            }
        }

        if (deadClients is not null)
        {
            foreach (var id in deadClients)
            {
                RemoveClient(id);
            }
        }
    }

    /// <summary>
    /// Handles an incoming WebSocket connection from a browser.
    /// Keeps the connection alive until the client disconnects.
    /// </summary>
    internal async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        var clientId = Guid.NewGuid().ToString("N");
        _clients.TryAdd(clientId, webSocket);
        LogClientConnected(_logger, clientId, _clients.Count);

        try
        {
            // Keep connection alive — read until close
            var buffer = new byte[256];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket
                    .ReceiveAsync(buffer, CancellationToken.None)
                    .ConfigureAwait(false);

                if (
                    result.MessageType == WebSocketMessageType.Close
                    || webSocket.State != WebSocketState.Open
                )
                {
                    break;
                }
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (WebSocketException)
        {
            // Client disconnected unexpectedly — this is normal
        }
#pragma warning restore CA1031
        finally
        {
            RemoveClient(clientId);
            LogClientDisconnected(_logger, clientId, _clients.Count);

            if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try
                {
                    await webSocket
                        .CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Server closing",
                            CancellationToken.None
                        )
                        .ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    // Best-effort close
                }
#pragma warning restore CA1031
            }
        }
    }

    public void Dispose()
    {
        foreach (var (_, socket) in _clients)
        {
            socket.Dispose();
        }

        _clients.Clear();
    }

    private void RemoveClient(string id)
    {
        if (_clients.TryRemove(id, out var socket))
        {
            socket.Dispose();
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Live reload: broadcasting {Type} from {Source} to {ClientCount} client(s)"
    )]
    private static partial void LogBroadcasting(
        ILogger logger,
        ReloadType type,
        string source,
        int clientCount
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Live reload: client {ClientId} connected ({TotalClients} total)"
    )]
    private static partial void LogClientConnected(
        ILogger logger,
        string clientId,
        int totalClients
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Live reload: client {ClientId} disconnected ({TotalClients} remaining)"
    )]
    private static partial void LogClientDisconnected(
        ILogger logger,
        string clientId,
        int totalClients
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Live reload: failed to send to client {ClientId}"
    )]
    private static partial void LogSendFailed(ILogger logger, Exception ex, string clientId);
}

/// <summary>
/// The type of reload to perform in the browser.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReloadType>))]
public enum ReloadType
{
    /// <summary>Full page reload (JS/TSX changes).</summary>
    Full,

    /// <summary>CSS-only hot swap (Tailwind/CSS changes).</summary>
    CssOnly,
}

public sealed class ReloadMessage
{
    [JsonPropertyName("type")]
    public ReloadType Type { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";
}

[JsonSerializable(typeof(ReloadMessage))]
internal sealed partial class LiveReloadJsonContext : JsonSerializerContext;

/// <summary>
/// Extension methods to wire up the live reload WebSocket endpoint.
/// </summary>
public static class LiveReloadEndpointExtensions
{
    private const string LiveReloadPath = "/dev/live-reload";

    /// <summary>
    /// Maps the <c>/dev/live-reload</c> WebSocket endpoint used by the browser
    /// to receive reload signals during development.
    /// </summary>
    public static WebApplication MapLiveReload(this WebApplication app)
    {
        app.UseWebSockets();

        app.Map(
            LiveReloadPath,
            async (HttpContext context, LiveReloadServer server) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                using var ws = await context.WebSockets.AcceptWebSocketAsync();
                await server.HandleWebSocketAsync(ws);
            }
        );

        return app;
    }
}
