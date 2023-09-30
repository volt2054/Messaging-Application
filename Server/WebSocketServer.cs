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

        private static readonly Dictionary<string, System.Net.WebSockets.WebSocket> _clientWebSockets = new Dictionary<string, System.Net.WebSockets.WebSocket>();
        private static readonly Dictionary<string, string> _clientUserIds = new Dictionary<string, string>();

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

                    System.Net.WebSockets.WebSocket webSocket = webSocketContext.WebSocket;

                    string clientID = Guid.NewGuid().ToString(); // generate a unique id for client
                    _clientWebSockets.Add(clientID, webSocket); // link the client id to a websocket

                    byte[] clientIDBytes = Encoding.ASCII.GetBytes(clientID);
                    await webSocket.SendAsync(new ArraySegment<byte>(clientIDBytes), WebSocketMessageType.Text, true, CancellationToken.None); // send client id to client

                    HandleWebSocket(webSocket, clientID);
                } else {
                    context.Response.Close();
                }
            }
        }

        private async void HandleWebSocket(System.Net.WebSockets.WebSocket webSocket, string clientID) {
            try {
                byte[] buffer = new byte[1024];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.CloseStatus.HasValue) {
                    string message = Encoding.ASCII.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received: {message}");

                    string requestID = message.Split(":")[0];
                    string messageDetails = message.Split(":")[1];

                    string responseMessage = _messageHandler(messageDetails);

                    responseMessage = requestID + ":" + responseMessage;

                    Console.WriteLine("SENT: " + responseMessage);
                    byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            } catch (WebSocketException ex) {
                Console.WriteLine($"WebSocket Exception: {ex.Message}");
            }
        }


        public static void SetClientUserId(string clientId, string userId) { // link user id to a client id
            _clientUserIds[clientId] = userId;
        }

        public static string GetClientUserId(string clientId) { // get user id from a client
            if (_clientUserIds.TryGetValue(clientId, out string userId)) {
                return userId;
            }
            return null;
        }

        public static async void SendMessageToUser(string userId, string message) {
            if (_clientUserIds.ContainsValue(userId)) {
                KeyValuePair<string, string> clientUserPair = _clientUserIds.FirstOrDefault(pair => pair.Value == userId);
                if (_clientWebSockets.TryGetValue(clientUserPair.Key, out System.Net.WebSockets.WebSocket webSocket)) {

                    // Send Message

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    Console.WriteLine($"Sent {message}");

                } else {
                    Console.WriteLine($"No WebSocket found for user with ID: {userId}");
                }
            } else {
                Console.WriteLine($"No user found with ID: {userId}");
            }
        }
    }
}
