using Microsoft.Data.SqlClient;
using SharedLibrary;
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


            return result;
        }

        public static List<string[]> FetchMessages(string channelID, string messageID, bool fetchBefore, int count) {
            List<string[]> messages = new List<string[]>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                    "SELECT TOP " + count + " u.username, message_content, message_id, s.user_id " +
                    "FROM Messages s, Users u " +
                    "WHERE channel_id = @ChannelID AND " +
                    "u.user_id = s.user_id AND " +
                    (fetchBefore ? "message_id < @MessageID " : "message_id > @MessageID ") +
                    "ORDER BY message_id " + (fetchBefore ? "DESC" : "ASC");

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@ChannelID", channelID);
                command.Parameters.AddWithValue("@MessageID", messageID);

                messages = ExecuteQuery<string[]>(connection, command);
            });


            return messages;
        }

        public static List<User> FetchUsersInChannel(string ChannelID) {
            List<User> users;
            List<string> queryResult = new List<string>();
            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                    "SELECT user_id, username FROM ChannelUsers WHERE channel_id = @ChannelId;";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@ChannelID", ChannelID);

                queryResult = ExecuteQuery<string>(connection, command);
            });

            users = User.StringListToUserList(queryResult);
            
            if (users.Count > 0 ) { return users; }

            List<string> servers = new List<string>();
            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                "SELECT server_id FROM Channels WHERE channel_id = @ChannelID;";
                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@ChannelID", ChannelID);

                servers = ExecuteQuery<string>(connection, command);
            });

            if (servers.Count > 0) {
                ExecuteDatabaseOperations(connection => {
                    string selectQuery =
                        "SELECT user_id FROM UserServers WHERE server_id = @ServerID;";

                    SqlCommand command = new SqlCommand(selectQuery, connection);
                    command.Parameters.AddWithValue("@ServerID", servers.First());

                    queryResult = ExecuteQuery<string>(connection, command);
                });
            }

            users = User.StringListToUserList(queryResult);

            return users;
        }

        public static string AddFriend(string userID, string friendID) {
            string result = "-1";
            try {

            ExecuteDatabaseOperations(connection => {
                string insertQuery =
                    "INSERT INTO UserFriendships (user_id, friend_id) VALUES (@UserID, @FriendID);";

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);
                command.Parameters.AddWithValue("@FriendID", friendID);
                ExecuteNonQuery(connection, command);
            });
                return "0";
            } catch {
                return result;
            }

        }

        public static string RemoveFriend(string userID, string friendID) {
            string result = "-1";
            try {
                ExecuteDatabaseOperations(connection => {
                string insertQuery =
                    "DELETE FROM UserFriendships WHERE(user_id = @UserID AND friend_id = @FriendID)";

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);
                command.Parameters.AddWithValue("@FriendID", friendID);
                ExecuteNonQuery(connection, command);
                });
                return "0";
            } catch {
                return result;
            }
        }

        public static List<string> GetFriends(string userID) {
            List<string> result = new List<string>();
            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                "SELECT u.username AS friend_username " +
                "FROM Users u " +
                "INNER JOIN UserFriendships f ON u.user_id = f.friend_id " +
                "WHERE f.user_id = @UserID";
                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);
                result = ExecuteQuery<string>(connection, command);
            });
            return result;
        }

        public static List<string> GetUsersInServer(string serverID) {
            List<string> result = new List<string>();
            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                "SELECT u.username AS username " +
                "FROM Users u " +
                "INNER JOIN UserServers s ON u.user_id = s.user_id " +
                "WHERE s.server_id = @ServerID";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@ServerID", serverID);
                result = ExecuteQuery<string>(connection, command);
            });

            return result;
        }

        public static List<string[]> FetchUserDMs(string userID) {
            List<string[]> result = new List<string[]>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT c.channel_id, channel_name " +
                "FROM Channels c " +
                "INNER JOIN ChannelUsers cu ON c.channel_id = cu.channel_id " +
                "WHERE cu.user_id = @UserID " +
                "AND c.server_id IS NULL;";
                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);
                result = ExecuteQuery<string[]>(connection, command);
            });

            foreach (string[] channel in result) {
                string channelID = channel[0];
                List<string> username = new List<string>();
                ExecuteDatabaseOperations(connection => {
                    string selectQuery = "SELECT Users.username " +
                    "FROM ChannelUsers, Users " +
                    "WHERE ChannelUsers.user_id = Users.user_id " +
                    "AND channel_id = @ChannelID " +
                    "AND Users.user_id != @UserID";
                    SqlCommand command = new SqlCommand(selectQuery, connection);
                    command.Parameters.AddWithValue("@ChannelID", channelID);
                    command.Parameters.AddWithValue("@UserID", userID);
                    username = ExecuteQuery<string>(connection, command);

                    channel[1] = username.First();
                });
            }
            

            return result;
        }

        public static List<string[]> FetchServerChannels(string serverID) {
            List<string[]> result = new List<string[]>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                    "SELECT channel_id, channel_name " +
                    "FROM Channels " +
                    "WHERE server_id = @ServerID";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@ServerID", serverID);

                result = ExecuteQuery<string[]>(connection, command);
            });

            return result;
        }

        public static List<string[]> FetchServers(string userID) {
            List<string[]> result = new List<string[]>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                "SELECT server_name,Servers.server_id FROM UserServers, Servers " +
                "WHERE UserServers.server_id = Servers.server_id " +
                "AND user_id = @UserID";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);

                result = ExecuteQuery<string[]>(connection, command);
            });

            return result;
        }




        public static void InsertMessageIntoDMChannel(int channelID, int userID, string messageContent) {
            ExecuteDatabaseOperations(connection => {
                string insertQuery = $"INSERT INTO Messages (message_content, channel_id, user_id) VALUES (@MessageContent, @ChannelID, @UserID)";
                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@MessageContent", messageContent);
                command.Parameters.AddWithValue("@ChannelID", channelID);
                command.Parameters.AddWithValue("@UserID", userID);
                ExecuteNonQuery(connection, command);
            });
        }


        public static string GetID(string username) {
            string result = "";

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT user_id FROM Users WHERE username = @Username";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@Username", username);

                List<string> queryResult = ExecuteQuery<string>(connection, command);
                result = queryResult.First();
            });

            return result;
        }

        public static string GetUsername(string userID) {
            string result = "";
            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT username FROM Users WHERE user_id = @UserID";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);

                List<string> queryResult = ExecuteQuery<string>(connection, command);
                result = queryResult.First();
            });

            return result;
        }

        public static string GetProfilePicture(string userID) {
            string result = "";
            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT profile_picture From Users WHERE user_id = @UserID";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);

                List<string> queryResult = ExecuteQuery<string>(connection, command);
                result = queryResult.First();
            });
            return result;
        }

        public static void SetProfilePicture(string fileName, string userID) {
            ExecuteDatabaseOperations(connection => {
                string updateQuery = "UPDATE Users SET profile_picture = @FileName WHERE user_id = @UserID";

                SqlCommand command = new SqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@FileName", fileName);
                command.Parameters.AddWithValue("@UserID", userID);

                ExecuteNonQuery(connection, command);
            });
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
            string userID = "-1";

            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Users (username, email, password) VALUES (@Username, @Email, @Password); SELECT SCOPE_IDENTITY();";

                SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Username", username);
                insertCommand.Parameters.AddWithValue("@Email", email);
                insertCommand.Parameters.AddWithValue("@Password", password);

                userID = Convert.ToInt32(insertCommand.ExecuteScalar()).ToString();
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

                List<string> result = ExecuteQuery<string>(connection, command);

                if (result.Count > 0 && result.Last() == password) {
                    isValidUser = true;
                }
            });

            return isValidUser;
        }

        public static string CreateDMChannel(string User1ID, string User2ID, out string ChannelName) {
            int user1 = Convert.ToInt32(User1ID);
            int user2 = Convert.ToInt32(User2ID);
            string dmChannelName = $"DM_{Math.Min(user1, user2)}_{Math.Max(user1, user2)}";
            ChannelName = dmChannelName;

            int channelID = -1;
            ExecuteDatabaseOperations(connection => {
                string insertQuery = $"INSERT INTO Channels (channel_name) VALUES (@ChannelName); SELECT SCOPE_IDENTITY();";
                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@ChannelName", dmChannelName);
                channelID = Convert.ToInt32(command.ExecuteScalar());
            });

            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO ChannelUsers(channel_id, user_id) VALUES(@channelId, @userId)";
                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@channelID", channelID);
                command.Parameters.AddWithValue("@userID", user1);
                ExecuteNonQuery(connection, command);
            });

            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO ChannelUsers(channel_id, user_id) VALUES(@channelId, @userId)";
                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@channelID", channelID);
                command.Parameters.AddWithValue("@userID", user2);
                ExecuteNonQuery(connection, command);
            });

            return channelID.ToString();
        }

        public static string CreateGroupChannel(List<string> users, out string ChannelName) {
            string groupChatName = $"GC_";
            foreach(string user in users) {
                groupChatName += (user+"_");
            }
            ChannelName = groupChatName;


            int channelID = -1;
            ExecuteDatabaseOperations(connection => {
                string insertQuery = $"INSERT INTO Channels (channel_name) VALUES (@ChannelName); SELECT SCOPE_IDENTITY();";
                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@ChannelName", groupChatName);
                channelID = Convert.ToInt32(command.ExecuteScalar());
            });

            foreach (string user in users) {
                ExecuteDatabaseOperations(connection => {
                    string insertQuery = "INSERT INTO ChannelUsers(channel_id, user_id) VALUES(@channelId, @userId)";
                    SqlCommand command = new SqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@channelID", channelID);
                    command.Parameters.AddWithValue("@userID", user);
                    ExecuteNonQuery(connection, command);
                });
            }

            return channelID.ToString();
        }

        public static string CreateChannel(string channel_name, string server_id) {
            int channelID = -1;
            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Channels (channel_name, server_id) VALUES (@ChannelName, @ServerID); SELECT SCOPE_IDENTITY();";

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@ChannelName", channel_name);
                command.Parameters.AddWithValue("@ServerID", server_id);

                channelID = Convert.ToInt32(command.ExecuteScalar());
            });
            // need to add users to channel
            return channelID.ToString();
        }

        public static string CreateServer(string server_name, string server_description, string UserID, List<string> channels, List<string> friends) {
            int serverID = 0;
            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Servers (server_name, server_owner) VALUES (@ServerName, @ServerOwner); SELECT SCOPE_IDENTITY();";

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@ServerName", server_name);
                command.Parameters.AddWithValue("@ServerOwner", UserID);
                serverID = Convert.ToInt32(command.ExecuteScalar());
            });

            friends.Add(UserID);
            foreach (string friend in friends) {
                ExecuteDatabaseOperations(connection => {
                    string insertQuery = "INSERT INTO UserServers (server_id, user_id) VALUES (@ServerID, @UserID)";

                    SqlCommand command = new SqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@ServerID", serverID);
                    command.Parameters.AddWithValue("@UserID", friend);
                    ExecuteNonQuery(connection, command);
                });
            }
            

            foreach(string channel in channels) {
                ExecuteDatabaseOperations(connection => {
                    string insertQuery = "INSERT INTO Channels (channel_name, server_id) VALUES (@ChannelName, @ServerID)";

                    SqlCommand command = new SqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@ChannelName", channel);
                    command.Parameters.AddWithValue("@ServerID", serverID.ToString());

                    ExecuteNonQuery(connection,command);
                });
            }

            return serverID.ToString();
        }


        public static void SelectAll() {
            List<string> result = new List<string>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT * FROM Users";
                result = ExecuteQuery<string>(connection, selectQuery);
            });

        }

    }
}
