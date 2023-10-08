
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
using System.Threading;
using System.DirectoryServices.ActiveDirectory;
using System.Diagnostics.Eventing.Reader;

namespace Client {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public class SpecialServerIDs {
        public static readonly string DirectMessages = "-1";
        public static readonly string CreateServer = "-2";
    }
    public class SpecialChannelIDs {
        public static readonly string Friends = "-1";
        public static readonly string CreateChannel = "-2";
        public static readonly string NotMade = "-3";
    }

    public class Icons {
        public static readonly string Chat = "/images/chat.png";
        public static readonly string Friends = "/images/friends.png";
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

        static async Task<string> CreateUser(string username, string email, string password, WebSocketClient Client) {
            string[] data = { username, email, password };
            string response = await Client.SendAndRecieve(TypeOfCommunication.RegisterUser, data);
            return response;
        }

        static async Task<string> VerifyUser(string username, string email, string password, WebSocketClient Client) {
            string[] data = { username, email, password };
            string response = await Client.SendAndRecieve(TypeOfCommunication.ValidateUser, data);
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

        static async Task<List<string[]>> FetchChannels(string serverID, WebSocketClient Client) {
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

        static async Task<List<string>> FetchFriends(WebSocketClient Client) {
            string[] data = { };
            string response = await Client.SendAndRecieve(TypeOfCommunication.GetFriends, data);

            if (response == "-1") {
                return new List<string>();
            }

            byte[] dataBytes = Convert.FromBase64String(response);
            List<string> friendsList = DeserializeList<string>(dataBytes);

            return friendsList;

        }

        public MainWindow() {
            InitializeComponent();

            //Client = new WebSocketClient();

            InitializeCreateServerUI();
        }

        // Make sure websocket is closed
        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e) {
            await Client.CloseWebSocket();

            base.OnClosing(e);
        }






        private void AddServerIcon(StackPanel parentStackPanel, Color backgroundColour, Color foregroundColour, string serverID, string text) {

            Grid ServerIcon = new Grid();

            Ellipse ServerBackground = new Ellipse {
                Width = 50,
                Height = 50,
                Fill = new SolidColorBrush(backgroundColour),
                Tag = serverID
            };


            Label label = new Label {
                Content = text,
                Width = 50,
                Height = 50,
                Foreground = new SolidColorBrush(foregroundColour),
                FontSize = 50 / text.Length,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,

            };

            ServerIcon.Children.Add(ServerBackground);
            ServerIcon.Children.Add(label);

            ServerIcon.MouseLeftButtonDown += ServerIcon_Click;


            parentStackPanel.Children.Add(ServerIcon);
        }

        private async void ServerIcon_Click(object sender, MouseButtonEventArgs e) {
            Grid ServerGrid = sender as Grid;
            Ellipse ServerIcon = ServerGrid.Children[0] as Ellipse;
            string Tag = ServerIcon.Tag as string;

            channelListStackPanel.Children.Clear();

            CurrentServerID = Tag;

            if (Tag == SpecialServerIDs.DirectMessages) {

                // FRIENDS CHANNEL
                AddChannel(channelListStackPanel, "Friends", "-1");

                foreach (string[] channel in await FetchDMs(Client)) {
                    AddChannel(channelListStackPanel, channel[1], channel[0]);
                }
            } else if (Tag == SpecialServerIDs.CreateServer) {
                // CREATE SERVER UI
                InitializeCreateServerUI();
            } else {
                foreach (string[] channel in await FetchChannels(CurrentServerID, Client)) {
                    AddChannel(channelListStackPanel, channel[1], channel[0]);
                }
            }
        }

        private void InitializeCreateServerUI() {
            Grid mainGrid = new Grid {
                Name = "MainGrid",
                Margin = new Thickness(30)
            };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            // ServerOptionsGrid
            Border serverOptionsBorder = new Border {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 25, 0)
            };

            Grid serverOptionsGrid = new Grid {
                Name = "ServerOptionsGrid"
            };
            serverOptionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            serverOptionsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // First row of ServerOptionsGrid
            StackPanel stackPanel1 = new StackPanel {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel1.Children.Add(new Label { Content = "Name:" });
            stackPanel1.Children.Add(new TextBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) });
            stackPanel1.Children.Add(new Label { Content = "Description:", Margin = new Thickness(10, 0, 0, 0) });
            stackPanel1.Children.Add(new TextBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) });

            // Second row of ServerOptionsGrid
            Grid channelGrid = new Grid {
                Name = "channelgrid"
            };
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ScrollViewer in the first column of channelgrid
            Border scrollViewerBorder = new Border {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(20)
            };

            ScrollViewer scrollViewer = new ScrollViewer {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            StackPanel scrollViewerContent = new StackPanel();

            scrollViewer.Content = scrollViewerContent;

            AddChannel(scrollViewerContent, "test", "-3");

            scrollViewerBorder.Child = scrollViewer;

            // Textbox and Button in the second column of channelgrid
            StackPanel stackPanel2 = new StackPanel {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };
            stackPanel2.Children.Add(new TextBox());
            stackPanel2.Children.Add(new Button { Content = "Add Channel", Margin = new Thickness(0, 5, 0, 0) });

            channelGrid.Children.Add(scrollViewerBorder);
            Grid.SetColumn(scrollViewerBorder, 0);
            channelGrid.Children.Add(stackPanel2);
            Grid.SetColumn(stackPanel2, 1);

            serverOptionsGrid.Children.Add(stackPanel1);
            Grid.SetRow(stackPanel1, 0);
            serverOptionsGrid.Children.Add(channelGrid);
            Grid.SetRow(channelGrid, 1);

            serverOptionsBorder.Child = serverOptionsGrid;

            // ScrollViewer in the second column
            Border scrollViewer2Border = new Border {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10)
            };

            ScrollViewer scrollViewer2 = new ScrollViewer {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            StackPanel scrollViewer2Content = new StackPanel();

            scrollViewer2.Content = scrollViewer2Content;
            scrollViewer2Border.Child = scrollViewer2;

            AddFriendElement("test", false, scrollViewer2Content);

            Grid.SetColumn(serverOptionsBorder, 0);
            Grid.SetColumn(scrollViewer2Border, 1);

            mainGrid.Children.Add(serverOptionsBorder);
            mainGrid.Children.Add(scrollViewer2Border);

            // Set the mainGrid as the content of the Window
            this.Content = mainGrid;
        }

        private void AddChannel(StackPanel parentStackPanel, string channelName, string channelID) {

            string iconPath;

            if (channelID == SpecialChannelIDs.Friends) {
                iconPath = Icons.Friends;
            } else {
                iconPath = Icons.Chat;
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
                Text = channelName,
                Margin = new Thickness(10, 0, 0, 0)
            };

            if (channelID != SpecialChannelIDs.NotMade) {
                ChannelElement.MouseLeftButtonDown += ChannelElement_MouseLeftButtonDown;
            }

            ChannelElement.Children.Add(icon);
            ChannelElement.Children.Add(textBlock);

            parentStackPanel.Children.Add(ChannelElement);
        }

        private async void ChannelElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            StackPanel ChannelElement = sender as StackPanel;
            CurrentChannelID = ChannelElement.Tag as string;

            if (CurrentChannelID == "-1") {
                InitializeFriendsUI();
            } else {
                messageStackPanel.Children.Clear();
                OldestMessage = int.MinValue.ToString();
                NewestMessage = int.MaxValue.ToString();

                foreach (string[] message in await FetchMessages(CurrentChannelID, OldestMessage, "false", Client)) {
                    messageScrollViewer.ScrollToEnd();
                    if (Convert.ToInt32(NewestMessage) < Convert.ToInt32(message[2])) { NewestMessage = message[2]; }
                    if (Convert.ToInt32(OldestMessage) > Convert.ToInt32(message[2])) { OldestMessage = message[2]; }
                    AddMessage(messageStackPanel, Colors.Black, message[0], message[1], true);
                    messageScrollViewer.ScrollToBottom();
                }
            }
        }

        private void ListenForMessages() {
            SynchronizationContext uiContext = SynchronizationContext.Current;

            Task.Run(() => {
                while (true) {
                    string message = Client.GetNextMessage();

                    HandleServerMessage(message, uiContext);

                    message = message.Substring(TypeOfCommunication.NotifyMessage.Length);

                    string[] args = message.Split(WebSocketMetadata.DELIMITER);
                }
            });
        }

        private void HandleServerMessage(string message, SynchronizationContext uiContext) {


            if (message.StartsWith(TypeOfCommunication.NotifyMessage)) {
                message = message.Substring(TypeOfCommunication.NotifyMessage.Length);
                string[] args = message.Split(WebSocketMetadata.DELIMITER);

                uiContext.Post(_ => AddMessage(args[0], args[1], args[2]), null);
            } else if (message.StartsWith(TypeOfCommunication.NotifyChannel)) {
                message = message.Substring(TypeOfCommunication.NotifyChannel.Length);
                string[] args = message.Split(WebSocketMetadata.DELIMITER);

                uiContext.Post(_ => AddChannel(channelListStackPanel, args[1], args[0]), null);
            }

        }

        public void AddMessage(string channelID, string username, string messageContent) {
            if (CurrentChannelID == channelID) {
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

        StackPanel channelListStackPanel;
        StackPanel messageStackPanel;
        ScrollViewer messageScrollViewer;

        private void InitializeLoginUI() {
            Grid gridLogin = new Grid();

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
            Grid messagingGrid = new Grid();

            Content = messagingGrid;

            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });

            // First Column: Circles
            StackPanel circleStackPanel = new StackPanel();
            ScrollViewer circleScrollViewer = new ScrollViewer() { Content = circleStackPanel };
            messagingGrid.Children.Add(circleScrollViewer);
            Grid.SetColumn(circleScrollViewer, 0);

            AddServerIcon(circleStackPanel, Colors.Black, Colors.White, SpecialServerIDs.DirectMessages, "DM"); // This is where we will access DMs from

            // FETCH SERVERS

            AddServerIcon(circleStackPanel, Colors.Black, Colors.White, SpecialServerIDs.CreateServer, "NEW"); // WE WILL USE THIS TO CREATE NEW SERVER

            // Second Column: Boxes with Icons and Text
            channelListStackPanel = new StackPanel();
            ScrollViewer boxScrollViewer = new ScrollViewer() { Content = channelListStackPanel };
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

            AddChannel(channelListStackPanel, "Friends", "-1");

            foreach (string[] channel in await FetchDMs(Client)) {
                AddChannel(channelListStackPanel, channel[1], channel[0]);
            }

            foreach (string[] message in await FetchMessages(CurrentChannelID, OldestMessage, "false", Client)) {
                messageScrollViewer.ScrollToEnd();
                if (Convert.ToInt32(NewestMessage) < Convert.ToInt32(message[2])) { NewestMessage = message[2]; }
                if (Convert.ToInt32(OldestMessage) > Convert.ToInt32(message[2])) { OldestMessage = message[2]; }
                AddMessage(messageStackPanel, Colors.Black, message[0], message[1], true);
                messageScrollViewer.ScrollToBottom();
            }

            ListenForMessages();
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

        StackPanel FriendsStackPanel;
        private async void InitializeFriendsUI() {
            Grid mainGrid = new Grid();
            mainGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainGrid.VerticalAlignment = VerticalAlignment.Top;

            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Star) });

            // Define the header Grid
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            // Create and add buttons to the header Grid
            Button addButton = new Button() { Content = "Add Friend" };
            Button dmButton = new Button() { Content = "New DM", Margin = new Thickness(5) };
            Button groupChatButton = new Button() { Content = "New Group Chat", Margin = new Thickness(5) };
            Button exitButton = new Button() { Content = "X", Margin = new Thickness(5) };

            addButton.Click += AddButton_Click;
            dmButton.Click += DmButton_Click;
            groupChatButton.Click += GroupChatButton_Click;
            exitButton.Click += ExitButton_Click;

            TextBox FriendText = new TextBox();

            StackPanel AddFriend = new StackPanel() { Margin = new Thickness(5) };
            AddFriend.Children.Add(addButton);
            AddFriend.Children.Add(FriendText);

            Grid.SetColumn(AddFriend, 0);
            Grid.SetColumn(dmButton, 1);
            Grid.SetColumn(groupChatButton, 2);
            Grid.SetColumn(exitButton, 3);

            headerGrid.Children.Add(AddFriend);
            headerGrid.Children.Add(dmButton);
            headerGrid.Children.Add(groupChatButton);
            headerGrid.Children.Add(exitButton);

            Grid.SetRow(headerGrid, 0);



            // Define the friend list StackPanel
            FriendsStackPanel = new StackPanel();
            FriendsStackPanel.Margin = new Thickness(10, 40, 10, 10);

            ScrollViewer friendsScrollViewer = new ScrollViewer();
            friendsScrollViewer.Content = FriendsStackPanel;
            Grid.SetRow(friendsScrollViewer, 1);

            friendsScrollViewer.VerticalAlignment = VerticalAlignment.Stretch;

            // Add the header Grid and friend list StackPanel to the main Grid
            mainGrid.Children.Add(headerGrid);
            mainGrid.Children.Add(friendsScrollViewer);

            foreach (string friend in await FetchFriends(Client)) {
                AddFriendElement(friend, true, FriendsStackPanel);
            }

            // Set the main Grid as the Window content
            this.Content = mainGrid;

        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) {
            InitializeMessagingUI();
        }

        private async void GroupChatButton_Click(object sender, RoutedEventArgs e) {
            List<string> UserIDs = new List<string>();
            UserIDs.Add(CurrentUserID);
            foreach (Border item in FriendsStackPanel.Children) {
                Grid grid = item.Child as Grid;
                CheckBox checkbox = grid.Children[0] as CheckBox;
                if (checkbox.IsChecked == true) {
                    Label label = grid.Children[2] as Label;

                    string UserID = await GetID(label.Content.ToString(), Client);
                    UserIDs.Add(UserID);
                }
            }
            byte[] SerializedData = SerializeList<string>(UserIDs);
            string B64Data = Convert.ToBase64String(SerializedData);

            string[] data = { B64Data };

            Client.SendAndRecieve(TypeOfCommunication.CreateGroupChannel, data);
        }

        private async void DmButton_Click(object sender, RoutedEventArgs e) {
            foreach (Border item in FriendsStackPanel.Children) {
                Grid grid = item.Child as Grid;
                CheckBox checkbox = grid.Children[0] as CheckBox;
                if (checkbox.IsChecked == true) {
                    Label label = grid.Children[2] as Label;

                    string ID = await GetID(label.Content.ToString(), Client);

                    string[] data = { ID };

                    await Client.SendAndRecieve(TypeOfCommunication.CreateDMChannel, data);
                }
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e) {
            Button button = sender as Button;
            StackPanel panel = button.Parent as StackPanel;
            TextBox text = panel.Children[1] as TextBox;


            string ID = await GetID(text.Text, Client);
            string[] data = { ID };
            await Client.SendAndRecieve(TypeOfCommunication.AddFriend, data);

            AddFriendElement(text.Text, true, FriendsStackPanel);
        }

        private void AddFriendElement(string username, bool removeButtonToggle, StackPanel stackPanel) {
            // Create a friend element
            Border friendBorder = new Border();
            friendBorder.BorderBrush = Brushes.LightGray;
            friendBorder.BorderThickness = new Thickness(0, 0, 0, 1);
            friendBorder.Padding = new Thickness(5);

            StackPanel friendStackPanel = new StackPanel();
            friendStackPanel.Orientation = Orientation.Horizontal;

            CheckBox checkBox = new CheckBox {
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 10, 0)
            };

            Ellipse ellipse = new Ellipse();
            ellipse.Width = 25;
            ellipse.Height = 25;
            ellipse.Fill = Brushes.Red;
            ellipse.HorizontalAlignment = HorizontalAlignment.Left;

            Label label = new Label();
            label.Content = username;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.HorizontalAlignment = HorizontalAlignment.Left;

            Button removeButton = new Button();
            removeButton.Content = "X";
            removeButton.VerticalAlignment = VerticalAlignment.Center;
            removeButton.HorizontalAlignment = HorizontalAlignment.Right;
            removeButton.Width = 20;
            removeButton.Click += RemoveFriend_Click;


            // Add elements to the friendGrid
            friendStackPanel.Children.Add(checkBox);
            friendStackPanel.Children.Add(ellipse);
            friendStackPanel.Children.Add(label);

            if (removeButtonToggle) {
                friendStackPanel.Children.Add(removeButton);
            }

            friendBorder.Child = friendStackPanel;

            // Add the friendBorder to the FriendsStackPanel
            stackPanel.Children.Add(friendBorder);
        }

        private async void RemoveFriend_Click(object sender, RoutedEventArgs e) {
            Button button = sender as Button;
            StackPanel stackPanel = button.Parent as StackPanel;
            Label label = stackPanel.Children[2] as Label;
            Border border = stackPanel.Parent as Border;
            string username = label.Content.ToString();
            string id = await GetID(username, Client);
            string[] data = { id };

            await Client.SendAndRecieve(TypeOfCommunication.RemoveFriend, data);

            FriendsStackPanel.Children.Remove(border);
        }
    }
}


