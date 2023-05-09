using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Client {


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private static TcpClient client;

        static void createUser() {
            try {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // Connect to local host
                int port = 7256;

                TcpClient client = new TcpClient(ipAddress.ToString(), port);
                MessageBox.Show($"Connected to server on {ipAddress}:{port}");

                NetworkStream stream = client.GetStream();
                string message = "CREATE testttusername testemailll testpasswordd";
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);
                MessageBox.Show($"Sent message: {message}");

                byte[] responseBytes = new byte[1024];
                int bytesRead = stream.Read(responseBytes, 0, responseBytes.Length);
                string responseMessage = Encoding.ASCII.GetString(responseBytes, 0, bytesRead);
                MessageBox.Show($"Recieved response {responseMessage}");
            } catch (Exception ex) {
                MessageBox.Show($"An Error Occured: {ex.Message}");
            } finally {
                MessageBox.Show("Connection closed");
            }
        }

        static string getID() {
            return "1";
        }

        static void deleteUser() {
            try {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // Connect to local host
                int port = 7256;

                string id = getID();

                TcpClient client = new TcpClient(ipAddress.ToString(), port);
                MessageBox.Show($"Connected to server on {ipAddress}:{port}");

                NetworkStream stream = client.GetStream();

                string message = $"DELETE {id}";
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);

                byte[] responseBytes = new byte[1024];
                int bytesRead = stream.Read(responseBytes, 0, responseBytes.Length);
                string responseMessage = Encoding.ASCII.GetString(responseBytes, 0, bytesRead);
                MessageBox.Show($"Recieved response {responseMessage}");
            } catch (Exception ex) {
                MessageBox.Show($"An Error Occured: {ex.Message}");

            } finally {
                MessageBox.Show("Connection closed");
            }
        }

        public MainWindow() {
            InitializeComponent();

            TextBox txt_Username = new TextBox();
            TextBox txt_Email = new TextBox();
            TextBox txt_Password = new TextBox();

            Button btn_Register = new Button();
        }
    }
}
