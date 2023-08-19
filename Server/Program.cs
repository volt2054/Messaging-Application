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

using static Server.Database.DatabaseManager;
using static Server.Database.DataManager;

namespace Server {
    class Server {

        // TODO - Loading options from file??
        const string DELIMITER = "|< delimiter >|"; //TODO replace with something else

        static void Main(string[] args) {

            CreateDatabase();

            //Console.WriteLine("Dropping Tables");
            DropTables();
            Console.WriteLine("Creating Tables");
            CreateTables();

            CreateChannel("test channel", "-1");

            bool isRunning = true;

            TcpListener listener;
            StartServer(out listener);

            while (isRunning) {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Client connected from {0}", client.Client.RemoteEndPoint);

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
                Console.WriteLine("Recieved message from {0}: {1}", client.Client.RemoteEndPoint, message);

                string responseMessage = "";
                string[] args = message.Split(DELIMITER);


                if (message.StartsWith(TypeOfCommunication.SendMessage)) {           // SEND MESSAGES
                    string message_content = args[1];
                    string channel = args[2];
                    string user = args[3];
                    responseMessage = InsertNewMessage(message_content, channel, user);

                } else if (message.StartsWith(TypeOfCommunication.RegisterUser)) {  // CREATE USER
                    string username = args[1];
                    string email = args[2];
                    string password = args[3];
                    responseMessage = InsertNewUser(username, email, password);
                    SelectAll();
                } else if (message.StartsWith(TypeOfCommunication.DeleteUser)) {  // DELETE USER
                    string userID = args[1];
                    DeleteUser(userID);
                    SelectAll();
                } else if (message.StartsWith(TypeOfCommunication.GetMessages)) {     // FETCH MESSAGES
                    string channel = args[1];
                    string message_from = args[2];
                    responseMessage = FetchMessages(channel, message_from).ToString(); ;
                } else if (message.StartsWith(TypeOfCommunication.GetID)) {
                    string username = args[1];
                    responseMessage = GetID(username);
                } else if (message.StartsWith(TypeOfCommunication.ValidateUser)) {
                    string username = args[1];
                    string email = args[2];
                    string password = args[3];
                    if (CheckUser(username, email, password)) { responseMessage = GetID(username); } else { responseMessage = "Bad Password"; }
                }

                byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessage);
                stream.Write(responseBytes, 0, responseBytes.Length);
            } catch (Exception ex) {
                Console.WriteLine("Error handling client: {0}", ex.Message);
            } finally {
                client.Close();
                Console.WriteLine("Client disconnected");
            }
        }


        

        


        
        



        


        
        

        
    }
}