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

namespace Server {
    class Server {

        // TODO - Loading options from file??

        static void Main(string[] args) {

            //CreateDatabase();

            //Console.WriteLine("Dropping Tables");
            DropTables();
            Console.WriteLine("Creating Tables");
            CreateTables();

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


        public class TypeOfCommunication {
            public static readonly string SendMessage = "SEND"; // (SEND + MESSAGE CONTENT) RETURNS WHETHER SUCCESSFUL
            public static readonly string GetMessages = "GET"; // (GET + CHANNEL ID) RETURNS RECENTLY SENT MESSAGES
            public static readonly string GetID = "GETUSERID"; // (GETUSERID + USERNAME)  RETURNS ID GIVEN USERNAME
            public static readonly string RegisterUser = "CREATE"; // (CREATE + USERNAME + EMAIL + PASSWORD) RETURNS WHETHER SUCCESSFUL
            public static readonly string ValidateUser = "CHECK"; // (CHECK + USERNAME + PASSWORD) RETURNS WHETHER SUCCESSFUL
            public static readonly string DeleteUser = "DELETEUSER"; // (DELETE + USERID) RETURNS WHETHER SUCCESSFUL
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
                string[] args = message.Split(" ");


                if (message.StartsWith(TypeOfCommunication.SendMessage)) {           // SEND MESSAGES

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

        private static string GetID(string username) {
            string result = "";
            ExecuteDatabaseOperations(connection => {
                string selectQuery = $"SELECT user_id FROM Users WHERE username = '{username}'";
                result = ExecuteQuery(connection, selectQuery)[0];
            });
            return result;
        }

        private static void StartServer(out TcpListener listener) {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 7256;
            listener = new TcpListener(ipAddress, port);
            listener.Start();
            Console.WriteLine("Server started");
        }

        static void DeleteUser(string userID) {
            ExecuteDatabaseOperations(connection => {
                string deleteQuery = $"DELETE FROM Users WHERE user_id = '{userID}'";
                ExecuteNonQuery(connection, deleteQuery);
            });
        }

        static string InsertNewUser(string username, string email, string password) {
            ExecuteDatabaseOperations(connection => {
                string insertQuery = $"INSERT INTO Users (username, email, password) VALUES('{username}', '{email}', '{password}')";
                ExecuteNonQuery(connection, insertQuery);
            });

            List<string> result = new List<string>();


            ExecuteDatabaseOperations(connection => {
                string selectQuery = $"SELECT user_id FROM Users WHERE username = '{username}'";
                result = ExecuteQuery(connection, selectQuery);
            });

            return result.Last();
        }

        static bool CheckUser(string username, string email, string password) {
            List<string> result = new List<string>();

            ExecuteDatabaseOperations(connection => {
                string searchQuery = $"SELECT password FROM USERS WHERE email = '{email}' OR username = '{username}'";
                result = ExecuteQuery(connection, searchQuery);
            });

            if (result.Last() != password) return false;

            return true;
        }


        static void SelectAll() {
            List<string> result = new List<string>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT * FROM Users";
                result = ExecuteQuery(connection, selectQuery);
            });

            foreach (string row in result) {
                Console.WriteLine(row);
            }
        }

        static void TestDatabaseConnection() {
            try {
                ExecuteDatabaseOperations(connection => {
                    connection.Open();
                    Console.WriteLine("Successful connection");
                });
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void ExecuteDatabaseOperations(Action<SqlConnection> databaseOperation) {
            string connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                try {
                    databaseOperation(connection);
                } catch {
                    Console.WriteLine("Error");
                }
            }
        }

        static void DropTables() {
            try {
                ExecuteDatabaseOperations(connection => {
                    string command = "DROP TABLE Users; DROP TABLE Messages";
                    ExecuteNonQuery(connection, command);

                });
                Console.WriteLine("Tables dropped successfuly");
            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            }
        }


        static void CreateDatabase() {
            try {
                Console.WriteLine("CreatingDatabase");
                ExecuteDatabaseOperations(connection => {
                    string command = "CREATE DATABASE messaging_application ON PRIMARY " +
                    "(NAME = messaging_application, " +
                    "FILENAME = 'C:\\Users\\ethan\\Documents\\dev\\c#\\Messaging-Application\\database.mdf'," +
                    "SIZE = 3MB, MAXSIZE = 100MB, FILEGROWTH = 10%) ";
                    ExecuteNonQuery(connection, command);

                });
            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            } finally {
                Console.WriteLine("Database created");
            }
        }


        static void CreateTables() {
            try {



                Console.WriteLine("Creating table users");

                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[Users] (" +
                    "   [user_id]       INT           NOT NULL  IDENTITY(1,1)," +
                    "   [username]      VARCHAR (255) NOT NULL," +
                    "   [email]         VARCHAR (255) NOT NULL," +
                    "   [password]      VARCHAR (255) NOT NULL," +
                    "   [date_created]  DATETIME      NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY CLUSTERED ([user_id] ASC)" +
                    ");";

                    ExecuteNonQuery(connection, command);
                });

                Console.WriteLine("Creating table servers");
                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[Servers] (" +
                    "   [server_id]         INT             NOT NULL IDENTITY(1,1)," +
                    "   [server_name]       VARCHAR (255)   NOT NULL," +
                    "   [server_owner]      INT             NOT NULL," +
                    "   [date_created]      DATETIME        NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY CLUSTERED ([server_id] ASC)," +
                    "   FOREIGN KEY (server_owner) REFERENCES Users(user_id)" +
                    ");";

                    ExecuteNonQuery(connection, command);

                });

                Console.WriteLine("Creating table channels ");
                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[Channels] (" +
                    "   [channel_id]       INT            NOT NULL IDENTITY(1,1)," +
                    "   [channel_name]     VARCHAR (255)  NOT NULL," +
                    "   [server_id]        INT            NOT NULL," +
                    "   [date_created]     DATETIME       NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY CLUSTERED ([channel_id] ASC)," +
                    "   FOREIGN KEY (server_id) REFERENCES Servers(server_id)" +
                    ");";

                    ExecuteNonQuery(connection, command);

                });

                Console.WriteLine("Creating table messages");
                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[Messages] (" +
                    "   [message_id]         INT         NOT NULL IDENTITY(1,1)," +
                    "   [message_content]    TEXT        NOT NULL," +
                    "   [channel_id]         INT         NOT NULL," +
                    "   [user_id]            INT         NOT NULL," +
                    "   [date_sent]          DATETIME    NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY CLUSTERED ([message_id] ASC)," +
                    "   FOREIGN KEY (channel_id) REFERENCES Channels(channel_id)," +
                    "   FOREIGN KEY (user_id) REFERENCES Users(user_id)" +
                    ");";

                    ExecuteNonQuery(connection, command);
                });

            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            } finally {
                Console.WriteLine("Tables created successfully");

            }
        }

        static void ExecuteNonQuery(SqlConnection connection, string sql) {
            using (SqlCommand command = new SqlCommand(sql, connection)) {
                try {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine("Rows Affected: " + rowsAffected);
                } catch (Exception e) {
                    Console.WriteLine("Error: " + e.Message);
                } finally {
                    connection.Close();
                }
            }
        }
        static List<string> ExecuteQuery(SqlConnection connection, string sql) {
            List<string> resultList = new List<string>();
            using (SqlCommand command = new SqlCommand(sql, connection)) {
                try {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) {
                        string rowString = "";
                        for (int i = 0; i < reader.FieldCount; i++) {
                            rowString += reader[i].ToString() + ",";
                        }
                        resultList.Add(rowString.TrimEnd(','));
                    }
                    reader.Close();
                } catch (Exception e) {
                    Console.WriteLine("Error: " + e.Message);
                } finally {
                    connection.Close();
                }
            }
            return resultList;
        }
    }
}