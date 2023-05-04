using System;
using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;


namespace Server {


    class Server {

        static void Main(string[] args) {
            Console.WriteLine("Dropping Tables");
            DropTables();
            Console.WriteLine("Creating Tables");
            CreateTables();
            Console.WriteLine("Inserting user");
            InsertNewUser("testuser123", "testemail123", "testpassword123");
            SelectAll();

            Console.ReadLine();
        }

        static void InsertNewUser(string username, string email, string password) {
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

        static void TestInsertAndSelect() {
            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Users (username, email, password) VALUES(@value1, @value2, @value3)";
                using (SqlCommand command = new SqlCommand(insertQuery, connection)) {
                    connection.Open();
                    command.Parameters.AddWithValue("@value1", "TestUser");
                    command.Parameters.AddWithValue("@value2", "TestEmail");
                    command.Parameters.AddWithValue("@value3", "TestPass");
                    command.ExecuteNonQuery();
                }
            });

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
            string databaseFilePath = @"C:\Users\ethan\Documents\dev\Server\database.mdf";
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