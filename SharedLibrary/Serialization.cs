using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace SharedLibrary {
    public class Serialization {
        public static byte[] SerializeList<T>(List<T> list) {
            if (list.Count == 0) {
                return new byte[0];
            }

            using (MemoryStream memoryStream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(memoryStream)) {
                    writer.Write(list.Count);

                    foreach (T item in list) {
                        if (typeof(T) == typeof(string)) {
                            writer.Write((string)(object)item);
                        } else if (typeof(T) == typeof(string[])) {
                            string[] arrayItem = (string[])(object)item;
                            writer.Write(arrayItem.Length);
                            foreach (string str in arrayItem) {
                                writer.Write(str);
                            }
                        } else if (typeof(T) == typeof(User)) {
                            User user = (User)(object)item;
                            writer.Write(user.Username);
                            writer.Write(user.UserID);
                        } else if (typeof(T) == typeof(Channel)) {
                            Channel channel = (Channel)(object)item;
                            writer.Write(channel.ChannelName);
                            writer.Write(channel.ChannelID);
                        } else if (typeof(T) == typeof(Server)) {
                            Server server = (Server)(object)item;
                            writer.Write(server.ServerName);
                            writer.Write(server.ServerID);
                        } else if (typeof(T) == typeof(Message)) {
                            Message message = (Message)(object)item;
                            writer.Write(message.MessageContent);
                            writer.Write(message.MessageOwner.ToString());
                        }
                    }
                }
                return memoryStream.ToArray();
            }
        }


        public static List<T> DeserializeList<T>(byte[] data) {
            if (data.Length == 0) {
                return new List<T>();
            }

            List<T> list = new List<T>();
            using (MemoryStream memoryStream = new MemoryStream(data)) {
                using (BinaryReader reader = new BinaryReader(memoryStream)) {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++) {
                        if (typeof(T) == typeof(string)) {
                            string item = reader.ReadString();
                            list.Add((T)(object)item);
                        } else if (typeof(T) == typeof(string[])) {
                            int arrayLength = reader.ReadInt32();
                            string[] arrayItem = new string[arrayLength];
                            for (int j = 0; j < arrayLength; j++) {
                                arrayItem[j] = reader.ReadString();
                            }
                            list.Add((T)(object)arrayItem);
                        } else if (typeof(T) == typeof(User)) {
                            string username = reader.ReadString();
                            string userID = reader.ReadString();
                            User user = new User(username, userID);
                            list.Add((T)(object)user);
                        } else if (typeof(T) == typeof(Channel)) {
                            string channelName = reader.ReadString();
                            string channelID = reader.ReadString();
                            Channel channel = new Channel(channelName, channelID);
                            list.Add((T)(object)channel);
                        } else if (typeof(T) == typeof(Server)) {
                            string serverName = reader.ReadString();
                            string serverID = reader.ReadString();
                            Server server = new Server(serverName, serverID);
                            list.Add((T)(object)server);
                        } else if (typeof(T) == typeof(Message)) {
                            string messageContent = reader.ReadString();
                            User messageOwner = new User(reader.ReadString());
                            
                            Message message = new Message(messageContent, messageOwner);
                            list.Add((T)(object)message);
                        }
                    }
                }
            }
            return list;
        }

    }
}
