using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary;

namespace Server {
    public class WebSocketServer {
        private readonly HttpListener _httpListener;
        private readonly Func<string, string> _messageHandler;

        public WebSocketServer(string ipAddress, int port, Func<string, string> messageHandler) {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://{ipAddress}:{port}/");
            _messageHandler = messageHandler;
        }

        public async Task StartAsync() {
            _httpListener.Start();
            Console.WriteLine("WebSocket server listening...");

            while (true) {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest) {
                    HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);

                    WebSocket webSocket = webSocketContext.WebSocket;
                    HandleWebSocket(webSocket);
                } else {
                    context.Response.Close();
                }
            }
        }

        private async void HandleWebSocket(WebSocket webSocket) {
            try {
                byte[] buffer = new byte[1024];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.CloseStatus.HasValue) {
                    string message = Encoding.ASCII.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received: {message}");

                    string responseMessage = _messageHandler(message);

                    byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            } catch (WebSocketException ex) {
                Console.WriteLine($"WebSocket Exception: {ex.Message}");
            }
        }
    }
}
