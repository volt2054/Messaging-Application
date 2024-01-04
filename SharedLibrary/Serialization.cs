namespace SharedLibrary {
    public class Serialization {
        public static byte[] SerializeList<T>(List<T> list) {

            if (list.Count == 0) {
                return new byte[0];
            }

            using (MemoryStream memoryStream = new MemoryStream())
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
                        writer.Write(user.ID);
                        writer.Write(user.username);
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
            using (MemoryStream memoryStream = new MemoryStream(data))
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
                        string id = reader.ReadString();
                        string username = reader.ReadString();
                        list.Add((T)(object)new User(id, username));
                    }
                }
            }

            return list;
        }
    }
}
