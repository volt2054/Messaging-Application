
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
using System.Windows.Threading;


using SharedLibrary;
using static Client.WebSocketClient;
using static SharedLibrary.Serialization;
using System.Net.WebSockets;

namespace Client {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public class SpecialServerIDs {
        public static readonly string DirectMessages = "-1";
    }
    public class SpecialChannelIDs {
        public static readonly string Friends = "-1";
    }

    public class Icons {
        public static readonly string Chat = "/images/chat.png";
        public static readonly string Friends = "/images/Friends.png";
    }



    public partial class MainWindow : Window {

        // TODO - Loading options from file??

        WebSocketClient Client;

        string CurrentUserID;
        string CurrentChannelID;
        string CurrentServerID = "-1";
        string NewestMessage = int.MinValue.ToString();
        string OldestMessage = int.MaxValue.ToString();


        TextBox txt_Username = new TextBox();
        TextBox txt_Email = new TextBox();
        TextBox txt_Password = new TextBox();

        Grid gridLogin = new Grid();

        Grid messagingGrid = new Grid();

        static async Task<string> CreateUser(string username, string email, string password, WebSocketClient Client) {
            string[] data = { username, email, password };
            string response = await Client.SendAndRecieve(TypeOfCommunication.RegisterUser, data);
            return response;
        }

        static async Task<string> VerifyUser(string username, string email, string password, WebSocketClient Client) {
            string[] data = { username, email, password };
            string response =await Client.SendAndRecieve(TypeOfCommunication.ValidateUser, data);
            return response;
        }

        static async Task<string> GetID(string username, WebSocketClient Client) {
            string[] data = { username };
            string response = await Client.SendAndRecieve(TypeOfCommunication.GetID, data);
            return response;
        }

        static async void SendMessage(string message, string channelID, WebSocketClient Client) {
            string[] data = { message, channelID };
            await Client.SendAndRecieve(TypeOfCommunication.SendMessage, data);
        }

        static async Task<string> CreateDMChannel(string user, WebSocketClient Client) {
            string[] data = { user };
            string response = await Client.SendAndRecieve(TypeOfCommunication.CreateDMChannel, data);
            return response;
        }

        static async Task<List<string[]>> FetchDMs(WebSocketClient Client) {
            string[] data = { };
            string response = await Client.SendAndRecieve(TypeOfCommunication.FetchChannels, data);

            if (response == "-1") {
                return new List<string[]>();
            }

            byte[] dataBytes = Convert.FromBase64String(response);
            List<string[]> userChannels = DeserializeList<string[]>(dataBytes);


            return userChannels;
        }

        static async Task<List<string[]>> FetchChannels( string serverID, WebSocketClient Client) {
            string[] data = { serverID };
            string response = await Client.SendAndRecieve(TypeOfCommunication.FetchChannels, data);
            if (response == "-1") {
                return new List<string[]>();
            }
            byte[] dataBytes = Convert.FromBase64String(response);
            List<string[]> userChannels = DeserializeList<string[]>(dataBytes);

            return userChannels;
        }

        static async Task<List<string[]>> FetchMessages(string channelID, string messageID, string before, WebSocketClient Client) {
            string[] data = { channelID, messageID, before };
            string response = await Client.SendAndRecieve(TypeOfCommunication.FetchMessages, data);

            if (response == "-1") {
                return new List<string[]>();
            }

            byte[] dataBytes = Convert.FromBase64String(response);
            List<string[]> messageList = DeserializeList<string[]>(dataBytes);

            return messageList;

        }

        public MainWindow() {
            InitializeComponent();

            Client = new WebSocketClient();

            InitializeLoginUI();
        }
        
        // Make sure websocket is closed
        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e) {
            await Client.CloseWebSocket();

            base.OnClosing(e);
        }






        private void AddServerIcon(StackPanel parentStackPanel, Color color, string serverID) {
            Ellipse ServerIcon = new Ellipse {
                Width = 50,
                Height = 50,
                Fill = new SolidColorBrush(color),
                Tag = serverID
            };

            ServerIcon.MouseLeftButtonDown += ServerIcon_Click;


            parentStackPanel.Children.Add(ServerIcon);
        }

        private async void ServerIcon_Click(object sender, MouseButtonEventArgs e) {
            Ellipse ServerIcon = sender as Ellipse;
            string Tag = ServerIcon.Tag as string;

            channeListStackPanel.Children.Clear();

            CurrentServerID = Tag;

            if (Tag == SpecialServerIDs.DirectMessages) {

                // FRIENDS CHANNEL
                AddChannel(channeListStackPanel, "Friends", "-1");


                foreach (string[] channel in await FetchDMs(Client)) {
                    AddChannel(channeListStackPanel, channel[1], channel[0]);
                }
            } else {
                foreach (string[] channel in await FetchChannels(CurrentServerID, Client)) {
                    AddChannel(channeListStackPanel, channel[1], channel[0]);
                }
            }
            
            
        }

        private void AddChannel(StackPanel parentStackPanel, string iconText, string channelID) {

            string iconPath;

            switch (channelID) {

                case "-1":
                    iconPath = Icons.Friends;
                    break;
                default:
                    iconPath = Icons.Chat;
                    break;
            }


            StackPanel ChannelElement = new StackPanel {
                Orientation = Orientation.Horizontal,
                Tag = channelID
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

            ChannelElement.MouseLeftButtonDown += ChannelElement_MouseLeftButtonDown;

            ChannelElement.Children.Add(icon);
            ChannelElement.Children.Add(textBlock);

            parentStackPanel.Children.Add(ChannelElement);
        }

        private async void ChannelElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            StackPanel ChannelElement = sender as StackPanel;
            CurrentChannelID = ChannelElement.Tag as string;

            messageStackPanel.Children.Clear();
            OldestMessage = int.MinValue.ToString();
            OldestMessage = int.MaxValue.ToString();

            foreach (string[] message in await FetchMessages(CurrentChannelID, OldestMessage, "true", Client)) {
                messageScrollViewer.ScrollToEnd();
                if (Convert.ToInt32(NewestMessage) < Convert.ToInt32(message[2])) { NewestMessage = message[2]; }
                if (Convert.ToInt32(OldestMessage) > Convert.ToInt32(message[2])) { OldestMessage = message[2]; }
                AddMessage(messageStackPanel, Colors.Black, message[0], message[1], true);
                messageScrollViewer.ScrollToBottom();
            }
        }
        private void AddMessages() {
            while (true) {
                Client.GetNextMessage();
            }
        }

        public void AddMessage(string channelID, string username, string messageContent) {
            if (CurrentChannelID  == channelID) {
                AddMessage(messageStackPanel, Color.FromRgb(0, 0, 0), username, messageContent, false);
            }
        }

        private void AddMessage(StackPanel parentStackPanel, Color color, string username, string message, bool before) {

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
                Width = parentStackPanel.Width - 20
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

            if (before) {
                double newContentHeight = 50; // TODO FETCH NEW CONTENT HEIGHT
                double currentVerticalOffset = messageScrollViewer.VerticalOffset;
                parentStackPanel.Children.Insert(0, messageStackPanel);
                messageScrollViewer.ScrollToVerticalOffset(currentVerticalOffset + newContentHeight);
            } else { parentStackPanel.Children.Add(messageStackPanel); messageScrollViewer.ScrollToEnd(); }
        }


        StackPanel channeListStackPanel;
        StackPanel messageStackPanel;
        ScrollViewer messageScrollViewer;

        private void InitializeLoginUI() {
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

            Content = gridLogin;
        }

        private async void Btn_Login_Click(object sender, RoutedEventArgs e) {
            CurrentUserID = await VerifyUser(txt_Username.Text, txt_Email.Text, txt_Password.Text, Client);

            if (CurrentUserID != "Bad Password") {
                InitializeMessagingUI();
            } else {
                MessageBox.Show("Bad Password");
            }

        }

        private async void Btn_Register_Click(object sender, RoutedEventArgs e) {
            CurrentUserID = await CreateUser(txt_Username.Text, txt_Email.Text, txt_Password.Text, Client);
            if (CurrentUserID != null) {
                InitializeMessagingUI();
            }
        }

        private async void InitializeMessagingUI() {
            Content = messagingGrid;

            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });

            // First Column: Circles
            StackPanel circleStackPanel = new StackPanel();
            ScrollViewer circleScrollViewer = new ScrollViewer() { Content = circleStackPanel };
            messagingGrid.Children.Add(circleScrollViewer);
            Grid.SetColumn(circleScrollViewer, 0);

            AddServerIcon(circleStackPanel, Colors.Black, SpecialServerIDs.DirectMessages); // This is where we will access DMs from

            // Second Column: Boxes with Icons and Text
            channeListStackPanel = new StackPanel();
            ScrollViewer boxScrollViewer = new ScrollViewer() { Content = channeListStackPanel };
            messagingGrid.Children.Add(boxScrollViewer);
            Grid.SetColumn(boxScrollViewer, 1);

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

            foreach (string[] channel in await FetchDMs(Client)) {
                AddChannel(channeListStackPanel, channel[1], channel[0]);
            }

            foreach (string[] message in await FetchMessages(CurrentChannelID, OldestMessage, "true", Client)) {
                messageScrollViewer.ScrollToEnd();
                if (Convert.ToInt32(NewestMessage) < Convert.ToInt32(message[2])) { NewestMessage = message[2]; }
                if (Convert.ToInt32(OldestMessage) > Convert.ToInt32(message[2])) { OldestMessage = message[2]; }
                AddMessage(messageStackPanel, Colors.Black, message[0], message[1], true);
                messageScrollViewer.ScrollToBottom();
            }
        }

        private async void MessageFetchTimer_Tick(object? sender, EventArgs e) {
            foreach (string[] message in await FetchMessages(CurrentChannelID, NewestMessage, "false", Client)) {
                NewestMessage = message[2];
                AddMessage(messageStackPanel, Colors.Black, message[0], message[1], false);
            }
        }

        private async void MessageScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (e.VerticalOffset == 0) {
                foreach (string[] message in await FetchMessages(CurrentChannelID, OldestMessage, "true", Client)) {
                    if (Convert.ToInt32(OldestMessage) > Convert.ToInt32(message[2])) { OldestMessage = message[2]; }
                    AddMessage(messageStackPanel, Colors.Black, message[0], message[1], true);
                }
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                TextBox textBox = sender as TextBox;
                if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text)) {
                    SendMessage(textBox.Text, CurrentChannelID, Client);
                    textBox.Clear();
                }
            }
        }
    }
}


