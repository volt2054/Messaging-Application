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

        static void Main(string[] args) {
            Console.WriteLine("Dropping Tables");
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


                if (message.StartsWith("SEND")) {           // SEND MESSAGES

                } else if (message.StartsWith("CREATE")) {  // CREATE USER
                    string username = args[1];
                    string email = args[2];
                    string password = args[3];
                    responseMessage = InsertNewUser(username, email, password);
                    SelectAll();
                } else if (message.StartsWith("DELETE")) {  // DELETE MESSAGES

                } else if (message.StartsWith("GET")) {     // FETCH MESSAGES

                } else {

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

        private static void StartServer(out TcpListener listener) {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 7256;
            listener = new TcpListener(ipAddress, port);
            listener.Start();
            Console.WriteLine("Server started");
        }

        static void DeleteUser(string username) { }

        static string InsertNewUser(string username, string email, string password) {
            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Users (username, email, password) VALUES(@value1, @value2, @value3)";
                using (SqlCommand command = new SqlCommand(insertQuery, connection)) {
                    connection.Open();
                    command.Parameters.AddWithValue("@value1", username);
                    command.Parameters.AddWithValue("@value2", email);
                    command.Parameters.AddWithValue("@value3", password);

                    command.ExecuteNonQuery();
                }
            });

            List<string> result = new List<string>();


            ExecuteDatabaseOperations(connection => {
                string selectQuery = $"SELECT user_id FROM Users WHERE username = '{username}'";
                result = ExecuteQuery(connection, selectQuery);
            });

            return result.Last();
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
            string databaseFilePath = @"C:\Users\school work\Documents\dev\Messaging-Application\Server\Database.mdf";
            string connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={databaseFilePath};Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                databaseOperation(connection);
            }
        }

        static void DropTables() {
            try {
                ExecuteDatabaseOperations(connection => {
                    string command = "DROP TABLE Users";
                    ExecuteNonQuery(connection, command);

                });
                    Console.WriteLine("Tables dropped successfuly");
            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            }
        }

        static void CreateTables() {
            try {
                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[Users] (" +
                    "   [user_id]       INT           NOT NULL  IDENTITY(1,1)," +
                    "   [username]      VARCHAR (255) NOT NULL," +
                    "   [email]         VARCHAR (255) NOT NULL," +
                    "   [password]      VARCHAR (255) NOT NULL," +
                    "   [date_created]  DATETIME          NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY CLUSTERED ([user_id] ASC)" +
                    ");";

                    ExecuteNonQuery(connection, command);
                });
                Console.WriteLine("Tables created successfully");
            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
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