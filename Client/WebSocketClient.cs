using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Windows;

namespace Client {
    class WebSocketClient {

        const string DELIMITER = "|< delimiter >|"; //TODO replace with something else
        //const int PORT = 7256;
        //const string IP_ADDRESS = "127.0.0.1";

        private static readonly string SERVER_URL = $"ws://127.0.0.1:7256";
        private static ClientWebSocket _webSocket;

        private static string clientID = "";


        public static async Task ConnectWebSocket() {
            _webSocket = new ClientWebSocket();
            Uri serverUri = new Uri(SERVER_URL);
            await _webSocket.ConnectAsync(serverUri, CancellationToken.None);

            clientID = await ReceiveClientID(_webSocket);
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
            MessageBox.Show("RECIEVE");
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

            try {
                string message = $"{clientID}{DELIMITER}{communicationType}";
                foreach (string datum in data) {
                    message += DELIMITER;
                    message += datum;
                }

                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                byte[] responseBytes = new byte[1024];
                WebSocketReceiveResult receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(responseBytes), CancellationToken.None);
                responseMessage = Encoding.ASCII.GetString(responseBytes, 0, receiveResult.Count);
                MessageBox.Show("CREATECOMM");
            } catch (Exception ex) {
                MessageBox.Show($"Error Occurred Creating WebSocket Communication: {ex.Message}");
            }
            return responseMessage;
        }


    }
}
