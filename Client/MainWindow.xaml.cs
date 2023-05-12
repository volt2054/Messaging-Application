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
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;

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


        //TODO SWITCH TO GRID
        public MainWindow() {
            InitializeComponent();

            int textBoxWidth = 120;
            int canvasWidth = 850;

            Label lab_Title = new Label();
            lab_Title.Content = "Messaging";
            lab_Title.FontSize = 36;
            lab_Title.Width = 200;

            Canvas.SetLeft(lab_Title, (canvasWidth - 200) / 2);
            Canvas.SetTop(lab_Title, 100);

            TextBox txt_Username = new TextBox();
            txt_Username.Text = "Username";
            txt_Username.HorizontalAlignment = HorizontalAlignment.Center;
            txt_Username.VerticalAlignment = VerticalAlignment.Center;
            txt_Username.Width = 120;

            Canvas.SetLeft(txt_Username, (canvasWidth - textBoxWidth) / 2);
            Canvas.SetTop(txt_Username, 185);


            TextBox txt_Email = new TextBox();
            txt_Email.Text = "Email";
            txt_Email.HorizontalAlignment = HorizontalAlignment.Center;
            txt_Email.VerticalAlignment = VerticalAlignment.Center;
            txt_Email.Width = 120;

            Canvas.SetLeft(txt_Email, (canvasWidth - textBoxWidth) / 2);
            Canvas.SetTop(txt_Email, 208);

            TextBox txt_Password = new TextBox();
            txt_Password.Text = "Password";
            txt_Password.HorizontalAlignment = HorizontalAlignment.Center;
            txt_Password.VerticalAlignment = VerticalAlignment.Center;
            txt_Password.Width = 120;

            Canvas.SetLeft(txt_Password, (canvasWidth - textBoxWidth) / 2);
            Canvas.SetTop(txt_Password, 231);

            Button btn_Register = new Button();
            btn_Register.Content = "Register";
            btn_Register.Width = 120;

            Canvas.SetLeft(btn_Register, (canvasWidth - textBoxWidth) / 2);
            Canvas.SetTop(btn_Register, 273);

            btn_Register.Click += Btn_Register_Click;

            MyCanvas.Children.Add(lab_Title);
            MyCanvas.Children.Add(txt_Username);
            MyCanvas.Children.Add(txt_Email);
            MyCanvas.Children.Add(txt_Password);
            MyCanvas.Children.Add(btn_Register);
        }


        private void Btn_Register_Click(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }
    }
}
