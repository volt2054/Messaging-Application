using System.Net.WebSockets;
using System.Net;
using System.Text;
using SharedLibrary;

namespace Server {
    public class WebSocketServer {
        private readonly HttpListener _httpListener;
        private readonly Func<string, string> _messageHandler;

        private static readonly Dictionary<string, WebSocket> _clientWebSockets = new Dictionary<string, WebSocket>();
        private static readonly Dictionary<string, string> _clientUserIds = new Dictionary<string, string>();

        public WebSocketServer(Func<string, string> messageHandler) {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://{WebSocketMetadata.IP_ADDRESS}:{WebSocketMetadata.PORT}/");
            _messageHandler = messageHandler;
        }

        public async Task StartAsync() {
            _httpListener.Start();

            while (true) {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest) {
                    HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);

                    WebSocket webSocket = webSocketContext.WebSocket;

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

        private async void HandleWebSocket(WebSocket webSocket, string clientID) {
            try {
                byte[] buffer = new byte[1024];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.CloseStatus.HasValue) {
                    string message = Encoding.ASCII.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received: {message}");

                    string requestID = message.Split(":")[0];

                    string messageDetails = message.Substring(requestID.Length + 1);


                    string responseMessage = _messageHandler(messageDetails);

                    responseMessage = requestID + ":" + responseMessage;

                    Console.WriteLine("SENT: " + responseMessage);
                    byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                Console.WriteLine($"Client {clientID} disconnected");

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                
                _clientWebSockets.Remove(clientID);
                _clientUserIds.Remove(clientID);
                

            } catch (WebSocketException ex) {
                Console.WriteLine($"WebSocket Exception: {ex.Message}");
            }
        }


        public static void SetClientUserId(string clientId, string userId) { // link user id to a client id

            if (_clientUserIds.ContainsValue(userId)) {
                string clientIdToRemove = _clientUserIds.FirstOrDefault(entry => entry.Value == userId).Key;

                _clientUserIds.Remove(clientIdToRemove);
            }

            _clientUserIds[clientId] = userId;
        }

        public static string GetClientUserId(string clientId) { // get user id from a client
            if (_clientUserIds.TryGetValue(clientId, out string userId)) {
                return userId;
            }
            return "-1";
        }

        public static async void SendMessageToUser(string[] args, string userIdToSendTo, string messageType) {
            if (_clientUserIds.ContainsValue(userIdToSendTo)) {
                KeyValuePair<string, string> clientUserPair = _clientUserIds.FirstOrDefault(pair => pair.Value == userIdToSendTo);
                if (_clientWebSockets.TryGetValue(clientUserPair.Key, out WebSocket webSocket)) {

                    string message = messageType;
                    foreach (string arg in args) { 
                        message += arg;
                        message += WebSocketMetadata.DELIMITER;
                    }

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    Console.WriteLine($"Notify Sent {message}");
                } else {
                    Console.WriteLine($"No WebSocket found for user with ID: {userIdToSendTo}");
                }
            } else {
                Console.WriteLine($"No user found with ID: {userIdToSendTo}");
            }
        }
    }
}
