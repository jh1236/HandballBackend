using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace HandballBackend.EndpointHelpers;

public class SocketWrapper {
    public readonly WebSocket Socket;
    protected Action<WebSocketReceiveResult, string> Callback;

    public SocketWrapper(WebSocket socket, Action<WebSocketReceiveResult, string> callback) {
        Socket = socket;
        Callback = callback;
    }

    public async Task Start() {
        await ReceiveMessage();
    }


    public async Task AsyncSendMessage(object message) {
        var serializedMessage = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(serializedMessage);
        var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
        await Socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    protected async Task ReceiveMessage() {
        var buffer = new ArraySegment<byte>(new byte[4 * 1024]);
        while (Socket.State == WebSocketState.Open) {
            var result = await Socket.ReceiveAsync(buffer, CancellationToken.None);
            var asString = Encoding.UTF8.GetString(buffer.Array);
            Callback(result, asString);
        }
    }
}