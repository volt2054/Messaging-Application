
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
using System.Net.Http;

namespace Client {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window {

        const int PORT = 7256;
        const string IP_ADDRESS = "127.0.0.1";
        string clientID;

        // C# doesnt support string enums
        public class TypeOfCommunication {
            public static readonly string SendMessage = "SEND"; // (SEND + MESSAGE CONTENT) RETURNS WHETHER SUCCESSFUL
            public static readonly string GetMessages = "GET"; // (GET + CHANNEL ID) RETURNS RECENTLY SENT MESSAGES
            public static readonly string GetID = "GETUSERID"; // (GETUSERID + USERNAME)  RETURNS ID GIVEN USERNAME
            public static readonly string RegisterUser = "CREATE"; // (CREATE + USERNAME + EMAIL + PASSWORD) RETURNS WHETHER SUCCESSFUL
            public static readonly string ValidateUser = "CHECK"; // (CHECK + USERNAME + PASSWORD) RETURNS WHETHER SUCCESSFUL
            public static readonly string DeleteUser = "DELETEUSER"; // (DELETE + USERID) RETURNS WHETHER SUCCESSFUL
        }

        // TODO Wrap network functions into a class (put in a seperate file asw)

        static string CreateCommunication(string communicationType, string data) {
            string responseMessage = "-1"; // FAILED
            try {
                TcpClient client = new(IP_ADDRESS, PORT);
                MessageBox.Show($"Connected to server on {IP_ADDRESS}:{PORT}");

                NetworkStream stream = client.GetStream();
                string message = $"{communicationType} {data}";
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);
                MessageBox.Show($"Sent message: {message}");

                byte[] responseBytes = new byte[1024];
                int bytesRead = stream.Read(responseBytes, 0, responseBytes.Length);
                responseMessage = Encoding.ASCII.GetString(responseBytes, 0, bytesRead);

            } catch (Exception ex) {
                MessageBox.Show($"An Error Occured: {ex.Message}");
            } finally {
                MessageBox.Show("Connection closed");
            }


            return responseMessage;
        }

        static string CreateUser(string username, string email, string password) {
            return CreateCommunication(TypeOfCommunication.RegisterUser, $"{username} {email} {password}");
        }

        static string GetID(string username) {
            return CreateCommunication(TypeOfCommunication.GetID, username);
        }

        static void DeleteUser(string userID) {
            CreateCommunication(TypeOfCommunication.DeleteUser, userID);
        }

        TextBox txt_Username = new TextBox();
        TextBox txt_Email = new TextBox();
        TextBox txt_Password = new TextBox();

        Grid gridLogin = new Grid();

        public MainWindow() {
            InitializeComponent();


            if (true) {

                

                RowDefinition rowDefinitionTitleLogin = new RowDefinition();
                rowDefinitionTitleLogin.Height = new GridLength(5, GridUnitType.Star);
                RowDefinition rowDefinitionUsernameLogin = new RowDefinition();
                rowDefinitionUsernameLogin.Height = new GridLength(1, GridUnitType.Star);
                RowDefinition rowDefinitionEmailLogin = new RowDefinition();
                rowDefinitionEmailLogin.Height = new GridLength(1, GridUnitType.Star);
                RowDefinition rowDefinitionPasswordLogin = new RowDefinition();
                rowDefinitionPasswordLogin.Height = new GridLength(1, GridUnitType.Star);
                RowDefinition rowDefinitionLoginButton = new RowDefinition();
                rowDefinitionLoginButton.Height = new GridLength(2, GridUnitType.Star);

                gridLogin.RowDefinitions.Add(rowDefinitionTitleLogin);
                gridLogin.RowDefinitions.Add(rowDefinitionUsernameLogin);
                gridLogin.RowDefinitions.Add(rowDefinitionEmailLogin);
                gridLogin.RowDefinitions.Add(rowDefinitionPasswordLogin);
                gridLogin.RowDefinitions.Add(rowDefinitionLoginButton);

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

                Grid.SetColumn(btn_Register, 0);

                Button btn_Login = new Button();
                btn_Login.Content = "Login";
                btn_Login.Width = 150;
                btn_Login.Height = 50;
                btn_Login.FontSize = 30;

                Grid.SetColumn(btn_Login, 1);

                gridLogin.Children.Add(lab_Title);
                gridLogin.Children.Add(txt_Username);
                gridLogin.Children.Add(txt_Email);
                gridLogin.Children.Add(txt_Password);
                

                btn_Register.Click += Btn_Register_Click;
                btn_Login.Click += Btn_Login_Click;

                Grid gridButtonOptions = new Grid();
                ColumnDefinition columnDefinitionLoginButton = new ColumnDefinition();
                ColumnDefinition columnDefinitionRegisterButton = new ColumnDefinition();

                RowDefinition rowDefintionButtons = new RowDefinition();

                gridButtonOptions.ColumnDefinitions.Add(columnDefinitionLoginButton);
                gridButtonOptions.ColumnDefinitions.Add(columnDefinitionRegisterButton);
                gridButtonOptions.RowDefinitions.Add(rowDefintionButtons);

                ColumnDefinition columnDefinitionLogin = new ColumnDefinition();
                gridLogin.ColumnDefinitions.Add(columnDefinitionLogin);

                Grid.SetRow(gridButtonOptions, 4);

                gridButtonOptions.Children.Add(btn_Login);
                gridButtonOptions.Children.Add(btn_Register);

                gridLogin.Children.Add(gridButtonOptions);

                PrimaryWindow.Content = gridLogin;

            } // So I can collapse code

        }

        private void Btn_Login_Click(object sender, RoutedEventArgs e) {
        }

        private void Btn_Register_Click(object sender, RoutedEventArgs e) {
            clientID = CreateUser(txt_Username.Text, txt_Email.Text, txt_Password.Text);
            if (clientID != null) {
                //PrimaryWindow.Content = gridMessages;
            }
        }
    }
}
