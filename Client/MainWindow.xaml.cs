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

        static void createUser(string username, string email, string password) {
            try {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // Connect to local host
                int port = 7256;

                TcpClient client = new TcpClient(ipAddress.ToString(), port);
                MessageBox.Show($"Connected to server on {ipAddress}:{port}");

                NetworkStream stream = client.GetStream();
                string message = $"CREATE {username} {email} {password}";
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

        // TODO get ID from Username
        static string getID(string username) {
            throw new NotImplementedException();
        }

        static void deleteUser() {
            try {
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // Connect to local host
                int port = 7256;

                string id = getID("a");

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


        TextBox txt_Username = new TextBox();
        TextBox txt_Email = new TextBox();
        TextBox txt_Password = new TextBox();



        public MainWindow() {
            InitializeComponent();

            Grid gridRegister = new Grid();

            ColumnDefinition columnDefinitionMain = new ColumnDefinition();
            gridRegister.ColumnDefinitions.Add(columnDefinitionMain);

            RowDefinition rowDefinitionTitle = new RowDefinition();
            rowDefinitionTitle.Height = new GridLength(5, GridUnitType.Star);
            RowDefinition rowDefinitionUsername = new RowDefinition();
            rowDefinitionUsername.Height = new GridLength(1, GridUnitType.Star);
            RowDefinition rowDefinitionEmail = new RowDefinition();
            rowDefinitionEmail.Height = new GridLength(1, GridUnitType.Star); 
            RowDefinition rowDefinitionPassword = new RowDefinition();
            rowDefinitionPassword.Height = new GridLength(1, GridUnitType.Star);
            RowDefinition rowDefinitionRegisterButton = new RowDefinition();
            rowDefinitionRegisterButton.Height = new GridLength(2, GridUnitType.Star);

            gridRegister.RowDefinitions.Add(rowDefinitionTitle);
            gridRegister.RowDefinitions.Add(rowDefinitionUsername);
            gridRegister.RowDefinitions.Add(rowDefinitionEmail);
            gridRegister.RowDefinitions.Add(rowDefinitionPassword);
            gridRegister.RowDefinitions.Add(rowDefinitionRegisterButton);

            Label lab_Title = new Label();
            lab_Title.Content = "Messaging";
            lab_Title.FontSize = 36;
            lab_Title.Width = 200;
            lab_Title.VerticalAlignment = VerticalAlignment.Center;
            lab_Title.HorizontalContentAlignment = HorizontalAlignment.Center;

            Grid.SetRow(lab_Title, 0);

            txt_Username.Text = "Username";
            txt_Username.HorizontalAlignment = HorizontalAlignment.Center;
            txt_Username.VerticalAlignment = VerticalAlignment.Center;
            txt_Username.Width = 150;
            txt_Username.Height = 30;
            txt_Username.FontSize = 20;
            Grid.SetRow(txt_Username, 1);


            txt_Email.Text = "Email";
            txt_Email.HorizontalAlignment = HorizontalAlignment.Center;
            txt_Email.VerticalAlignment = VerticalAlignment.Center;
            txt_Email.Width = 150;
            txt_Email.Height = 30;
            txt_Email.FontSize = 20;
            Grid.SetRow(txt_Email, 2);

            txt_Password.Text = "Password";
            txt_Password.HorizontalAlignment = HorizontalAlignment.Center;
            txt_Password.VerticalAlignment = VerticalAlignment.Center;
            txt_Password.Width = 150;
            txt_Password.Height = 30;
            txt_Password.FontSize = 20;
            Grid.SetRow(txt_Password, 3);

            Button btn_Register = new Button();
            btn_Register.Content = "Register";
            btn_Register.Width = 150;
            btn_Register.Height = 50;
            btn_Register.FontSize = 30;

            Grid.SetRow(btn_Register, 4);

            gridRegister.Children.Add(lab_Title);
            gridRegister.Children.Add(txt_Username);
            gridRegister.Children.Add(txt_Email);
            gridRegister.Children.Add(txt_Password);
            gridRegister.Children.Add(btn_Register);
            


            btn_Register.Click += Btn_Register_Click;



            PrimaryWindow.Content = gridRegister;
        }


        private void Btn_Register_Click(object sender, RoutedEventArgs e) {
            createUser(txt_Username.Text, txt_Email.Text, txt_Password.Text);
        }
    }
}
