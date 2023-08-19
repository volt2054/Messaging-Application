
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

using SharedLibrary;
using static SharedLibrary.Serialization;

namespace Client {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 



    public partial class MainWindow : Window {

        // TODO - Loading options from file??
        const string DELIMITER = "|< delimiter >|"; //TODO replace with something else

        const int PORT = 7256;
        const string IP_ADDRESS = "127.0.0.1";
        string clientID;

        string channelID;
        string serverID;

        

        // TODO Wrap network functions into a class (put in a seperate file asw)

        static string CreateCommunication(string communicationType, string[] data) {
            string responseMessage = "-1"; // FAILED
            try {
                TcpClient client = new(IP_ADDRESS, PORT);
                MessageBox.Show($"Connected to server on {IP_ADDRESS}:{PORT}");

                NetworkStream stream = client.GetStream();
                string message = $"{communicationType}";
                for (int i = 0; i<data.Length; i++) {
                    message += DELIMITER;
                    message += data[i];
                }
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);
                MessageBox.Show($"Sent message: {message}");

                byte[] responseBytes = new byte[1024];
                int bytesRead = stream.Read(responseBytes, 0, responseBytes.Length);
                responseMessage = Encoding.ASCII.GetString(responseBytes, 0, bytesRead);

                MessageBox.Show(responseMessage);

            } catch (Exception ex) {
                MessageBox.Show($"Error Occured Creating Communication: {ex.Message}");
            } finally {
                MessageBox.Show("Connection closed");
            }


            return responseMessage;
        }

        static string CreateUser(string username, string email, string password) {
            string[] data = { username, email, password };
            return CreateCommunication(TypeOfCommunication.RegisterUser, data);
        }

        static string VerifyUser(string username, string email, string password) {
            string[] data = { username, email, password };
            return CreateCommunication(TypeOfCommunication.ValidateUser, data);
        }

        static string GetID(string username) {
            string[] data = { username };
            return CreateCommunication(TypeOfCommunication.GetID, data);
        }

        static void DeleteUser(string userID) {
            string[] data = { userID };
            CreateCommunication(TypeOfCommunication.DeleteUser, data);
        }

        static void SendMessage(string message, string channelID, string clientID) {
            string[] data = { message, channelID, clientID };
            CreateCommunication(TypeOfCommunication.SendMessage, data);
        }

        static string CreateDMChannel(string user1, string user2) {
            string[] data = { user1, user2 };
            return CreateCommunication(TypeOfCommunication.CreateDMChannel, data);
        }

        static List<string> FetchChannels(string userID) {
            string[] data = { userID };
            string response = CreateCommunication(TypeOfCommunication.FetchChannels, data);
            byte[] dataBytes = Convert.FromBase64String(response);
            List<string> userChannels = DeserializeList(dataBytes);

            foreach (string channel in userChannels) {
                MessageBox.Show("User is in channel: " + channel);
            }

            return userChannels;
        }

        static List<string> FetchMessages(string channelID, string messageID) {

            string[] data = { channelID, messageID };
            string response = CreateCommunication(TypeOfCommunication.FetchMessages, data);
            byte[] dataBytes = Convert.FromBase64String(response);
            List<string> messageList = DeserializeList(dataBytes);

            return messageList;

        }

        TextBox txt_Username = new TextBox();
        TextBox txt_Email = new TextBox();
        TextBox txt_Password = new TextBox();

        Grid gridLogin = new Grid();

        Grid messagingGrid = new Grid();


        public MainWindow() {
            InitializeComponent();

            

            string user1 = CreateUser("testuser1", "testemail1", "testpassword1");
            string user2 = CreateUser("testuser2", "testemail2", "testpassword2");

            string channel = CreateDMChannel(user1, user2);
            channelID = channel;

            SendMessage("test message", channel, user1);
            SendMessage("test message2", channel, user2);


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

            } // So I can collapse code // MAIN LOGIN SCREEN

        }

        private void Btn_Login_Click(object sender, RoutedEventArgs e) {
            clientID = VerifyUser(txt_Username.Text, txt_Email.Text, txt_Password.Text);
            PrimaryWindow.Content = messagingGrid;
            InitializeUI();
        }

        private void Btn_Register_Click(object sender, RoutedEventArgs e) {
            PrimaryWindow.Content = messagingGrid;
            InitializeUI();


            //clientID = CreateUser(txt_Username.Text, txt_Email.Text, txt_Password.Text);
            //if (clientID != null) {
            //PrimaryWindow.Content = gridMessages;
            //}
        }

        private void AddServerIcon(StackPanel parentStackPanel, Color color) {
            Ellipse ellipse = new Ellipse {
                Width = 50,
                Height = 50,
                Fill = new SolidColorBrush(color)
            };

            parentStackPanel.Children.Add(ellipse);
        }

        private void AddChatElement(StackPanel parentStackPanel, string iconPath, string iconText) {

            StackPanel stackPanel = new StackPanel {
                Orientation = Orientation.Horizontal
            };

            Image icon = new Image {
                Source = new BitmapImage(new Uri(iconPath, UriKind.Relative)),
                Width = 24,
                Height = 24
            };

            TextBlock textBlock = new TextBlock {
                Text = iconText,
                Margin = new Thickness(10, 0, 0, 0)
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textBlock);

            parentStackPanel.Children.Add(stackPanel);
        }

        private void AddMessage(StackPanel parentStackPanel, Color color, string username, string message) {

            StackPanel messageStackPanel = new StackPanel {
                Orientation = Orientation.Horizontal
            };

            Ellipse ellipse = new Ellipse {
                Width = 25,
                Height = 25,
                Fill = new SolidColorBrush(color)
            };

            StackPanel usernameAndMessageStackPanel = new StackPanel {
                Orientation = Orientation.Vertical,
                Width = parentStackPanel.ActualWidth - 20
            };

            TextBlock usernameTextBlock = new TextBlock {
                Text = username,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 0, 0, 3)
            };

            TextBlock messageTextBlock = new TextBlock {
                Text = message,
                Margin = new Thickness(5, 0, 0, 10),
                MaxHeight = 100,
                TextWrapping = TextWrapping.Wrap
            };

            usernameAndMessageStackPanel.Children.Add(usernameTextBlock);
            usernameAndMessageStackPanel.Children.Add(messageTextBlock);

            messageStackPanel.Children.Add(ellipse);
            messageStackPanel.Children.Add(usernameAndMessageStackPanel);

            parentStackPanel.Children.Add(messageStackPanel);
            //parentStackPanel.Children.Insert(0, messageStackPanel); THIS ADDS AT BEGINNING USE LATER WHEN FETCHING MESSAGES

        }

        StackPanel messageStackPanel;
        ScrollViewer messageScrollViewer;

        private void InitializeUI() {
            Content = messagingGrid;

            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });

            // First Column: Circles
            StackPanel circleStackPanel = new StackPanel();
            ScrollViewer circleScrollViewer = new ScrollViewer() { Content = circleStackPanel };
            messagingGrid.Children.Add(circleScrollViewer);
            Grid.SetColumn(circleScrollViewer, 0);

            AddServerIcon(circleStackPanel, Colors.Blue);
            AddServerIcon(circleStackPanel, Colors.Red);

            // Second Column: Boxes with Icons and Text
            StackPanel boxStackPanel = new StackPanel();
            ScrollViewer boxScrollViewer = new ScrollViewer() { Content = boxStackPanel };
            messagingGrid.Children.Add(boxScrollViewer);
            Grid.SetColumn(boxScrollViewer, 1);

            AddChatElement(boxStackPanel, "/images/icon.png", "Text");
            AddChatElement(boxStackPanel, "/images/icon.png", "Text");


            // Third Column: Message Container with Text Box
            messageStackPanel = new StackPanel();
            messageStackPanel.VerticalAlignment = VerticalAlignment.Bottom;

            TextBox messageBox = new TextBox {
                Height = 30,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            messageBox.KeyDown += TextBox_KeyDown;

            messageScrollViewer = new ScrollViewer() {
                Content = messageStackPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            messageScrollViewer.ScrollChanged += MessageScrollViewer_ScrollChanged;


            Grid messageGrid = new Grid();
            messageGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(9, GridUnitType.Star) }); // Messages
            messageGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // TextBox
            messageGrid.Children.Add(messageScrollViewer);
            messageGrid.Children.Add(messageBox);

            Grid.SetRow(messageScrollViewer, 0);
            Grid.SetRow(messageBox, 1);

            messagingGrid.Children.Add(messageGrid);
            Grid.SetColumn(messageGrid, 2);

            foreach (string row in FetchMessages(channelID, "2000")) {
                AddMessage(messageStackPanel, Colors.Black, "test:", row);

            }
        }

        private void MessageScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (e.VerticalOffset == 0) {
                // Load earlier messages
            }
            
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                TextBox textBox = sender as TextBox;
                if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text)) {
                    AddMessage(messageStackPanel, Colors.Black, "You", textBox.Text);
                    SendMessage(textBox.Text, "0", clientID);
                    messageScrollViewer.ScrollToEnd();
                    textBox.Clear();
                }
            }
        }
    }
}
