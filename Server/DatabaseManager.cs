﻿using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Database {
    public class DatabaseManager {
        private static string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True";
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

            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            } finally {
                Console.WriteLine("Tables created successfully");

            }

        }

        public static void DropTables() {
            try {
                ExecuteDatabaseOperations(connection => {
                    string command = "DROP TABLE Users; DROP TABLE Messages; DROP TABLE Channels";
                    ExecuteNonQuery(connection, command);

                });
                Console.WriteLine("Tables dropped successfuly");
            } catch (SqlException ex) {
                Console.WriteLine($"An error occured: {ex.Message}");
            }
        }


        public static void ExecuteDatabaseOperations(Action<SqlConnection> databaseOperation) {
            string connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                try {
                    databaseOperation(connection);
                } catch {
                    Console.WriteLine("Error");
                }
            }
        }

        public static void ExecuteNonQuery(SqlConnection connection, string sql) {
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

        public static void ExecuteNonQuery(SqlConnection connection, SqlCommand command) {
            using (command) {
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
        public static List<string> ExecuteQuery(SqlConnection connection, string sql) {
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

        public static List<string> ExecuteQuery(SqlConnection connection, SqlCommand command) {
            List<string> resultList = new List<string>();
            using (command) {
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

        public static void CreateDatabase() {
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
    }
}
