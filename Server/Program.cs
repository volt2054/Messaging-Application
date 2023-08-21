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

                    int userID = InsertNewUser(username, email, password);
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

            try {

                string[] args = message.Split(DELIMITER);


                if (args[0] == TypeOfCommunication.SendMessage) {           // SEND MESSAGES
                    string message_content = args[1];
                    string channel = args[2];
                    string user = args[3];
                    responseMessage = InsertNewMessage(message_content, channel, user);

                } else if (args[0] == TypeOfCommunication.RegisterUser) {  // CREATE USER
                    string username = args[1];
                    string email = args[2];
                    string password = args[3];
                    responseMessage = Convert.ToString(InsertNewUser(username, email, password));
                    SelectAll();
                } else if (args[0] == TypeOfCommunication.DeleteUser) {  // DELETE USER
                    string userID = args[1];
                    DeleteUser(userID);
                    SelectAll();
                } else if (args[0] == TypeOfCommunication.FetchMessages) {     // FETCH MESSAGES
                    string channel = args[1];
                    string message_from = args[2];
                    bool before = (args[3] == "true");
                    List<string[]> messageList = FetchMessages(channel, message_from, before, 10);

                    byte[] messageData = SerializeList(messageList);
                    responseMessage = Convert.ToBase64String(messageData);
                } else if (args[0] == TypeOfCommunication.GetID) {
                    string username = args[1];
                    responseMessage = GetID(username);
                } else if (args[0] == TypeOfCommunication.ValidateUser) {
                    string username = args[1];
                    string email = args[2];
                    string password = args[3];
                    if (CheckUser(username, email, password)) { responseMessage = GetID(username); } else { responseMessage = "Bad Password"; }
                } else if (args[0] == TypeOfCommunication.FetchChannels) { //TODO FETCH SERVER CHANNELS
                    string userID = args[1];
                    List<string[]> userChannels = FetchUserDMs(userID);
                    byte[] channelsData = SerializeList(userChannels);
                    responseMessage = Convert.ToBase64String(channelsData);
                } else if (args[0] == TypeOfCommunication.CreateDMChannel) {
                    string user1 = args[1];
                    string user2 = args[2];
                    responseMessage = CreateDMChannel(user1, user2);
                }

                return responseMessage;

            } catch (Exception e) {
                return "-1";
            }
        }
    }
}