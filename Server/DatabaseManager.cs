using Microsoft.Data.SqlClient;
using SharedLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Database {
    public class DatabaseManager {

        private static string databaseName = "messaging_application";

        public static void TestDatabaseConnection() {
            try {
                ExecuteDatabaseOperations(connection => {
                    connection.Open();
                    Console.WriteLine("Successful connection");
                });
            } catch (Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static void CreateTables() {
            try {

                Console.WriteLine("Creating table users");

                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[Users] (" +
                    "   [user_id]       INT           NOT NULL  IDENTITY(1,1)," +
                    "   [username]      VARCHAR (255) NOT NULL," +
                    "   [email]         VARCHAR (255) NOT NULL," +
                    "   [password]      VARCHAR (255) NOT NULL," +
                    "   [date_created]  DATETIME      NOT NULL DEFAULT(getdate()), " +
                    "   [profile_picture]   VARCHAR(255) NULL  DEFAULT('PFP.png'), " +
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
                    "   [server_id]        INT            NULL," +
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

                Console.WriteLine("Creating table roles");
                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[Roles] (" +
                    "   [role_id]           INT         NOT NULL IDENTITY(1,1)," +
                    "   [role_name]         VARCHAR(255) NOT NULL," +
                    "   [server_id]         INT         NOT NULL," +
                    "   [date_created]      DATETIME    NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY CLUSTERED ([role_id] ASC)," +
                    "   FOREIGN KEY (server_id) REFERENCES Servers(server_id)" +
                    ");";

                    ExecuteNonQuery(connection, command);
                });

                Console.WriteLine("Creating table user_servers");
                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[UserServers] (" +
                    "   [user_id]           INT         NOT NULL," +
                    "   [server_id]         INT         NOT NULL," +
                    "   [date_joined]       DATETIME    NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY ([user_id], [server_id])," +
                    "   FOREIGN KEY (user_id) REFERENCES Users(user_id)," +
                    "   FOREIGN KEY (server_id) REFERENCES Servers(server_id)" +
                    ");";

                    ExecuteNonQuery(connection, command);
                });

                Console.WriteLine("Creating table channel_users");
                ExecuteDatabaseOperations(connection => {
                    string command =
                        "CREATE TABLE [dbo].[ChannelUsers] (" +
                        "   [channel_id] INT NOT NULL," +
                        "   [user_id] INT NOT NULL," +
                        "   [date_joined] DATETIME NOT NULL DEFAULT(getdate())," +
                        "   PRIMARY KEY ([channel_id], [user_id])," +
                        "   FOREIGN KEY (channel_id) REFERENCES Channels(channel_id)," +
                        "   FOREIGN KEY (user_id) REFERENCES Users(user_id)" +
                        ");";
                    ExecuteNonQuery(connection, command);
                });

                Console.WriteLine("Creating table channel_roles");
                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[ChannelRoles] (" +
                    "   [channel_id]        INT         NOT NULL," +
                    "   [role_id]           INT         NOT NULL," +
                    "   [date_created]      DATETIME    NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY ([channel_id], [role_id])," +
                    "   FOREIGN KEY (channel_id) REFERENCES Channels(channel_id)," +
                    "   FOREIGN KEY (role_id) REFERENCES Roles(role_id)" +
                    ");";

                    ExecuteNonQuery(connection, command);
                });

                Console.WriteLine("Creating table user_channel_roles");
                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[UserChannelRoles] (" +
                    "   [user_id]           INT         NOT NULL," +
                    "   [channel_id]        INT         NOT NULL," +
                    "   [role_id]           INT         NOT NULL," +
                    "   [date_created]      DATETIME    NOT NULL DEFAULT(getdate())," +
                    "   PRIMARY KEY ([user_id], [channel_id], [role_id])," +
                    "   FOREIGN KEY (user_id) REFERENCES Users(user_id)," +
                    "   FOREIGN KEY (channel_id) REFERENCES Channels(channel_id)," +
                    "   FOREIGN KEY (role_id) REFERENCES Roles(role_id)" +
                    ");";

                    ExecuteNonQuery(connection, command);
                });

                ExecuteDatabaseOperations(connection => {
                    string command =
                    "CREATE TABLE [dbo].[UserFriendships] (" +
                    "   [user_id]           INT         NOT NULL," +
                    "   [friend_id]        INT         NOT NULL," +
                    "   [date_created]      DATETIME    NOT NULL DEFAULT(getdate())," +
                    "   FOREIGN KEY (user_id) REFERENCES Users(user_id)," +
                    "   FOREIGN KEY (friend_id) REFERENCES Users(user_id)," +
                    ");";

                    ExecuteNonQuery(connection, command);
                });



            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            } finally {
                Console.WriteLine("Tables created successfully");

            }

        }

        public static void DropTables() {
            try {
                ExecuteDatabaseOperations(connection => {
                    string command =    "DROP TABLE [dbo].[UserFriendships];" +
                                        "DROP TABLE [dbo].[UserChannelRoles];" +
                                        "DROP TABLE [dbo].[ChannelRoles];" +
                                        "DROP TABLE [dbo].[ChannelUsers];" +
                                        "DROP TABLE [dbo].[UserServers];" +
                                        "DROP TABLE [dbo].[Messages];" +
                                        "DROP TABLE [dbo].[Roles];" +
                                        "DROP TABLE [dbo].[Channels];" +
                                        "DROP TABLE [dbo].[Servers];" +
                                        "DROP TABLE [dbo].[Users];";
                    ExecuteNonQuery(connection, command);

                });
                Console.WriteLine("Tables dropped successfuly");
            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            }
        }


        public static void ExecuteDatabaseOperations(Action<SqlConnection> databaseOperation) {
            string connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={AppDomain.CurrentDomain.BaseDirectory}\{databaseName}_database.mdf;Integrated Security=True;Database={databaseName}";


            using (SqlConnection connection = new SqlConnection(connectionString)) {
                try {
                    connection.Open();
                    databaseOperation(connection);
                    connection.Close();
                } catch {
                    Console.WriteLine("Error");
                }
            }
        }

        public static void ExecuteNonQuery(SqlConnection connection, string sql) {
            using (SqlCommand command = new SqlCommand(sql, connection)) {
                try {
                    int rowsAffected = command.ExecuteNonQuery();
                } catch (Exception e) {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
        }

        public static void ExecuteNonQuery(SqlConnection connection, SqlCommand command) {
            using (command) {
                try {
                    int rowsAffected = command.ExecuteNonQuery();
                } catch (Exception e) {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
        }
        public static List<T> ExecuteQuery<T>(SqlConnection connection, string sql) {
            List<T> resultList = new List<T>();
            using (SqlCommand command = new SqlCommand(sql, connection)) {
                try {
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) {
                        if (typeof(T) == typeof(string[])) {
                            string[] rowValues = new string[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++) {
                                rowValues[i] = reader[i].ToString();
                            }
                            resultList.Add((T)(object)rowValues);
                        } else if (typeof(T) == typeof(string)) {
                            resultList.Add((T)(object)reader[0].ToString());
                        }
                    }
                    reader.Close();
                } catch (Exception e) {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            return resultList;
        }

        public static List<T> ExecuteQuery<T>(SqlConnection connection, SqlCommand command) {
            List<T> resultList = new List<T>();
            using (command) {
                try {
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read()) {
                        if (typeof(T) == typeof(string[])) {
                            string[] rowValues = new string[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++) {
                                rowValues[i] = reader[i].ToString();
                            }
                            resultList.Add((T)(object)rowValues);
                        } else if (typeof(T) == typeof(string)) {
                            resultList.Add((T)(object)reader[0].ToString());
                        }
                    }
                    reader.Close();
                } catch (Exception e) {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            return resultList;
        }

        public static void DropDatabase() {
            try {
                Console.WriteLine($"Dropping database {databaseName}");

                string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True";

                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    connection.Open();

                    string command = $"DROP DATABASE {databaseName}";
                    ExecuteNonQuery(connection, command);

                    connection.Close();

                }

            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            } finally {
                Console.WriteLine("Database droppped");
            }
        }

        public static void CreateDatabase() {
            try {
                Console.WriteLine("CreatingDatabase");

                string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True";

                using (SqlConnection connection = new SqlConnection(connectionString)) {
                    connection.Open();

                    string command = $"CREATE DATABASE {databaseName} ON PRIMARY " +
                    "(NAME = messaging_application, " +
                    $"FILENAME = '{AppDomain.CurrentDomain.BaseDirectory}\\{databaseName}_database.mdf'," +
                    "SIZE = 3MB, MAXSIZE = 100MB, FILEGROWTH = 10%) ";
                    ExecuteNonQuery(connection, command);

                    connection.Close();

                }
            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            } finally {
                Console.WriteLine("Database created");
            }
        }
    }
}
