using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Windows;
using static SharedLibrary.WebSocketMetadata;
using System.Collections.Concurrent;
using SharedLibrary;

namespace Client {

    class WebSocketClient {

        // Add a dictionary for responses instead
        // USE QUEUE FOR MESSAGES.
        BlockingCollection<string> messageQueue = new BlockingCollection<string>();

        public string GetNextMessage() {
            return messageQueue.Take();
        }

        ConcurrentQueue<string> responseMessages = new ConcurrentQueue<string>();
        private ClientWebSocket _webSocket;
        private string clientID;

        private static TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        public WebSocketClient() {
            _webSocket = new ClientWebSocket();
            Init();
        }

        public async void Init() {
            await ConnectWebSocket();
            StartListeningForServerMessages();
        }

        private void OnMessageReceived(string message) {
            _taskCompletionSource.TrySetResult(true);
            
            if (message.StartsWith(TypeOfCommunication.NotifyMessage) || message.StartsWith(TypeOfCommunication.NotifyChannel) || message.StartsWith(TypeOfCommunication.NotifyServer) || message.StartsWith(TypeOfCommunication.NotifyAttachment)) { // disgusting TODO FIX
                messageQueue.Add(message);
            } else {
                responseMessages.Enqueue(message);
            }
        }

        private void StartListeningForServerMessages() {

            Task.Run(async () => {
                while (_webSocket.State == WebSocketState.Open) {
                    byte[] buffer = new byte[16384];
                    WebSocketReceiveResult result;
                    do {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Text) {
                            string message = Encoding.ASCII.GetString(buffer, 0, result.Count);
                            OnMessageReceived(message);
                        } else if (result.MessageType == WebSocketMessageType.Close) {
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        }
                    } while (!result.EndOfMessage);
                }
            });

            
        }


        private async Task ConnectWebSocket() {
            Uri serverUri = new Uri(WebSocketMetadata.SERVER_URL);
            await _webSocket.ConnectAsync(serverUri, CancellationToken.None);

            clientID = await ReceiveClientID(_webSocket); // Retrieve ClientID before listening to messages to make sure that we get the right ResponseMessage
        }

        private static async Task<string> ReceiveClientID(ClientWebSocket webSocket) {
            byte[] buffer = new byte[16384];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string clientId = Encoding.ASCII.GetString(buffer, 0, result.Count);
            return clientId;
        }

        public async Task<string> SendAndRecieve(string communicationType, string[] data) {
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

                bool found = false;
                while (found == false) {
                    await _taskCompletionSource.Task;
                    foreach (string message in responseMessages) {
                        if (message.StartsWith(requestId)) {
                            responseMessage = message.Substring(requestId.Length + 1);
                            _taskCompletionSource.TrySetResult(false);
                            return responseMessage;
                        }
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
                    // No connection to close
                }
            } catch (Exception ex) {
                //MessageBox.Show($"Error during WebSocket closure: {ex.Message}");
            }
        }
    }

}
