using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace HandballBackend.Sockets;

public static class SocketDirectory {
    public static ConcurrentDictionary<string, WebSocket> Sockets = new();

    public static void AddSocket(string id, WebSocket socket) {
        Sockets.TryAdd(id, socket);
    }

    public static async Task CloseSocket(string id) {
        WebSocket? socket;
        Sockets.TryRemove(id, out socket);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    }

    public static WebSocket GetSocket(string id) {
        return Sockets[id];
    }

    public static Guid getId() {
        return Guid.NewGuid();
    }
}