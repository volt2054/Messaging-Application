using Azure.Messaging;
using Microsoft.Data.SqlClient;
using SharedLibrary;
using static Server.Database.DatabaseManager;

namespace Server.Database {
    public class DataManager {

        public static string InsertNewMessage(string message_content, string channel, string user) {
            string result = "-1";

            if (GetUserRole(user, channel) == PermissionLevel.ReadWrite)
            {
                ExecuteDatabaseOperations(connection => {
                    string insertQuery = "INSERT INTO Messages (message_content, channel_id, user_id) " +
                                         "VALUES (@MessageContent, @Channel, @User)";

                    SqlCommand command = new SqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@MessageContent", message_content);
                    command.Parameters.AddWithValue("@Channel", channel);
                    command.Parameters.AddWithValue("@User", user);

                    ExecuteNonQuery(connection, command);
                });
                result = "1";
            }

            return result;
        }

        public static void ChangeUsername(string user_id, string username) {
            ExecuteDatabaseOperations(connection => {
                string updateQuery = 
                "UPDATE Users " +
                "SET Username = @Username " +
                "WHERE user_id = @UserID";

                SqlCommand command = new SqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@UserID", user_id);

                ExecuteNonQuery(connection, command);
            });
        }

        public static void ChangePassword(string user_id, string password) {
            ExecuteDatabaseOperations(connection => {
                string updateQuery =
                "UPDATE Users " +
                "SET Password = @Password " +
                "WHERE user_id = @UserID";

                SqlCommand command = new SqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@Password", password);
                command.Parameters.AddWithValue("@UserID", user_id);

                ExecuteNonQuery(connection, command);
            });
        }


        public static bool DoesUserOwnServer(string userId, string serverId) {
            List<string> result = new List<string>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT server_owner " +
                "FROM Servers " +
                "WHERE server_id = @serverId";

                SqlCommand command = new SqlCommand(selectQuery, connection);

                command.Parameters.AddWithValue("@serverId", serverId);

                result = ExecuteQuery<string>(connection, command);
            });

            if (result.First() == userId) {
                return true;
            }

            return false;
        }

        public static string InsertNewAttachment(string fileId, string channel, string user) {
            string result = "-1";

            if (GetUserRole(user, channel) == PermissionLevel.ReadWrite)
            {
                ExecuteDatabaseOperations(connection => {
                    string insertQuery = "INSERT INTO Messages (message_content, channel_id, user_id, message_type) " +
                                         "VALUES (@fileID, @Channel, @User, 2)";

                    SqlCommand command = new SqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@fileID", fileId);
                    command.Parameters.AddWithValue("@Channel", channel);
                    command.Parameters.AddWithValue("@User", user);

                    ExecuteNonQuery(connection, command);
                });
                result = "1";
            }

            return result;
        }

        // makes sure messages are fetched in correct order
        public static List<string[]> FetchMessages(string channelID, string messageID, bool fetchBefore, int count) {
            List<string[]> messages = new List<string[]>();

            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                    "SELECT TOP " + count + " u.username, message_content, message_id, s.user_id, message_type " +
                    "FROM Messages s, Users u " +
                    "WHERE channel_id = @ChannelID AND " +
                    "u.user_id = s.user_id AND " +
                    (fetchBefore ? "message_id < @MessageID " : "message_id > @MessageID ") +
                    // if we are fetching before a specific id then less than if not greater than
                    "ORDER BY message_id " + (fetchBefore ? "DESC" : "ASC");
                    // if we are fetching before sort by descending

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@ChannelID", channelID);
                command.Parameters.AddWithValue("@MessageID", messageID);

                messages = ExecuteQuery<string[]>(connection, command);
            });


            return messages;
        }

        public static List<User> FetchUsersInChannel(string ChannelID) {
            List<User> users;
            List<string[]> queryResult = new List<string[]>();
            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                    "SELECT ChannelUsers.user_id, Users.username " +
                    "FROM ChannelUsers, Users " +
                    "WHERE channel_id = @ChannelId " +
                    "AND ChannelUsers.user_id = Users.user_id;";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@ChannelID", ChannelID);

                queryResult = ExecuteQuery<string[]>(connection, command);
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

                    queryResult = ExecuteQuery<string[]>(connection, command);
                });

                
            }
            List<string[]> queryResultWithUsernames = new List<string[]>();
            for (int i = 0; i < queryResult.Count; ++i) {
                string[] newResult = new string[2];

                string[] result = queryResult.ElementAt(i);
                string id = result[0];
                string username = GetUsername(id);
                newResult[0] = id;
                newResult[1] = username;
                queryResultWithUsernames.Add(newResult);
            }

            users = User.StringListToUserList(queryResultWithUsernames);

            return users;
        }

        public static string AddFriend(string userID, string friendID) {
            string result = "-1";

            List<User> pendingFriends =  GetFriends(userID, FriendStatus.Pending);
            List<User> acceptedFriends =  GetFriends(userID, FriendStatus.Accepted);

            foreach (User friend in pendingFriends) {
                if(friend.ID == friendID) {
                    return "0";
                }
            }

            foreach (User friend in acceptedFriends) {
                if (friend.ID == friendID) {
                    return "0";
                }
            }

            List<User> friendsPendingFriends = GetFriends(friendID, FriendStatus.Pending);

            foreach (User friend in friendsPendingFriends) {
                if (friend.ID == userID) {
                    AcceptFriendRequest(userID, friendID); return "1";
                }
            }

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
        public static string AcceptFriendRequest(string userID, string friendID) {
            try {
                ExecuteDatabaseOperations(connection => {
                    string updateQuery =
                        "UPDATE UserFriendships " +
                        "SET status = 'Accepted' " +
                        "WHERE user_id = @UserID AND friend_id = @FriendID;";

                    SqlCommand command = new SqlCommand(updateQuery, connection);
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@FriendID", friendID);
                    ExecuteNonQuery(connection, command);
                });

                return "0"; // Successful acceptance
            } catch {
                return "-1"; // Failed to accept
            }
        }

        public static string RemoveFriend(string userID, string friendID) {
            string result = "-1";
            try {
                ExecuteDatabaseOperations(connection => {
                string insertQuery =
                    "DELETE FROM UserFriendships " +
                    "WHERE (user_id = @UserID AND friend_id = @FriendID)" +
                    "OR (friend_id = @UserID AND user_id = @FriendID)";

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

        public static List<User> GetFriends(string userID, string status) {
            List<User> friends;
            List<string[]> queryResult = new List<string[]>();

            if (status == FriendStatus.Pending) {
                ExecuteDatabaseOperations(connection => {
                    string selectQuery =
                    "SELECT UserFriendships.user_id, Users.username " +
                    "FROM UserFriendships " +
                    "JOIN Users ON UserFriendships.user_id = Users.user_id " +
                    "WHERE UserFriendships.friend_id = @UserID " +
                    "AND UserFriendships.status = @Status";
                    SqlCommand command = new SqlCommand(selectQuery, connection);
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@Status", status);
                    queryResult = ExecuteQuery<string[]>(connection, command);
                });

                friends = User.StringListToUserList(queryResult);

                return friends;
            } else {
                ExecuteDatabaseOperations(connection =>
                {
                    // As friends can now either be on user_id or friend_id we have to use a union to join 2 seperate querys individually selecting each possible case
                    string selectQuery =
                        "SELECT u.user_id, u.username " +
                        "FROM UserFriendships uf " +
                        "JOIN Users u ON uf.friend_id = u.user_id " +
                        "WHERE uf.user_id = @UserID AND uf.status = @Status " +
                        "UNION " +
                        "SELECT u.user_id, u.username " +
                        "FROM UserFriendships uf " +
                        "JOIN Users u ON uf.user_id = u.user_id " +
                        "WHERE uf.friend_id = @UserID AND uf.status = @Status";

                    SqlCommand command = new SqlCommand(selectQuery, connection);
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@Status", status);

                    queryResult = ExecuteQuery<string[]>(connection, command);
                });

                friends = User.StringListToUserList(queryResult);
                return friends;
            }
        }

        public static List<(User, int)> GetFriendsOfFriends(string userID, int depth) {
            var result = new List<(User, int)>();

            HashSet<string> visited = new HashSet<string>();
            visited.Add(userID);
            GetFriendsOfFriendsRecursive(userID, depth, result, visited, 0);

            foreach(var friend in result) {
                Console.WriteLine(friend.Item1.username);
                Console.WriteLine(friend.Item2.ToString());
            }

            return result;
        }

        private static void GetFriendsOfFriendsRecursive(string userID, int depth, List<(User, int)> result, HashSet<string> visited, int currentDepth) {
            // base case: if depth is less than 0, return immediately
            if (depth < 0)
                return;

            // Get direct friends
            var directFriends = GetFriends(userID, FriendStatus.Accepted);

            // Iterate through each direct friend
            foreach (var friend in directFriends) {
                // Check if the friend has been visited before
                if (!visited.Contains(friend.ID)) {
                    // Add the friend to the visited set
                    visited.Add(friend.ID);

                    // Add the friend and their depth to the result list
                    result.Add((friend, currentDepth));

                    // If depth is greater than 0, recursively call the function with the friend's ID
                    // and decremented depth, passing the result list and visited set
                    if (depth > 0)
                        GetFriendsOfFriendsRecursive(friend.ID, depth - 1, result, visited, currentDepth+1);
                }
            }
        }

        public static List<User> GetUsersInServer(string serverID) {
            List<User> users;
            List<string[]> result = new List<string[]>();
            ExecuteDatabaseOperations(connection => {
                string selectQuery =
                "SELECT u.user_id, u.username AS username " +
                "FROM Users u " +
                "INNER JOIN UserServers s ON u.user_id = s.user_id " +
                "WHERE s.server_id = @ServerID";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@ServerID", serverID);
                result = ExecuteQuery<string[]>(connection, command);
            });

            users = User.StringListToUserList(result);

            return users;
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

        public static List<string[]> FetchServerChannels(string serverID, string userID) {
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

            //filter results by roles
            foreach (string[] channel in result) {
                string channelID = channel[0];
                int role = GetUserRole(userID, channelID);

                if (role == PermissionLevel.NoAccess) {
                    result.Remove(channel);
                }
            }

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

        public static string GetID(string username) {
            string result = "";

            try {
                ExecuteDatabaseOperations(connection => {
                    string selectQuery = "SELECT user_id FROM Users WHERE username = @Username";

                    SqlCommand command = new SqlCommand(selectQuery, connection);
                    command.Parameters.AddWithValue("@Username", username);

                    List<string> queryResult = ExecuteQuery<string>(connection, command);
                    result = queryResult.First();
                });
            } catch {
                result = "-1";
            }
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

        public static string InsertNewUser(string username, string password) {
            string userID = "-1";

            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Users (username, password) VALUES (@Username, @Password); SELECT SCOPE_IDENTITY();";

                SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Username", username);
                insertCommand.Parameters.AddWithValue("@Password", password);

                userID = Convert.ToInt32(insertCommand.ExecuteScalar()).ToString();
            });

            return userID;
        }


        public static bool CheckUser(string username, string password) {  
            bool isValidUser = false;

            ExecuteDatabaseOperations(connection => {
                string searchQuery = "SELECT password FROM USERS WHERE username = @Username";
                SqlCommand command = new SqlCommand(searchQuery, connection);
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
            return channelID.ToString();
        }

        public static string CreateServer(string server_name, string server_description, string UserID, List<string> channels, List<string> users) {
            int serverID = 0;
            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO Servers (server_name, server_owner) VALUES (@ServerName, @ServerOwner); SELECT SCOPE_IDENTITY();";

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@ServerName", server_name);
                command.Parameters.AddWithValue("@ServerOwner", UserID);
                serverID = Convert.ToInt32(command.ExecuteScalar());
            });

            foreach (string friend in users) {
                AddUserToServer(friend, serverID.ToString());
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

        public static void AddUserToServer(string userId, string serverId) {
            ExecuteDatabaseOperations(connection => {
                string insertQuery = "INSERT INTO UserServers (server_id, user_id) VALUES (@ServerID, @UserID)";

                SqlCommand command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@ServerID", serverId);
                command.Parameters.AddWithValue("@UserID", userId);
                ExecuteNonQuery(connection, command);
            });
        }

        public static void RemoveUserFromServer(string userId, string serverId) {
            ExecuteDatabaseOperations(connection => {
                string deleteQuery = "DELETE FROM UserServers WHERE " +
                "user_id = @UserID AND server_id = @ServerID";

                SqlCommand command = new SqlCommand(deleteQuery, connection);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@ServerID", serverId);
                ExecuteNonQuery(connection, command);
            });
        }

        public static void AssignRoleToUser(string userID, string channelID, int permissionLevel) {
            ExecuteDatabaseOperations(connection => {
                // Check if the entry already exists
                string selectQuery = "SELECT COUNT(*) FROM UserChannelRoles WHERE user_id = @UserID AND channel_id = @ChannelID";
                SqlCommand selectCommand = new SqlCommand(selectQuery, connection);
                selectCommand.Parameters.AddWithValue("@UserID", userID);
                selectCommand.Parameters.AddWithValue("@ChannelID", channelID);

                int existingEntryCount = (int)selectCommand.ExecuteScalar();

                if (existingEntryCount > 0) {
                    // Entry exists, perform an update
                    string updateQuery = "UPDATE UserChannelRoles SET role_id = @PermissionLevel WHERE user_id = @UserID AND channel_id = @ChannelID";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@UserID", userID);
                    updateCommand.Parameters.AddWithValue("@ChannelID", channelID);
                    updateCommand.Parameters.AddWithValue("@PermissionLevel", permissionLevel);

                    ExecuteNonQuery(connection, updateCommand);
                } else {
                    // Entry doesn't exist, perform an insert
                    string insertQuery = "INSERT INTO UserChannelRoles (user_id, channel_id, role_id) VALUES (@UserID, @ChannelID, @PermissionLevel)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@UserID", userID);
                    insertCommand.Parameters.AddWithValue("@ChannelID", channelID);
                    insertCommand.Parameters.AddWithValue("@PermissionLevel", permissionLevel);

                    ExecuteNonQuery(connection, insertCommand);
                }
            });
        }

        public static int GetUserRole(string userID, string channelID) {
            List<string> result = new List<string>();
            ExecuteDatabaseOperations(connection => {
                string selectQuery = "SELECT role_id FROM UserChannelRoles WHERE user_id = @UserID AND channel_id = @ChannelID";

                SqlCommand command = new SqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@UserID", userID);
                command.Parameters.AddWithValue("@ChannelID", channelID);

                result = ExecuteQuery<string>(connection, command);
            });

            if (result.Count > 0) {
                return Convert.ToInt32(result.First());
            } else {
                return PermissionLevel.ReadWrite;
            }

        }


    }
}
