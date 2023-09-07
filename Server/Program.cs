using System;
using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Net;
using System.Threading;
using System.Text;
using System.Diagnostics;


using SharedLibrary;
using static SharedLibrary.Serialization;

using static Server.Database.DatabaseManager;
using static Server.Database.DataManager;

using static Server.WebSocketServer;

namespace Server {
    
    class Server {

        // TODO - Loading options from file??

        const string ipAddress = "127.0.0.1";
        const int port = 7256;
        const string DELIMITER = "|< delimiter >|"; //TODO replace with something else


        static bool isRunning = true;

        static async Task Main(string[] args) {
            CreateDatabase();

            ExecuteDatabaseOperations(connection => {
                string del = "DELETE FROM Messages";
                ExecuteNonQuery(connection, del);
            });

            ExecuteDatabaseOperations(connection => {
                string del = "DELETE FROM Users";
                ExecuteNonQuery(connection, del);
            });
            ExecuteDatabaseOperations(connection => {
                string del = "DELETE FROM Channels";
                ExecuteNonQuery(connection, del);
            });

            

            //Console.WriteLine("Dropping Tables");
            DropTables();
            Console.WriteLine("Creating Tables");
            CreateTables();

            Task task = Task.Run(CommandLine);

            WebSocketServer webSocketServer = new WebSocketServer(ipAddress, port, HandleClient);
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

                    string channelID = CreateDMChannel(user1ID, user2ID);
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
                } else if (command == "EXIT") {
                    isRunning = false;
                    break;
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

                string[] args = message.Split(DELIMITER);

                clientID = args[0];
                string communicationType = args[1];

                args = args.Skip(2).ToArray();

                foreach (string arg in args) {
                    Console.WriteLine(arg);
                }

                userID = GetClientUserId(clientID);

                if (userID == null) {

                    if (communicationType == TypeOfCommunication.RegisterUser) {  // CREATE USER
                        string username = args[0];
                        string email = args[1];
                        string password = args[2];
                        userID = InsertNewUser(username, email, password);
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

                        List<string> usersInChannel = FetchUsersInChannel(channel);
                        foreach (string user in usersInChannel) {
                            // Send message to user client
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

                    } else if (communicationType == TypeOfCommunication.FetchChannels) { //TODO FETCH SERVER CHANNELS //FIXME

                        List<string[]> userChannels;

                        if (args.Length != 0) {
                            userChannels = FetchServerChannels(userID);
                        } else {
                            userChannels = FetchUserDMs(userID);
                        }

                        
                        byte[] channelsData = SerializeList(userChannels);
                        responseMessage = Convert.ToBase64String(channelsData);
                    } else if (communicationType == TypeOfCommunication.CreateDMChannel) {
                        string user1 = userID;
                        string user2 = args[0];
                        responseMessage = CreateDMChannel(user1, user2);
                    }
                }

                return responseMessage;

            } catch (Exception e) {
                return "-1";
            }
        }

        
    }
}