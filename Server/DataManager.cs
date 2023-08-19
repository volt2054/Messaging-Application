using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;


using static Server.Database.DatabaseManager;

namespace Server.Database {
    public class DataManager {

        public static string InsertNewMessage(string message_content, string channel, string user) {
            string result = "";

            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Messages (message_content, channel_id, user_id) " +
                                     "VALUES (@MessageContent, @Channel, @User)";

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@MessageContent", message_content);
                command.Parameters.AddWithValue("@Channel", channel);
                command.Parameters.AddWithValue("@User", user);

                ExecuteNonQuery(connection, command);
            });

            SelectAllMessages();

            return result;
        }

        public static void SelectAllMessages() {
            List<string> result = new List<string>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT * FROM Messages";
                result = ExecuteQuery(connection, selectQuery);
            });

            foreach (string row in result) {
                Console.WriteLine(row);
            }
        }

        public static List<string> FetchMessages(string channel, string message_from) {
            List<string> result = new List<string>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT message_content FROM Messages WHERE channel_id = @Channel";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@Channel", channel);

                result = ExecuteQuery(connection, command);
            });

            return result;
        }

        public static string GetID(string username) {
            string result = "";

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT user_id FROM Users WHERE username = @Username";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@Username", username);

                List<string> queryResult = ExecuteQuery(connection, command);
                result = queryResult.First();
            });

            return result;
        }


        public static void StartServer(out TcpListener listener) {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 7256;
            listener = new TcpListener(ipAddress, port);
            listener.Start();
            Console.WriteLine("Server started");
        }

        public static void DeleteUser(string userID) {
            ExecuteDatabaseOperations(connection => {
                string deleteQuery = "DELETE FROM Users WHERE user_id = @UserID";

                SqlCommand command = new SqlCommand(deleteQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);

                ExecuteNonQuery(connection, command);
            });
        }

        public static string InsertNewUser(string username, string email, string password) {
            string userID = "";

            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Users (username, email, password) VALUES (@Username, @Email, @Password)";

                SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Username", username);
                insertCommand.Parameters.AddWithValue("@Email", email);
                insertCommand.Parameters.AddWithValue("@Password", password);

                ExecuteNonQuery(connection, insertCommand);
            });

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT user_id FROM Users WHERE username = @Username";
                SqlCommand selectCommand = new SqlCommand(selectQuery, connection);
                selectCommand.Parameters.AddWithValue("@Username", username);

                List<string> result = ExecuteQuery(connection, selectCommand);
                userID = result.Last();
            });

            return userID;
        }


        public static bool CheckUser(string username, string email, string password) {
            bool isValidUser = false;

            ExecuteDatabaseOperations(connection => {
                string searchQuery = "SELECT password FROM USERS WHERE email = @Email OR username = @Username";
                SqlCommand command = new SqlCommand(searchQuery, connection);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Username", username);

                List<string> result = ExecuteQuery(connection, command);

                if (result.Count > 0 && result.Last() == password) {
                    isValidUser = true;
                }
            });

            return isValidUser;
        }

        public static void CreateChannel(string channel_name, string server_id) {
            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Channels (channel_name, server_id) VALUES (@ChannelName, @ServerID)";

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@ChannelName", channel_name);

                // If server_id is "-1", set it as NULL; otherwise, set its value
                if (server_id == "-1") {
                    command.Parameters.AddWithValue("@ServerID", DBNull.Value);
                } else {
                    command.Parameters.AddWithValue("@ServerID", server_id);
                }

                ExecuteNonQuery(connection, command);
            });
        }


        public static void SelectAll() {
            List<string> result = new List<string>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT * FROM Users";
                result = ExecuteQuery(connection, selectQuery);
            });

            foreach (string row in result) {
                Console.WriteLine(row);
            }
        }

    }
}
