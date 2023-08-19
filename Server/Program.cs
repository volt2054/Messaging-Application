using System;
using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using System.Diagnostics;


using SharedLibrary;
using static SharedLibrary.Serialization;

using static Server.Database.DatabaseManager;
using static Server.Database.DataManager;
using System.Threading.Channels;

namespace Server {
    
    class Server {

        // TODO - Loading options from file??
        const string DELIMITER = "|< delimiter >|"; //TODO replace with something else

        static void Main(string[] args) {
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

            bool isRunning = true;

            TcpListener listener;
            StartServer(out listener);

            while (isRunning) {
                TcpClient client = listener.AcceptTcpClient();
                //Console.WriteLine("Client connected from {0}", client.Client.RemoteEndPoint);

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);

            }

            Console.ReadLine();
        }


        

        private static void HandleClient(object obj) {
            TcpClient client = (TcpClient)obj;

            try {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                //Console.WriteLine("Recieved message from {0}: {1}", client.Client.RemoteEndPoint, message);

                string responseMessage = "";
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

                byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessage);
                stream.Write(responseBytes, 0, responseBytes.Length);
            } catch (Exception ex) {
                //Console.WriteLine("Error handling client: {0}", ex.Message);
            } finally {
                client.Close();
                //Console.WriteLine("Client disconnected");
            }
        }


        

        


        
        



        


        
        

        
    }
}