using Microsoft.AspNetCore.Mvc;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using sscs2023.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static sscs2023.Classes.DBs.DBClasses.PlayerDBClasses;

namespace sscs2023.Controllers
{
    [ApiController]
    [Route("/noti")]
    public class NotiController : ControllerBase
    {
        public static ConcurrentDictionary<string, WebSocket> WebSockets { get; } = new();
        public static ConcurrentDictionary<long, HashSet<string>> PlayerConnections { get; } = new();

        [HttpPost("hub/v1/negotiate")]
        public IActionResult Negotiate()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();

            string connectionId = Guid.NewGuid().ToString("N");

            var response = new
            {
                negotiateVersion = 0,
                connectionId = connectionId,
                availableTransports = new[]
                {
                    new
                    {
                        transport = "WebSockets",
                        transferFormats = new[] { "Text", "Binary" }
                    }
                }
            };

            return Ok(response);
        }

        [Route("/announcements/v2/mine/unread")]

        [HttpGet]
        public IActionResult GetUnreadAnnouncements()
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null)
                return Unauthorized();
            return Ok(new { });
        }

        [Route("hub/v1")]
        public async Task HandleHub([FromQuery] string id)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null)
            {
            	Console.WriteLine($"[WebSocket] Player is unauthorized");
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
            	Console.WriteLine($"[WebSocket] IsWebSocketRequest is false");
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }


            using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine($"[WebSocket] Player {playerId} connected with connection {id}");

            PlayerConnections.AddOrUpdate(
                (long)playerId,
                _ => new HashSet<string> { id },
                (_, hs) =>
                {
                    lock (hs) { hs.Add(id); }
                    return hs;
                }
            );

            await HandleConnectionAsync((long)playerId, id, socket);
        }

        private static async Task HandleConnectionAsync(long playerId, string connectionId, WebSocket socket)
        {
            using var pingCts = new CancellationTokenSource();

            try
            {
                WebSockets[connectionId] = socket;

                await SendHandshakeAsync(socket);

                _ = Task.Run(() => PingLoopAsync(socket, pingCts.Token));

                var buffer = new byte[4096];
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count).TrimEnd('\x1e');
                    Console.WriteLine($"Player ({playerId}) sent: {message}");

                    await HandleClientMessageAsync(socket, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error:{connectionId}] {ex.Message}");
            }
            finally
            {
                pingCts.Cancel();
                WebSockets.TryRemove(connectionId, out _);

                if (PlayerConnections.TryGetValue(playerId, out var connections))
                {
                    lock (connections)
                    {
                        connections.Remove(connectionId);

                        if (connections.Count == 0)
                            PlayerConnections.TryRemove(playerId, out _);
                    }
                }

                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
                }

                Console.WriteLine($"[WebSocket] Player {playerId} disconnected from connection {connectionId}");
            }
        }

        private static async Task HandleClientMessageAsync(WebSocket socket, string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeProp) && typeProp.GetInt32() == 1)
                {
                    string target = root.GetProperty("target").GetString() ?? string.Empty;
                    string invocationId = root.GetProperty("invocationId").GetString() ?? string.Empty;

                    if (target == "SubscribeToPlayers")
                    {
                        var response = new
                        {
                            type = 3,
                            invocationId = invocationId,
                            result = (object?)null
                        };
                        await SendJsonAsync(socket, response);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ParseError] {ex.Message}");
            }
        }

        private static async Task SendHandshakeAsync(WebSocket socket)
        {
            var handshake = new { protocol = "json", version = 1 };
            await SendJsonAsync(socket, handshake);
        }

        private static async Task PingLoopAsync(WebSocket socket, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    var ping = new { type = 6 };
                    await SendJsonAsync(socket, ping);
                    await Task.Delay(10000, token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[PingError] {ex.Message}");
            }
        }
        public static async Task SendNotificationToPlayer(long playerId, object message)
        {
            if (!PlayerConnections.TryGetValue(playerId, out var connections))
                return;

            foreach (var connectionId in connections)
            {
                if (WebSockets.TryGetValue(connectionId, out var socket) && socket.State == WebSocketState.Open)
                {
                    await SendJsonAsync(socket, message);
                }
            }
        }

        private static async Task SendJsonAsync(WebSocket socket, object obj)
        {
            var json = JsonSerializer.Serialize(obj) + "\x1e";
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}