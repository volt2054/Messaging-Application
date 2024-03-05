
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using SharedLibrary;
using static SharedLibrary.Serialization;
using System.Threading;

using static SharedLibrary.ContentDeliveryInterface;
using Microsoft.Win32;



namespace Client {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public class SpecialServerIDs {
        public static readonly string DirectMessages = "-1";
        public static readonly string CreateServer = "-2";
        public static readonly string Settings = "-3";
    }
    public class SpecialChannelIDs {
        public static readonly string Friends = "-1";
        public static readonly string CreateChannel = "-2";
        public static readonly string NotMade = "-3";
        public static readonly string UsersList = "-4";
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

        static async Task<string> CreateUser(string username, string password, WebSocketClient Client) {
            string[] data = { username, password };
            string response = await Client.SendAndRecieve(TypeOfCommunication.RegisterUser, data);
            return response;
        }

        static async Task<string> VerifyUser(string username, string password, WebSocketClient Client) {
            string[] data = { username, password };
            string response = await Client.SendAndRecieve(TypeOfCommunication.ValidateUser, data);
            return response;
        }

        static async Task<string> GetID(string username, WebSocketClient Client) {
            string[] data = { username };
            string response = await Client.SendAndRecieve(TypeOfCommunication.GetID, data);
            return response;
        }

        static async Task<string> GetPFP(string userID, WebSocketClient Client) {
            string[] data = { userID };
            string response = await Client.SendAndRecieve(TypeOfCommunication.GetProfilePicture, data);
            return response;
        }

        static async void SendMessage(string message, string channelID, WebSocketClient Client) {
            string[] data = { message, channelID };
            await Client.SendAndRecieve(TypeOfCommunication.SendMessage, data);
        }

        static async void SendFile(string fileID, string channelID, WebSocketClient Client) {
            string[] data = { fileID, channelID };
            await Client.SendAndRecieve(TypeOfCommunication.SendAttachment, data);
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

        static Dictionary<string, string> userProfileCache = new Dictionary<string, string>();

        static async Task<List<string[]>> FetchMessages(string channelID, string messageID, string before, WebSocketClient Client) {
            string[] data = { channelID, messageID, before };
            string response = await Client.SendAndRecieve(TypeOfCommunication.FetchMessages, data);

            if (response == "-1") {
                return new List<string[]>();
            }

            byte[] dataBytes = Convert.FromBase64String(response);
            List<string[]> messageList = DeserializeList<string[]>(dataBytes);

            foreach (string[] message in messageList) {
                string userId = message[3];
                if (!userProfileCache.ContainsKey(userId)) {
                    string userPFP = await GetPFP(userId, Client);
                    userProfileCache[userId] = userPFP;
                }
                string pfpName = userProfileCache[userId];
                message[3] = pfpName;
            }


            return messageList;

        }

        static async Task<List<string[]>> FetchServers(WebSocketClient Client) {
            string[] data = new string[0];
            string response = await Client.SendAndRecieve(TypeOfCommunication.FetchServers, data);

            if (response == "-1") {
                return new List<string[]>();
            }

            byte[] dataBytes = Convert.FromBase64String(response);
            List<string[]> serverList = DeserializeList<string[]>(dataBytes);

            return serverList;
        }

        static async Task<List<User>> FetchUsersInServer(WebSocketClient Client, string serverID) {
            string[] data = { serverID };
            string response = await Client.SendAndRecieve(TypeOfCommunication.GetUsersInServer, data);

            if (response == "-1") {
                return new List<User>();
            }

            byte[] dataBytes = Convert.FromBase64String(response);
            List<User> UsersList = DeserializeList<User>(dataBytes);

            return UsersList;
        }

        static async Task<List<User>> FetchFriends(WebSocketClient Client) {
            string[] data = { };
            string response = await Client.SendAndRecieve(TypeOfCommunication.GetFriends, data);

            if (response == "-1") {
                return new List<User>();
            }

            byte[] dataBytes = Convert.FromBase64String(response);
            List<User> friendsList = DeserializeList<User>(dataBytes);

            return friendsList;
        }

        static async Task<List<User>> FetchFriendRequests(WebSocketClient Client) {
            string[] data = { };
            string response = await Client.SendAndRecieve(TypeOfCommunication.GetRequests, data);

            if (response == "-1") {
                return new List<User>();
            }

            byte[] dataBytes = Convert.FromBase64String(response);
            List<User> friendsList = DeserializeList<User>(dataBytes);

            return friendsList;
        }

        public MainWindow() {
            InitializeComponent();
            Init();
        }

        private void Init() {
            Client = new WebSocketClient();
            ClearCache();
            InitializeLoginUI();
        }

        // Make sure websocket is closed
        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e) {

            if (Client != null) {
                await Client.CloseWebSocket();
            }

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
            Grid ServerGrid = (Grid)sender;
            Ellipse ServerIcon = (Ellipse)ServerGrid.Children[0];
            string Tag = (string)ServerIcon.Tag;

            channelListStackPanel.Children.Clear();

            CurrentServerID = Tag;

            if (Tag == SpecialServerIDs.DirectMessages) {

                // FRIENDS CHANNEL
                AddChannel(channelListStackPanel, "Friends", "-1", SpecialServerIDs.DirectMessages);

                foreach (string[] channel in await FetchDMs(Client)) {
                    AddChannel(channelListStackPanel, channel[1], channel[0], SpecialServerIDs.DirectMessages);
                }
            } else if (Tag == SpecialServerIDs.CreateServer) {
                // CREATE SERVER UI
                InitializeCreateServerUI();
            } else if (Tag == SpecialServerIDs.Settings) {
                // SETTINGS UI
                InitializeSettingsUI();
            } else {

                Button button = new Button() {
                    Content = "New Channel"
                };

                button.Click += (s, e) => {
                    TextBox newChannelTextBox = new TextBox();
                    channelListStackPanel.Children.Add(newChannelTextBox);
                    newChannelTextBox.Focus();
                    newChannelTextBox.Background = Brushes.LightGray;

                    newChannelTextBox.KeyDown += async (s, e) => {
                        if (e.Key == Key.Enter) {
                            string[] data = { newChannelTextBox.Text, CurrentServerID };
                            string channelID = await Client.SendAndRecieve(TypeOfCommunication.CreateChannel, data);
                            channelListStackPanel.Children.Remove(newChannelTextBox);
                        }
                    };
                };

                channelListStackPanel.Children.Add(button);

                AddChannel(channelListStackPanel, "User List", SpecialChannelIDs.UsersList, CurrentServerID);

                foreach (string[] channel in await FetchChannels(CurrentServerID, Client)) {
                    AddChannel(channelListStackPanel, channel[1], channel[0], CurrentServerID);
                }
            }
        }

        private async void InitializeCreateServerUI() {
            List<string> channels = new List<string>();
            Grid mainGrid = new Grid {
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

            Grid serverOptionsGrid = new Grid();
            serverOptionsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            serverOptionsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // First row of ServerOptionsGrid
            StackPanel stackPanel1 = new StackPanel {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel1.Children.Add(new Label { Content = "Name:" });

            TextBox serverName = new TextBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) };
            stackPanel1.Children.Add(serverName);
            stackPanel1.Children.Add(new Label { Content = "Description:", Margin = new Thickness(10, 0, 0, 0) });
            TextBox serverDescription = new TextBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) };
            stackPanel1.Children.Add(serverDescription);

            // Second row of ServerOptionsGrid
            Grid channelGrid = new Grid();
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
            scrollViewerBorder.Child = scrollViewer;

            // Textbox and Button in the second column of channelgrid
            StackPanel ChannelsStackPanel = new StackPanel {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };

            TextBox addChannelTextBox = new TextBox();


            Button addChannelButton = new Button {
                Content = "Add Channel",
                Margin = new Thickness(0, 5, 0, 0)
            };
            addChannelButton.Click += (s, e) => {
                AddChannel(scrollViewerContent, addChannelTextBox.Text, "-2", SpecialServerIDs.CreateServer);
                channels.Add(addChannelTextBox.Text);
            };

            ChannelsStackPanel.Children.Add(addChannelTextBox);
            ChannelsStackPanel.Children.Add(addChannelButton);


            channelGrid.Children.Add(scrollViewerBorder);
            Grid.SetColumn(scrollViewerBorder, 0);
            channelGrid.Children.Add(ChannelsStackPanel);
            Grid.SetColumn(ChannelsStackPanel, 1);

            serverOptionsGrid.Children.Add(stackPanel1);
            Grid.SetRow(stackPanel1, 0);
            serverOptionsGrid.Children.Add(channelGrid);
            Grid.SetRow(channelGrid, 1);

            serverOptionsBorder.Child = serverOptionsGrid;


            Grid friendsGrid = new Grid();
            RowDefinition friendsGridRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
            RowDefinition friendsGridRow2 = new RowDefinition { Height = new GridLength(4, GridUnitType.Star) };
            RowDefinition friendsGridRow3 = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };


            friendsGrid.RowDefinitions.Add(friendsGridRow);
            friendsGrid.RowDefinitions.Add(friendsGridRow2);
            friendsGrid.RowDefinitions.Add(friendsGridRow3);

            // ScrollViewer in the second column
            Border scrollViewer2Border = new Border {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                VerticalAlignment = VerticalAlignment.Stretch
            };

            ScrollViewer scrollViewer2 = new ScrollViewer {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            StackPanel friendsStackPanel = new StackPanel();

            scrollViewer2.Content = friendsStackPanel;
            scrollViewer2Border.Child = scrollViewer2;

            foreach (User friend in await FetchFriends(Client)) {
                AddUserElement(friend, false, true, false, false, friendsStackPanel);
            }

            Button goBack = new Button();
            goBack.Margin = new Thickness(0, 0, 0, 20);
            goBack.FontSize = 24;
            goBack.Content = "Go Back";
            goBack.VerticalAlignment = VerticalAlignment.Top;

            goBack.Click += (s, e) => {
                channels.Clear();
                InitializeMessagingUI();
            };

            Button createServer = new Button();
            createServer.Margin = new Thickness(0, 20, 0, 0);
            createServer.FontSize = 24;
            createServer.Content = "Create Server";
            createServer.VerticalAlignment = VerticalAlignment.Bottom;

            createServer.Click += async (s, e) => {
                byte[] SerializedChannels = SerializeList<string>(channels);
                string SerializedChannelsString = Convert.ToBase64String(SerializedChannels);

                List<string> friends = new List<string>();

                foreach (Border friendsBorder in friendsStackPanel.Children) {
                    StackPanel stackpanel = (StackPanel)friendsBorder.Child;
                    CheckBox checkbox = (CheckBox)stackpanel.Children[0];
                    if (checkbox.IsChecked == true) {
                        friends.Add((string)friendsBorder.Tag);
                    }
                }

                List<string> friendIDs = new List<string>();
                foreach (string friendUsername in friends) {
                    string[] dataForGetID = { friendUsername };
                    string friendID = await Client.SendAndRecieve(TypeOfCommunication.GetID, dataForGetID);
                    friendIDs.Add(friendID);
                }

                byte[] SerializedFriends = SerializeList<string>(friendIDs);
                string SerializedFriendsString = Convert.ToBase64String(SerializedFriends);


                string[] data = { serverName.Text, serverDescription.Text, SerializedChannelsString, SerializedFriendsString };

                await Client.SendAndRecieve(TypeOfCommunication.CreateServer, data);
                InitializeMessagingUI();
            };



            Grid.SetRow(goBack, 0);
            Grid.SetRow(scrollViewer2Border, 1);
            Grid.SetRow(createServer, 2);

            friendsGrid.Children.Add(goBack);
            friendsGrid.Children.Add(scrollViewer2Border);
            friendsGrid.Children.Add(createServer);

            Grid.SetColumn(serverOptionsBorder, 0);
            Grid.SetColumn(friendsGrid, 1);

            mainGrid.Children.Add(serverOptionsBorder);
            mainGrid.Children.Add(friendsGrid);

            // Set the mainGrid as the content of the Window
            this.Content = mainGrid;
        }


        private async void InitializeSettingsUI() {
            // Main Grid
            Grid mainGrid = new Grid();
            mainGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainGrid.VerticalAlignment = VerticalAlignment.Stretch;

            // Main section content
            Grid contentGrid = new Grid();
            contentGrid.Margin = new Thickness(160, 0, 10, 0);

            // Account section
            StackPanel accountSection = new StackPanel();

            // Profile Picture
            StackPanel profilePicturePanel = new StackPanel();
            profilePicturePanel.Orientation = Orientation.Horizontal;
            profilePicturePanel.HorizontalAlignment = HorizontalAlignment.Center;
            profilePicturePanel.Margin = new Thickness(0, 20, 0, 10);

            Image profilePicture = new Image { Name = "ProfilePicture", Width = 100, Height = 100 };

            string pfpFileName = await GetPFP(CurrentUserID, Client);

            string pfp = await CacheFileAsync(pfpFileName);

            profilePicture.Source = new BitmapImage(new Uri(pfp));

            Button changeProfilePicButton = new Button { Content = "Change Profile Pic", Margin = new Thickness(10, 0, 0, 0), Width = 100 };
            changeProfilePicButton.Click += async (s , e) => {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image Files(*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == true) {
                    string imagePath = openFileDialog.FileName;

                    string pfpUrl = await UploadFileAsync(imagePath);
                    string[] data = { pfpUrl };
                    await Client.SendAndRecieve(TypeOfCommunication.SetProfilePicture, data);

                    // Save the file path and update the UI
                    profilePicture.Source = new BitmapImage(new Uri(imagePath));
                }
            };

            profilePicturePanel.Children.Add(profilePicture);
            profilePicturePanel.Children.Add(changeProfilePicButton);

            // Account Information
            StackPanel accountInfoPanel = new StackPanel();
            accountInfoPanel.Margin = new Thickness(0, 10, 0, 0);
            accountInfoPanel.HorizontalAlignment = HorizontalAlignment.Center;

            Grid accountInfoGrid = new Grid();
            accountInfoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            accountInfoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            accountInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            accountInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            accountInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock usernameTextBlock = new TextBlock { Text = "Username" };
            TextBox usernameTextBox = new TextBox { Margin = new Thickness(10, 0, 10, 10), MinWidth = 200 };
            Button changeUsernameButton = new Button { Content = "Change", Margin = new Thickness(10,0,0,10) };

            changeUsernameButton.Click += async (s, e) => {
                string[] data = { usernameTextBox.Text };
                await Client.SendAndRecieve(TypeOfCommunication.ChangeUsername, data);
            };

            Grid.SetRow(usernameTextBlock, 0);
            Grid.SetColumn(usernameTextBlock, 0);
            Grid.SetRow(usernameTextBox, 0);
            Grid.SetColumn(usernameTextBox, 1);
            Grid.SetRow(changeUsernameButton, 0);
            Grid.SetColumn(changeUsernameButton, 2);

            accountInfoGrid.Children.Add(usernameTextBlock);
            accountInfoGrid.Children.Add(usernameTextBox);
            accountInfoGrid.Children.Add(changeUsernameButton);

            TextBlock passwordTextBlock = new TextBlock { Text = "Password" };
            TextBox passwordTextBox = new TextBox { Margin = new Thickness(10, 0, 10, 0), MinWidth = 200 };
            Button changePasswordButton = new Button { Content = "Change", Margin = new Thickness(10, 0, 0, 0) };

            changePasswordButton.Click += async (s, e) => {
                string[] data = { passwordTextBlock.Text };
                await Client.SendAndRecieve(TypeOfCommunication.ChangePassword, data);
            };

            Grid.SetRow(passwordTextBlock, 1);
            Grid.SetColumn(passwordTextBlock, 0);
            Grid.SetRow(passwordTextBox, 1);
            Grid.SetColumn(passwordTextBox, 1);
            Grid.SetRow(changePasswordButton, 1);
            Grid.SetColumn(changePasswordButton, 2);

            accountInfoGrid.Children.Add(passwordTextBlock);
            accountInfoGrid.Children.Add(passwordTextBox);
            accountInfoGrid.Children.Add(changePasswordButton);

            accountInfoPanel.Children.Add(accountInfoGrid);

            accountSection.Children.Add(profilePicturePanel);
            accountSection.Children.Add(accountInfoPanel);

            contentGrid.Children.Add(accountSection);

            // Appearance section
            StackPanel appearanceSection = new StackPanel();
            appearanceSection.Visibility = Visibility.Collapsed;
            appearanceSection.HorizontalAlignment = HorizontalAlignment.Left;
            appearanceSection.VerticalAlignment = VerticalAlignment.Center;

            Grid appearanceSectionGrid = new Grid();

            appearanceSection.Children.Add(appearanceSectionGrid);

            appearanceSectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            appearanceSectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            appearanceSectionGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            appearanceSectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            appearanceSectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            appearanceSectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // List of 5 rows
            for (int i = 0; i < 3; i++) {

                // TextBlock for color label
                TextBlock label = new TextBlock();
                label.Text = GetColorLabel(i);
                label.Margin = new Thickness(0, 0, 10, 10);

                // TextBox for color entry
                TextBox colorTextBox = new TextBox();
                colorTextBox.Name = $"ColorTextBox_{i}";
                colorTextBox.Width = 100;
                colorTextBox.Margin = new Thickness(0, 0, 0, 10);


                // Little square for color preview
                Border colorPreviewBorder = new Border();
                colorPreviewBorder.BorderThickness = new Thickness(1, 1, 1, 1);
                colorPreviewBorder.BorderBrush = Brushes.Black;
                colorPreviewBorder.Margin = new Thickness(10, 0, 0, 10);

                Rectangle colorPreview = new Rectangle();
                colorPreview.Width = 20;
                colorPreview.Height = 20;

                colorPreviewBorder.Child = colorPreview;

                colorTextBox.TextChanged += (s, e) => {
                    try {
                        Brush colorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorTextBox.Text));
                        colorPreview.Fill = colorBrush;
                    } catch {

                    }
                };

                appearanceSectionGrid.Children.Add(label);
                appearanceSectionGrid.Children.Add(colorTextBox);
                appearanceSectionGrid.Children.Add(colorPreviewBorder);

                Grid.SetRow(label, i);
                Grid.SetRow(colorTextBox, i);
                Grid.SetRow(colorPreviewBorder, i);
                Grid.SetColumn(label, 0);
                Grid.SetColumn(colorTextBox, 1);
                Grid.SetColumn(colorPreviewBorder, 2);

            }

            contentGrid.Children.Add(appearanceSection);

            // Navigation bar on the left
            StackPanel navigationPanel = new StackPanel();
            navigationPanel.Width = 150;
            navigationPanel.Background = new SolidColorBrush(Color.FromRgb(238, 238, 238));
            navigationPanel.HorizontalAlignment = HorizontalAlignment.Left;

            Button goBackButton = new Button { Content = "Go Back", Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(10) };
            goBackButton.Click += (s, e) => {
                InitializeMessagingUI();
            };

            Button accountButton = new Button { Content = "Account", Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(10) };
            accountButton.Click += async (s, e) => {
                appearanceSection.Visibility = Visibility.Collapsed;
                accountSection.Visibility = Visibility.Visible;

                string pfpFileName = await GetPFP(CurrentUserID, Client);

                string savePath = await CacheFileAsync(pfpFileName);

                profilePicture.Source = new BitmapImage(new Uri(savePath));
            };

            Button appearanceButton = new Button { Content = "Appearance", Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(10) };
            appearanceButton.Click += (s, e) => {
                accountSection.Visibility = Visibility.Collapsed;
                appearanceSection.Visibility = Visibility.Visible;
            };

            Button logOutButton = new Button { Content = "Log Out", Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(10) };
            logOutButton.Click += (s, e) => {
                Client.CloseWebSocket();
                Init();
            };

            navigationPanel.Children.Add(goBackButton);
            navigationPanel.Children.Add(accountButton);
            navigationPanel.Children.Add(appearanceButton);
            navigationPanel.Children.Add(logOutButton);

            mainGrid.Children.Add(navigationPanel);

            mainGrid.Children.Add(contentGrid);

            this.Content = mainGrid;
        }

        private string GetColorLabel(int index) {
            switch (index) {
                case 0: return "Background Color:";
                case 1: return "Text Color:";
                case 2: return "Accent Color:";
                default: return "";
            }
        }



        private async void changeProfilePicButton_Click(object sender, RoutedEventArgs e) {
            
        }

        private void AddChannel(StackPanel parentStackPanel, string channelName, string channelID, string serverID) {

            if (CurrentServerID != serverID) {
                return;
            }

            string iconPath;

            if (channelID == SpecialChannelIDs.Friends || channelID == SpecialChannelIDs.UsersList) {
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

            Button settings = new Button {
                Content = "⚙",
                Margin = new Thickness(10, 0, 0, 0)
            };

            settings.Click += (s, e) => {
                CurrentChannelID = channelID;
                InitializeUserListUI(true, false);
            };

            if (channelID != SpecialChannelIDs.NotMade) {
                ChannelElement.MouseLeftButtonDown += ChannelElement_MouseLeftButtonDown;
            }

            ChannelElement.Children.Add(icon);
            ChannelElement.Children.Add(textBlock);

            if (Convert.ToInt32(serverID) >= 0 && Convert.ToInt32(channelID) >= 0) {
                ChannelElement.Children.Add(settings);
            }

            parentStackPanel.Children.Add(ChannelElement);
        }

        private async void ChannelElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            StackPanel ChannelElement = (StackPanel)sender;
            CurrentChannelID = (string)ChannelElement.Tag;

            if (CurrentChannelID == SpecialChannelIDs.Friends) {
                InitializeFriendsUI();
            } else if (CurrentChannelID == SpecialChannelIDs.UsersList) {
                InitializeUserListUI(false, true);
            } else {
                messageStackPanel.Children.Clear();
                OldestMessage = int.MinValue.ToString();
                NewestMessage = int.MaxValue.ToString();

                foreach (string[] message in await FetchMessages(CurrentChannelID, OldestMessage, "false", Client)) {
                    messageScrollViewer.ScrollToEnd();
                    if (Convert.ToInt32(NewestMessage) < Convert.ToInt32(message[2])) { NewestMessage = message[2]; }
                    if (Convert.ToInt32(OldestMessage) > Convert.ToInt32(message[2])) { OldestMessage = message[2]; }
                    if (message[4] != "2") {
                        await AddMessage(messageStackPanel, message[3], message[0], message[1], false);
                    } else {
                        AddAttachment(messageStackPanel, message[3], message[0], message[1], false);
                    }
                    messageScrollViewer.ScrollToBottom();
                }
            }
        }

        // BROKEN
        private void ListenForMessages() {
            SynchronizationContext uiContext = SynchronizationContext.Current;

            Task.Run(() => {
                while (true) {
                    string message = Client.GetNextMessage();

                    HandleServerMessage(message, uiContext);
                }
            });
        }

        private void HandleServerMessage(string message, SynchronizationContext uiContext) {
            if (message.StartsWith(TypeOfCommunication.NotifyMessage)) {
                message = message.Substring(TypeOfCommunication.NotifyMessage.Length);
                string[] args = message.Split(WebSocketMetadata.DELIMITER);

                uiContext.Post(_ => AddMessage(args[0], args[1], args[2], args[3]), null);
            } else if (message.StartsWith(TypeOfCommunication.NotifyAttachment)) {
                message = message.Substring(TypeOfCommunication.NotifyAttachment.Length);
                string[] args = message.Split(WebSocketMetadata.DELIMITER);

                uiContext.Post(_ => AddAttachment(args[0], args[1], args[2], args[3]), null);
            } else if (message.StartsWith(TypeOfCommunication.NotifyChannel)) {
                message = message.Substring(TypeOfCommunication.NotifyChannel.Length);
                string[] args = message.Split(WebSocketMetadata.DELIMITER);

                uiContext.Post(_ => AddChannel(channelListStackPanel, args[1], args[0], args[2]), null);
            } else if (message.StartsWith(TypeOfCommunication.NotifyServer)) {
                message = message.Substring(TypeOfCommunication.NotifyServer.Length);
                string[] args = message.Split(WebSocketMetadata.DELIMITER);

                uiContext.Post(_ => AddServerIcon(serverStackPanel, Colors.Azure, Colors.Red, args[0], args[1]), null);
            }
        }

        public async void AddMessage(string channelID, string username, string messageContent, string userPFP) {
            if (CurrentChannelID == channelID) {
                await AddMessage(messageStackPanel, userPFP, username, messageContent, false);
            }
        }

        public void AddAttachment(string channelID, string username, string fileId, string userPFP) {
            if (CurrentChannelID == channelID) {
                AddAttachment(messageStackPanel, userPFP, username, fileId, false);
            }
        }

        private async void AddAttachment(StackPanel parentStackPanel, string PFP, string username, string fileId, bool before) {
            StackPanel messageStackPanel = new StackPanel {
                Orientation = Orientation.Horizontal
            };

            BitmapImage pfp = new BitmapImage(new Uri(await CacheFileAsync(PFP)));
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = pfp;

            Ellipse ellipse = new Ellipse {
                Width = 25,
                Height = 25,
                Fill = imageBrush
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

            Button AttachmentButton = new Button {
                Content = $"Download {fileId}",
                Margin = new Thickness(5, 0, 0, 10),
                MaxHeight = 100,
                BorderThickness = new Thickness(0)
            };

            AttachmentButton.Click += async (s, e) => {
                System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    string savePath = folderBrowserDialog.SelectedPath + "/";

                    try {
                        await DownloadFileAsync(fileId, savePath);
                    } catch (Exception ex) {
                    }
                }
            };

            usernameAndMessageStackPanel.Children.Add(usernameTextBlock);
            usernameAndMessageStackPanel.Children.Add(AttachmentButton);

            messageStackPanel.Children.Add(ellipse);
            messageStackPanel.Children.Add(usernameAndMessageStackPanel);

            if (before) {
                double newContentHeight = 50; // TODO FETCH NEW CONTENT HEIGHT
                double currentVerticalOffset = messageScrollViewer.VerticalOffset;
                parentStackPanel.Children.Insert(0, messageStackPanel);
                messageScrollViewer.ScrollToVerticalOffset(currentVerticalOffset + newContentHeight);
            } else { parentStackPanel.Children.Add(messageStackPanel); messageScrollViewer.ScrollToEnd(); }
        }

        private async Task AddMessage(StackPanel parentStackPanel, string PFP, string username, string message, bool before) {
            StackPanel messageStackPanel = new StackPanel {
                Orientation = Orientation.Horizontal
            };

            string pfpFile = await CacheFileAsync(PFP);
            ImageBrush imageBrush = new ImageBrush();
            if (pfpFile != "-1") {
                BitmapImage pfp = new BitmapImage(new Uri(pfpFile));
                imageBrush.ImageSource = pfp;
            } else {
                //BGROKEN
                imageBrush.ImageSource = new BitmapImage(new Uri(Icons.Chat, UriKind.Relative));
            }

            Ellipse ellipse = new Ellipse {
                Width = 25,
                Height = 25,
                Fill = imageBrush
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

            TextBox messageTextBlock = new TextBox {
                Text = message,
                Margin = new Thickness(5, 0, 0, 10),
                MaxHeight = 100,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                BorderThickness = new Thickness(0)
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
            RowDefinition rowDefinitionPasswordLogin = new RowDefinition();
            rowDefinitionPasswordLogin.Height = new GridLength(1, GridUnitType.Star);
            RowDefinition rowDefinitionLoginButton = new RowDefinition();
            rowDefinitionLoginButton.Height = new GridLength(2, GridUnitType.Star);

            gridLogin.RowDefinitions.Add(rowDefinitionTitleLogin);
            gridLogin.RowDefinitions.Add(rowDefinitionUsernameLogin);
            gridLogin.RowDefinitions.Add(rowDefinitionPasswordLogin);
            gridLogin.RowDefinitions.Add(rowDefinitionLoginButton);

            Label lab_Title = new Label();
            lab_Title.Content = "Messaging";
            lab_Title.FontSize = 36;
            lab_Title.Width = 200;
            lab_Title.VerticalAlignment = VerticalAlignment.Center;
            lab_Title.HorizontalContentAlignment = HorizontalAlignment.Center;

            Grid.SetRow(lab_Title, 0);

            TextBox txt_Username = new TextBox();
            txt_Username.Text = "Username";
            txt_Username.HorizontalAlignment = HorizontalAlignment.Center;
            txt_Username.VerticalAlignment = VerticalAlignment.Center;
            txt_Username.Width = 150;
            txt_Username.Height = 30;
            txt_Username.FontSize = 20;
            Grid.SetRow(txt_Username, 1);

            TextBox txt_Password = new TextBox();
            txt_Password.Text = "Password";
            txt_Password.HorizontalAlignment = HorizontalAlignment.Center;
            txt_Password.VerticalAlignment = VerticalAlignment.Center;
            txt_Password.Width = 150;
            txt_Password.Height = 30;
            txt_Password.FontSize = 20;
            Grid.SetRow(txt_Password, 2);

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
            gridLogin.Children.Add(txt_Password);


            btn_Register.Click += async (s,e) => {
                CurrentUserID = await CreateUser(txt_Username.Text, txt_Password.Text, Client);
                if (CurrentUserID != null) {
                    InitializeMessagingUI();
                }
            };
            btn_Login.Click += async (s,e) => {
                CurrentUserID = await VerifyUser(txt_Username.Text, txt_Password.Text, Client);

                if (CurrentUserID != "Bad Password") {
                    InitializeMessagingUI();
                } else {
                    MessageBox.Show("Bad Password");
                }
            };

            Grid gridButtonOptions = new Grid();
            ColumnDefinition columnDefinitionLoginButton = new ColumnDefinition();
            ColumnDefinition columnDefinitionRegisterButton = new ColumnDefinition();

            RowDefinition rowDefintionButtons = new RowDefinition();

            gridButtonOptions.ColumnDefinitions.Add(columnDefinitionLoginButton);
            gridButtonOptions.ColumnDefinitions.Add(columnDefinitionRegisterButton);
            gridButtonOptions.RowDefinitions.Add(rowDefintionButtons);

            ColumnDefinition columnDefinitionLogin = new ColumnDefinition();
            gridLogin.ColumnDefinitions.Add(columnDefinitionLogin);

            Grid.SetRow(gridButtonOptions, 3);

            gridButtonOptions.Children.Add(btn_Login);
            gridButtonOptions.Children.Add(btn_Register);

            gridLogin.Children.Add(gridButtonOptions);

            Content = gridLogin;
        }

        StackPanel serverStackPanel = new StackPanel();
        private async void InitializeMessagingUI() {

            serverStackPanel = new StackPanel();
            Grid messagingGrid = new Grid();

            Content = messagingGrid;

            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
            messagingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });

            // First Column: SErvers
            ScrollViewer serverScrollViewer = new ScrollViewer() { Content = serverStackPanel };
            messagingGrid.Children.Add(serverScrollViewer);
            Grid.SetColumn(serverScrollViewer, 0);

            AddServerIcon(serverStackPanel, Colors.Black, Colors.White, SpecialServerIDs.Settings, "SETTINGS"); // USER SETTINGS ETC


            AddServerIcon(serverStackPanel, Colors.Black, Colors.White, SpecialServerIDs.DirectMessages, "DM"); // This is where we will access DMs from

            // FETCH SERVERS

            AddServerIcon(serverStackPanel, Colors.Black, Colors.White, SpecialServerIDs.CreateServer, "NEW"); // WE WILL USE THIS TO CREATE NEW SERVER

            foreach (string[] server in await FetchServers(Client)) {
                AddServerIcon(serverStackPanel, Colors.Azure, Colors.Red, server[1], server[0]);
            }

            // Second Column: Chann els
            channelListStackPanel = new StackPanel();
            ScrollViewer boxScrollViewer = new ScrollViewer() { Content = channelListStackPanel };
            messagingGrid.Children.Add(boxScrollViewer);
            Grid.SetColumn(boxScrollViewer, 1);

            // Third Column: messages
            messageStackPanel = new StackPanel();
            messageStackPanel.VerticalAlignment = VerticalAlignment.Bottom;

            // Creating the Grid named "messagesendinggrid"
            Grid messageSendingGrid = new Grid();
            messageSendingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // Button
            messageSendingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(9, GridUnitType.Star) }); // TextBox


            // Button
            Button attachmentButton = new Button {
                Content = "+",
                Height = 30,
                Width = 30,
            };
            Grid.SetColumn(attachmentButton, 0);

            attachmentButton.Click += async (s, e) => {
                // Open file dialog
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true) {
                    // Get the selected file name
                    string selectedFileName = openFileDialog.FileName;
                    string fileID = await UploadFileAsync(selectedFileName);

                    string fileLink = $"{WebSocketMetadata.CDSERVER_URL + fileID}";
                    SendFile(fileID, CurrentChannelID, Client);
                }
            };

            // TextBox
            TextBox messageBox = new TextBox {
                Height = 30,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 5, 10, 5)
            };
            messageBox.KeyDown += TextBox_KeyDown;
            Grid.SetColumn(messageBox, 1);

            // Adding Button and TextBox to the Grid
            messageSendingGrid.Children.Add(attachmentButton);
            messageSendingGrid.Children.Add(messageBox);

            // Creating the ScrollViewer for the messageStackPanel
            messageScrollViewer = new ScrollViewer() {
                Content = messageStackPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            messageScrollViewer.ScrollChanged += MessageScrollViewer_ScrollChanged;

            // Creating the main Grid
            Grid messageGrid = new Grid();
            messageGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(9, GridUnitType.Star) }); // Messages
            messageGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // TextBox
            messageGrid.Children.Add(messageScrollViewer);
            messageGrid.Children.Add(messageSendingGrid); // Adding the messagesendinggrid to the main Grid

            Grid.SetRow(messageScrollViewer, 0);
            Grid.SetRow(messageSendingGrid, 1);


            messagingGrid.Children.Add(messageGrid);
            Grid.SetColumn(messageGrid, 2);

            AddChannel(channelListStackPanel, "Friends", "-1", SpecialServerIDs.DirectMessages);

            foreach (string[] channel in await FetchDMs(Client)) {
                AddChannel(channelListStackPanel, channel[1], channel[0], SpecialServerIDs.DirectMessages);
            }

            foreach (string[] message in await FetchMessages(CurrentChannelID, OldestMessage, "false", Client)) {
                messageScrollViewer.ScrollToEnd();
                if (Convert.ToInt32(NewestMessage) < Convert.ToInt32(message[2])) { NewestMessage = message[2]; }
                if (Convert.ToInt32(OldestMessage) > Convert.ToInt32(message[2])) { OldestMessage = message[2]; }
                if (message[4] != "2") {
                    await AddMessage(messageStackPanel, message[3], message[0], message[1], false);
                } else {
                    AddAttachment(messageStackPanel, message[3], message[0], message[1], false);
                }
                messageScrollViewer.ScrollToBottom();
            }

            ListenForMessages();
        }

        private async void MessageScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (e.VerticalOffset == 0) {
                foreach (string[] message in await FetchMessages(CurrentChannelID, OldestMessage, "true", Client)) {
                    if (Convert.ToInt32(OldestMessage) > Convert.ToInt32(message[2])) { OldestMessage = message[2]; }
                    if (message[4] != "2") {
                        await AddMessage(messageStackPanel, message[3], message[0], message[1], true);
                    } else {
                        AddAttachment(messageStackPanel, message[3], message[0], message[1], true);
                    }
                }
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                TextBox textBox = (TextBox)sender;
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
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(2, GridUnitType.Star) });

            // Define the header Grid
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            TextBox FriendText = new TextBox();

            // Create and add buttons to the header Grid
            Button addButton = new Button() { Content = "Add Friend" };
            Button dmButton = new Button() { Content = "New DM", Margin = new Thickness(5) };
            Button groupChatButton = new Button() { Content = "New Group Chat", Margin = new Thickness(5) };
            Button exitButton = new Button() { Content = "X", Margin = new Thickness(5) };

            addButton.Click += async (s, e) => {
                string ID = await GetID(FriendText.Text, Client);
                string[] data = { ID };
                await Client.SendAndRecieve(TypeOfCommunication.AddFriend, data);
            };
            dmButton.Click += DmButton_Click;
            groupChatButton.Click += GroupChatButton_Click;
            exitButton.Click += ExitButton_Click;

            

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

            Button FriendRequests = new Button() {
                Content = "Friend Requests",
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            Grid.SetRow(FriendRequests, 2);

            FriendRequests.Click += (s, e) => {
                InitializeFriendRequestsUI();
            };

            // Add the header Grid and friend list StackPanel to the main Grid
            mainGrid.Children.Add(headerGrid);
            mainGrid.Children.Add(friendsScrollViewer);
            mainGrid.Children.Add(FriendRequests);

            foreach (User friend in await FetchFriends(Client)) {
                AddUserElement(friend, true, true, false, false, FriendsStackPanel);
            }

            // Set the main Grid as the Window content
            this.Content = mainGrid;
        }

        private async void InitializeFriendRequestsUI() {
            Grid FriendRequestsGrid = new Grid();

            FriendRequestsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            FriendRequestsGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(7, GridUnitType.Star) });

            StackPanel RequestsStackPanel = new StackPanel() {
                Margin = new Thickness(20),
            };

            ScrollViewer RequestsScrollViewer = new ScrollViewer();
            RequestsScrollViewer.Content = RequestsStackPanel;

            Grid.SetRow(RequestsScrollViewer, 1);

            List<User> FriendRequests = await FetchFriendRequests(Client);
            foreach (User user in FriendRequests) { 
                AddUserElement(user, true, false, false, true, RequestsStackPanel);
            }

            Button goBackButton = new Button() {
                Content = "Go Back",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10),
                Padding = new Thickness(5)
            };

            Grid.SetRow(goBackButton, 0);

            goBackButton.Click += (s, e) => {
                InitializeFriendsUI();
            };


            FriendRequestsGrid.Children.Add(goBackButton);
            FriendRequestsGrid.Children.Add(RequestsScrollViewer);
            Content = FriendRequestsGrid;
        }

        private async void InitializeUserListUI(bool roleSelect, bool addOrRemoveToServer) {

            Grid mainGrid = new Grid();

            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(9, GridUnitType.Star) });
            if (addOrRemoveToServer) mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            // Define the user list StackPanel
            StackPanel UsersStackPanel = new StackPanel();
            UsersStackPanel.Margin = new Thickness(10, 40, 10, 10);

            Label FriendsLabel = new Label() { Content = "Friends", FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(FriendsLabel, 0);
            Grid.SetRow(FriendsLabel, 0);

            ScrollViewer usersScrollViewer = new ScrollViewer();
            usersScrollViewer.Content = UsersStackPanel;
            Grid.SetColumn(usersScrollViewer, 1);
            Grid.SetRow(usersScrollViewer, 1);

            usersScrollViewer.VerticalAlignment = VerticalAlignment.Stretch;

            // Define the friend list StackPanel
            StackPanel FriendsStackPanel = new StackPanel();
            FriendsStackPanel.Margin = new Thickness(10, 40, 10, 10);

            Label UserListLabel = new Label() { Content = "User List", FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(UserListLabel, 1);
            Grid.SetRow(UserListLabel, 0);

            ScrollViewer friendsScrollViewer = new ScrollViewer();
            friendsScrollViewer.Content = FriendsStackPanel;
            Grid.SetColumn(friendsScrollViewer, 0);
            Grid.SetRow(friendsScrollViewer, 1);

            friendsScrollViewer.VerticalAlignment = VerticalAlignment.Stretch;

            Button GoBack = new Button();
            GoBack.Content = "Go Back";
            GoBack.Margin = new Thickness(10, 10, 0, 0);
            GoBack.Width = 50;
            GoBack.HorizontalAlignment = HorizontalAlignment.Left;

            GoBack.Click += (s, e) => {
                InitializeMessagingUI();
            };

            Grid.SetColumn(GoBack, 0);
            Grid.SetRow(GoBack, 0);
            mainGrid.Children.Add(GoBack);

            mainGrid.Children.Add(friendsScrollViewer);
            mainGrid.Children.Add(usersScrollViewer);

            mainGrid.Children.Add(UserListLabel);
            mainGrid.Children.Add(FriendsLabel);


            if (addOrRemoveToServer) {
                Button addToServer = new Button();
                addToServer.Content = "Add To Server";
                addToServer.Margin = new Thickness(10);

                Grid.SetColumn(addToServer, 0);
                Grid.SetRow(addToServer, 2);

                mainGrid.Children.Add(addToServer);

                addToServer.Click += async (s, e) => {
                    foreach(var obj in FriendsStackPanel.Children) {
                        Border border = (Border)obj;
                        StackPanel stackpanel = (StackPanel)border.Child;
                        CheckBox checkbox = (CheckBox)stackpanel.Children[0];

                        if (checkbox.IsChecked == true){
                            string username = (string)border.Tag;
                            string id = await GetID(username, Client);
                            string[] data = { CurrentServerID, id };
                            await Client.SendAndRecieve(TypeOfCommunication.AddToServer, data);
                        }
                    }
                };

                Button removeFromServer = new Button();
                removeFromServer.Content = "Remove From Server";
                removeFromServer.Margin = new Thickness(40, 10, 40, 10);

                removeFromServer.Click += async (s, e) => {
                    foreach (var obj in UsersStackPanel.Children) {
                        Border border = (Border)obj;
                        StackPanel stackpanel = (StackPanel)border.Child;
                        CheckBox checkbox = (CheckBox)stackpanel.Children[0];

                        if (checkbox.IsChecked == true) {
                            string username = (string)border.Tag;
                            string id = await GetID(username, Client);
                            string[] data = { CurrentServerID, id };
                            await Client.SendAndRecieve(TypeOfCommunication.RemoveFromServer, data);
                        }
                    }
                };

                Grid.SetColumn(removeFromServer, 1);
                Grid.SetRow(removeFromServer, 2);

                mainGrid.Children.Add(removeFromServer);

            }

            foreach (User user in await FetchUsersInServer(Client, CurrentServerID)) {
                AddUserElement(user, false, addOrRemoveToServer, roleSelect, false, UsersStackPanel);
            } // Fetch Users In Server
            foreach (User friend in await FetchFriends(Client)) {
                AddUserElement(friend, false, addOrRemoveToServer, false, false, FriendsStackPanel);
            }

            // Set the main Grid as the Window content
            this.Content = mainGrid;

        }

        private void GoBack_Click(object sender, RoutedEventArgs e) {
            throw new NotImplementedException();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) {
            CurrentServerID = SpecialServerIDs.DirectMessages;
            InitializeMessagingUI();
        }

        private async void GroupChatButton_Click(object sender, RoutedEventArgs e) {
            List<string> UserIDs = new List<string>();
            UserIDs.Add(CurrentUserID);
            foreach (Border item in FriendsStackPanel.Children) {
                StackPanel stackpanel = (StackPanel)item.Child;
                CheckBox checkbox = (CheckBox)stackpanel.Children[0];
                if (checkbox.IsChecked == true) {
                    Label label = (Label)stackpanel.Children[2];

                    string UserID = await GetID(label.Content.ToString(), Client);
                    UserIDs.Add(UserID);
                }
            }
            byte[] SerializedData = SerializeList<string>(UserIDs);
            string B64Data = Convert.ToBase64String(SerializedData);

            string[] data = { B64Data };

            await Client.SendAndRecieve(TypeOfCommunication.CreateGroupChannel, data);
        }

        private async void DmButton_Click(object sender, RoutedEventArgs e) {
            foreach (Border item in FriendsStackPanel.Children) {
                StackPanel stackpanel = (StackPanel)item.Child;
                CheckBox checkbox = (CheckBox)stackpanel.Children[0];
                if (checkbox.IsChecked == true) {
                    Label label = (Label)stackpanel.Children[2];

                    string ID = await GetID(label.Content.ToString(), Client);

                    string[] data = { ID };

                    await Client.SendAndRecieve(TypeOfCommunication.CreateDMChannel, data);
                }
            }
        }

        private async void AddUserElement(User user, bool removeButtonToggle, bool checkBoxToggle, bool dropDownToggle, bool acceptButtonToggle, StackPanel stackPanel) {
            // Create a friend element
            Border friendBorder = new Border();
            friendBorder.BorderBrush = Brushes.LightGray;
            friendBorder.BorderThickness = new Thickness(0, 0, 0, 1);
            friendBorder.Padding = new Thickness(5);
            friendBorder.Tag = user.username;

            StackPanel friendStackPanel = new StackPanel();
            friendStackPanel.Orientation = Orientation.Horizontal;

            CheckBox checkBox = new CheckBox {
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 10, 0)
            };

            string PFP = await GetPFP(user.ID, Client);

            BitmapImage pfp = new BitmapImage(new Uri(await CacheFileAsync(PFP)));
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = pfp;

            Ellipse ellipse = new Ellipse {
                Width = 25,
                Height = 25,
                Fill = imageBrush,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            

            Label label = new Label();
            label.Content = user.username;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.HorizontalAlignment = HorizontalAlignment.Left;

            ComboBox DropDownMenu_Roles = new ComboBox {
                IsEnabled = dropDownToggle,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 10, 0)
            };
            if (dropDownToggle) {
                DropDownMenu_Roles.Items.Add("Can't Read");
                DropDownMenu_Roles.Items.Add("Read Only");
                DropDownMenu_Roles.Items.Add("Read and Send");

                string[] data = { user.ID, CurrentChannelID };
                string role = await Client.SendAndRecieve(TypeOfCommunication.CheckRole, data);
                DropDownMenu_Roles.SelectedIndex = Convert.ToInt32(role) + 1;
                DropDownMenu_Roles.SelectionChanged += async (s, e) => {
                    if (DropDownMenu_Roles.SelectedItem != null) {
                        string roleSelected = (DropDownMenu_Roles.SelectedIndex + 1).ToString();
                        string[] data = { user.ID, CurrentChannelID, roleSelected, CurrentServerID };
                        await Client.SendAndRecieve(TypeOfCommunication.ChangeRole, data);
                    }
                };
            }


            Button removeButton = new Button();
            removeButton.Content = "X";
            removeButton.VerticalAlignment = VerticalAlignment.Center;
            removeButton.HorizontalAlignment = HorizontalAlignment.Right;
            removeButton.Width = 20;
            removeButton.Click += async (s, e) => {
                string[] data = { user.ID };
                await Client.SendAndRecieve(TypeOfCommunication.RejectRequest, data);
            };

            Button acceptButton = new Button();
            acceptButton.Content = "✓";
            acceptButton.VerticalAlignment = VerticalAlignment.Center;
            acceptButton.HorizontalAlignment = HorizontalAlignment.Right;
            acceptButton.Width = 20;
            acceptButton.Margin = new Thickness(5);
            acceptButton.Click += async (s, e) => {
                string[] data = { user.ID };
                await Client.SendAndRecieve(TypeOfCommunication.AcceptRequest, data);
            };


            // really hacky method of alligning an item to the right of a stackpanel using a spacer
            int SpacerWidth = 0;

            if (checkBoxToggle) {
                checkBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                SpacerWidth += Convert.ToInt32(checkBox.DesiredSize.Width);
            }

            ellipse.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            SpacerWidth += Convert.ToInt32(ellipse.DesiredSize.Width); // Width of the Ellipse

            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            SpacerWidth += Convert.ToInt32(label.DesiredSize.Width) ;

            if (dropDownToggle) {
                DropDownMenu_Roles.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                SpacerWidth += Convert.ToInt32(DropDownMenu_Roles.DesiredSize.Width);
            }

            SpacerWidth = Convert.ToInt32(Width - ((int)friendBorder.Margin.Right * 2)) - SpacerWidth - 150;

            FrameworkElement spacer = new FrameworkElement() {
                Width = SpacerWidth
            };

            // Add elements to the friendGrid
            if (checkBoxToggle) friendStackPanel.Children.Add(checkBox);
            friendStackPanel.Children.Add(ellipse);
            friendStackPanel.Children.Add(label);
            if (dropDownToggle) friendStackPanel.Children.Add(DropDownMenu_Roles);
            friendStackPanel.Children.Add(spacer);
            if (removeButtonToggle) friendStackPanel.Children.Add(removeButton);
            if (acceptButtonToggle) friendStackPanel.Children.Add(acceptButton);

            friendBorder.Child = friendStackPanel;

            // Add the friendBorder to the StackPanel
            stackPanel.Children.Add(friendBorder);
        }
    }
}


