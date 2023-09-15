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



// TODO SPLIT INTO MESSAGE HANDLER AND COMMUNICATION HANDLER
// 2 SEPERATE WEBSOCKETS ONE FOR RECIEVING MESSAGES FROM THE SERVER AND ANOTHER FOR SENDING AND RECIEVING MESSAGES FROM THE SERVER



namespace Client {

    class WebSocketClient {

        Queue<string> responseMessages = new Queue<string>();
        private ClientWebSocket _webSocket;
        private string clientID;

        public WebSocketClient() {
            _webSocket = new ClientWebSocket();


        }

        private void init() {
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
                //TODO SERVER ADD REQUEST ID

            } catch (Exception ex) {
                MessageBox.Show($"Error Occurred Creating WebSocket Communication: {ex.Message}");
            }
            return responseMessage;

        }



    }


    class WebSocketClient1 {

        const string DELIMITER = "|< delimiter >|"; //TODO replace with something else
        //const int PORT = 7256;
        //const string IP_ADDRESS = "127.0.0.1";

        private static readonly string SERVER_URL = $"ws://127.0.0.1:7256";
        private static ClientWebSocket _webSocket;

        private static string clientID = "";

        static Queue<string> responseMessages = new Queue<string>();


        private static async Task StartListeningForMessages() {
            while (true) {
                byte[] buffer = new byte[1024];
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string responseMessage = Encoding.ASCII.GetString(buffer, 0, result.Count);
                responseMessages.Enqueue(responseMessage);
            }
        }

        public static async Task ConnectWebSocket() {
            _webSocket = new ClientWebSocket();
            Uri serverUri = new Uri(SERVER_URL);
            await _webSocket.ConnectAsync(serverUri, CancellationToken.None);

            clientID = await ReceiveClientID(_webSocket);
            StartListeningForMessages();
        }

        private static async Task<string> ReceiveClientID(ClientWebSocket webSocket) {
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string clientId = Encoding.ASCII.GetString(buffer, 0, result.Count);
            return clientId;
        }

        public static async Task<string> RecieveMessage() {
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string message = Encoding.ASCII.GetString(buffer, 0, result.Count);
            return message;
        }

        public static async Task CloseWebSocket() {
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

        public static async Task<string> CreateCommunication(string communicationType, string[] data) {
            string responseMessage = "-1"; // FAILED

            // Generate a UID for the request
            string requestId = Guid.NewGuid().ToString();

            try {
                string message = $"{requestId}:{clientID}{DELIMITER}{communicationType}";
                foreach (string datum in data) {
                    message += DELIMITER;
                    message += datum;
                }

                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                byte[] responseBytes = new byte[1024];
                WebSocketReceiveResult receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(responseBytes), CancellationToken.None);
                responseMessage = Encoding.ASCII.GetString(responseBytes, 0, receiveResult.Count);

                //TODO SERVER ADD REQUEST ID
                //responseMessage = responseMessage.Substring(requestId.Length + 1);


            } catch (Exception ex) {
                MessageBox.Show($"Error Occurred Creating WebSocket Communication: {ex.Message}");
            }
            return responseMessage;

        }


    }
}
