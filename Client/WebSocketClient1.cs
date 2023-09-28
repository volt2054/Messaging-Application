using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Windows;
using static SharedLibrary.WebSocket;
using SharedLibrary;

namespace Client {

    class WebSocketClient {

        Queue<string> responseMessages = new Queue<string>();
        private ClientWebSocket _webSocket;
        private string clientID;

        public WebSocketClient() {
            _webSocket = new ClientWebSocket();


        }

        private void Init() {
            ConnectWebSocket();
            StartListeningForServerMessages();
        }

        private static void OnMessageRecieved() {

        }

        private async Task StartListeningForServerMessages() {
            while (true) {
                byte[] buffer = new byte[1024];
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string responseMessage = Encoding.ASCII.GetString(buffer, 0, result.Count);
                responseMessages.Enqueue(responseMessage);
                OnMessageRecieved();
            }
        }

        private async Task ConnectWebSocket() {
            Uri serverUri = new Uri(SERVER_URL);
            await _webSocket.ConnectAsync(serverUri, CancellationToken.None);

            clientID = await ReceiveClientID(_webSocket); // Retrieve ClientID before listening to messages to make sure that we get the right ResponseMessage
        }

        private static async Task<string> ReceiveClientID(ClientWebSocket webSocket) {
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string clientId = Encoding.ASCII.GetString(buffer, 0, result.Count);
            return clientId;
        }

        private async Task<string> SendAndRecieve(string communicationType, string[] data) {
            string responseMessage = "-1"; // FAILED

            // Generate a UID for the request
            string requestId = Guid.NewGuid().ToString();

            try {
                string messageToSend = $"{requestId}:{clientID}{DELIMITER}{communicationType}";
                foreach (string datum in data) {
                    messageToSend += DELIMITER;
                    messageToSend += datum;
                }

                byte[] messageBytes = Encoding.ASCII.GetBytes(messageToSend);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None); // Send Request

                int attempts = 0;
                int maxAttempts = 10;
                while(true) {
                    attempts++;
                    foreach (var message in responseMessages) {
                        if (message.StartsWith(requestId)) {
                            responseMessage = responseMessage.Substring(requestId.Length + 1);
                            return responseMessage;
                        }
                    }
                    Thread.Sleep(1000 * attempts * attempts);

                    if (attempts > maxAttempts) {
                        return responseMessage; // -1
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show($"Error Occurred Creating WebSocket Communication: {ex.Message}");
            }
            return responseMessage;
        }

        public async Task CloseWebSocket() {
            try {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open) {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
                } else {
                    MessageBox.Show("No connection to close");
                }
            } catch (Exception ex) {
                MessageBox.Show($"Error during WebSocket closure: {ex.Message}");
            }
        }
    }

}
