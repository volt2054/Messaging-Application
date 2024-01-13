using SharedLibrary;
using static SharedLibrary.Serialization;

using static Server.Database.DatabaseManager;
using static Server.Database.DataManager;

using static Server.WebSocketServer;
using System.ComponentModel.Design;
using Azure.Messaging;

namespace Server {

    class Server {
        static bool isRunning = true;

        static async Task Main(string[] args) {
            DropDatabase();
            CreateDatabase();

            Console.WriteLine("Dropping Tables");
            DropTables();
            Console.WriteLine("Creating Tables");
            CreateTables();

            string dc;
            InsertNewUser("test", "test", "test");
            InsertNewUser("test", "test", "test");
            string id = CreateDMChannel("1", "2", out dc);

            AssignRoleToUser("1", id, PermissionLevel.ReadWrite);

            Task task = Task.Run(CommandLine);

            WebSocketServer webSocketServer = new WebSocketServer(HandleClient);
            await webSocketServer.StartAsync();
        }

        static int requestCount = 0;

        static void CommandLine() {
            while (isRunning) {
                Console.Write("Server> ");
                string input = Console.ReadLine();

                string[] commandParts = input.Split(' ');
                string command = commandParts[0].ToUpper();

                if (command == "NEWUSER" && commandParts.Length == 4) {
                    string username = commandParts[1];
                    string email = commandParts[2];
                    string password = commandParts[3];

                    string userID = InsertNewUser(username, email, password);
                    Console.WriteLine($"User {userID} inserted successfully.");
                } else if (command == "GETID" && commandParts.Length == 2) {
                    string username = commandParts[1];
                    string userId = GetID(username);

                    Console.WriteLine($"User ID for {username}: {userId}");
                } else if (command == "NEWDMCHANNEL" && commandParts.Length == 3) {
                    string user1ID = commandParts[1];
                    string user2ID = commandParts[2];

                    string channelID = CreateDMChannel(user1ID, user2ID, out _); // discard result
                    Console.WriteLine($"DM channel {channelID} created successfully.");

                } else if (command == "NEWCHANNEL" && commandParts.Length == 3) {
                    string channelName = commandParts[1];
                    string serverID = commandParts[2];

                    string channelID = CreateChannel(channelName, serverID);
                    Console.WriteLine($"Channel {channelID} created successfully.");
                } else if (command == "DELETEUSER" && commandParts.Length == 2) {
                    string userId = commandParts[1];
                    DeleteUser(userId);
                    Console.WriteLine("User deleted successfully.");
                } else if (command == "TEST") {
                    List<User> users = new List<User>();
                    users.Add(new User("12345", "John Doe"));
                    users.Add(new User("67890", "Jane Smith"));

                    byte[] data = SerializeList(users);
                    List<User> deserializedUsers = DeserializeList<User>(data);

                    bool isEqual = users.SequenceEqual(deserializedUsers);

                    if (isEqual) {
                        Console.WriteLine("The deserialized list is equal to the original list.");
                    } else {
                        Console.WriteLine("The deserialized list is not equal to the original list.");
                    }
                } else {
                    Console.WriteLine("Invalid command.");
                }
            }

        }

        static void PrintRequestCount() {
            int past10seconds;
            int recording1;
            int recording2;
            float requestsPerSecond;
            while (isRunning) {
                recording1 = requestCount;
                Thread.Sleep(10000);
                recording2 = requestCount;
                past10seconds = recording2 - recording1;
                requestsPerSecond = past10seconds / 10;
                Console.WriteLine("Requests per second: " + requestsPerSecond.ToString());
            }
        }


        private static string HandleClient(string message) {
            requestCount++;
            string responseMessage = "";

            string clientID = ""; // should be able to fetch from messages
            string userID = "";


            try {



                string[] args = message.Split(WebSocketMetadata.DELIMITER);

                clientID = args[0];
                string communicationType = args[1];

                args = args.Skip(2).ToArray();

                userID = GetClientUserId(clientID);

                if (userID == null) {

                    if (communicationType == TypeOfCommunication.RegisterUser) {  // CREATE USER
                        string username = args[0];
                        string email = args[1];
                        string password = args[2];
                        userID = InsertNewUser(username, email, password);
                        SetClientUserId(clientID, userID);
                        responseMessage = Convert.ToString(userID);
                        SelectAll();

                    } else if (communicationType == TypeOfCommunication.ValidateUser) { // CHECK USER DETAILS
                        string username = args[0];
                        string email = args[1];
                        string password = args[2];
                        if (CheckUser(username, email, password)) {
                            userID = GetID(username);
                            SetClientUserId(clientID, userID);
                            responseMessage = userID;
                        } else { responseMessage = "Bad Password"; }
                    }
                } else {
                    if (communicationType == TypeOfCommunication.SendMessage) {           // SEND MESSAGES
                        string message_content = args[0];
                        string channel = args[1];
                        responseMessage = InsertNewMessage(message_content, channel, userID);

                        List<User> usersInChannel = FetchUsersInChannel(channel);
                        string[] argsToSend = new string[4];

                        // TODO CONVERT TO USER CLASS
                        argsToSend[0] = channel;
                        argsToSend[1] = GetUsername(userID);
                        argsToSend[2] = message_content;
                        argsToSend[3] = GetProfilePicture(userID);
                        foreach (User user in usersInChannel) {
                            SendMessageToUser(argsToSend, user.ID, TypeOfCommunication.NotifyMessage);
                        }

                    } else if (communicationType == TypeOfCommunication.SendAttachment) { // SEND ATTACHMENTS
                        string file_id = args[0];
                        string channel = args[1];
                        responseMessage = InsertNewAttachment(file_id, channel, userID);

                        List<User> usersInChannel = FetchUsersInChannel(channel);
                        string[] argsToSend = new string[4];

                        // TODO CONVERT TO USER CLASS
                        argsToSend[0] = channel;
                        argsToSend[1] = GetUsername(userID);
                        argsToSend[2] = file_id;
                        argsToSend[3] = GetProfilePicture(userID);
                        foreach (User user in usersInChannel) {
                            SendMessageToUser(argsToSend, user.ID, TypeOfCommunication.NotifyAttachment);
                        }

                    } else if (communicationType == TypeOfCommunication.FetchMessages) {     // FETCH MESSAGES
                        string channel = args[0];
                        string message_from = args[1];
                        bool before = (args[2] == "true");
                        List<string[]> messageList = FetchMessages(channel, message_from, before, 10);

                        byte[] messageData = SerializeList(messageList);
                        responseMessage = Convert.ToBase64String(messageData);
                    } else if (communicationType == TypeOfCommunication.GetID) {
                        string username = args[0];
                        responseMessage = GetID(username);

                    } else if (communicationType == TypeOfCommunication.FetchChannels) {

                        List<string[]> userChannels;

                        if (args.Length != 0) {
                            string serverID = args[0];
                            userChannels = FetchServerChannels(serverID);
                        } else {
                            userChannels = FetchUserDMs(userID);
                        }

                        byte[] channelsData = SerializeList(userChannels);
                        responseMessage = Convert.ToBase64String(channelsData);

                    } else if (communicationType == TypeOfCommunication.FetchServers) {
                        List<string[]> userServers;

                        userServers = FetchServers(userID);

                        byte[] serversData = SerializeList(userServers);
                        responseMessage = Convert.ToBase64String(serversData);


                    } else if (communicationType == TypeOfCommunication.CreateDMChannel) {
                        string user1 = userID;
                        string user2 = args[0];
                        string channelName;
                        string channelID = CreateDMChannel(user1, user2, out channelName);
                        responseMessage = channelID;

                        string[] argsToSend = new string[3];
                        argsToSend[0] = channelID;
                        argsToSend[1] = GetUsername(userID);
                        argsToSend[2] = "-1";
                        SendMessageToUser(argsToSend, user2, TypeOfCommunication.NotifyChannel);

                    } else if (communicationType == TypeOfCommunication.CreateServer) {
                        string serverName = args[0];
                        string serverDescription = args[1];

                        string SerializedChannelsString = args[2];
                        byte[] SerializedChannels = Convert.FromBase64String(SerializedChannelsString);

                        List<string> Channels = DeserializeList<string>(SerializedChannels);

                        string SerializedFriendsString = args[3];
                        byte[] SerializedFriends = Convert.FromBase64String(SerializedFriendsString);

                        List<string> Friends = DeserializeList<string>(SerializedFriends);

                        string id = CreateServer(serverName, serverDescription, userID, Channels, Friends);
                        string[] argsToSend = new string[2];
                        argsToSend[0] = id;
                        argsToSend[1] = serverName;
                        foreach (string friend in Friends) {
                            if (friend != userID)
                                SendMessageToUser(argsToSend, friend, TypeOfCommunication.NotifyServer);
                        }
                    } else if (communicationType == TypeOfCommunication.CreateChannel) {
                        string channelName = args[0];
                        string serverID = args[1];

                        string channelID = CreateChannel(channelName, serverID);

                        responseMessage = channelID;

                        List<User> usersInChannel = FetchUsersInChannel(channelID);
                        string[] argsToSend = new string[3];
                        argsToSend[0] = channelID;
                        argsToSend[1] = channelName;
                        argsToSend[2] = serverID;
                        foreach (User user in usersInChannel) {
                            SendMessageToUser(argsToSend, user.ID, TypeOfCommunication.NotifyChannel);
                        }

                    } else if (communicationType == TypeOfCommunication.CreateGroupChannel) {
                        byte[] usersData = Convert.FromBase64String(args[0]);
                        List<string> users = DeserializeList<string>(usersData);
                        string channelName;
                        string channelID = CreateGroupChannel(users, out channelName);
                        responseMessage = channelID;

                        string[] argsToSend = new string[2];
                        argsToSend[0] = channelID;
                        argsToSend[1] = channelName;
                        foreach (string user in users) {
                            SendMessageToUser(argsToSend, user, TypeOfCommunication.NotifyChannel);
                        }
                    } else if (communicationType == TypeOfCommunication.AddFriend) {
                        string user1 = userID;
                        string user2 = args[0];
                        responseMessage = AddFriend(user1, user2);
                    } else if (communicationType == TypeOfCommunication.RemoveFriend) {
                        string user1 = userID;
                        string user2 = args[0];
                        responseMessage = RemoveFriend(user1, user2);
                    } else if (communicationType == TypeOfCommunication.GetFriends) {
                        string user1 = userID;

                        List<User> friends = GetFriends(userID);
                        byte[] friendsData = SerializeList(friends);
                        responseMessage = Convert.ToBase64String(friendsData);

                    } else if (communicationType == TypeOfCommunication.GetUsersInServer) {
                        string serverID = args[0];

                        List<User> users = GetUsersInServer(serverID);
                        byte[] usersData = SerializeList(users);
                        responseMessage = Convert.ToBase64String(usersData);

                    } else if (communicationType == TypeOfCommunication.GetUsername) {
                        string UserIDToCheck = args[0];
                        string username = GetUsername(userID);
                        //TODO WHAT IS HAPPENING HERE?

                        responseMessage = username;
                    } else if (communicationType == TypeOfCommunication.GetProfilePicture) {
                        string userIDToCheck = args[0];

                        string profilePicture = GetProfilePicture(userIDToCheck);
                        responseMessage = profilePicture;
                    } else if (communicationType == TypeOfCommunication.SetProfilePicture) {
                        string fileName = args[0];
                        SetProfilePicture(fileName, userID);
                        responseMessage = "1";
                    }
                }

                return responseMessage;

            } catch {
                return "-1";
            }
        }
    }
}