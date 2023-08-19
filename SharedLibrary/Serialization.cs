using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace SharedLibrary {
    public class Serialization {
        public static byte[] SerializeList(List<string> list) {
            using (MemoryStream memoryStream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(memoryStream)) {
                    writer.Write(list.Count);
                    foreach (string item in list) {
                        writer.Write(item);
                    }
                }
                return memoryStream.ToArray();
            }
        }

        public static List<string> DeserializeList(byte[] data) {
            List<string> list = new List<string>();
            using (MemoryStream memoryStream = new MemoryStream(data)) {
                using (BinaryReader reader = new BinaryReader(memoryStream)) {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++) {
                        string channel = reader.ReadString();
                        list.Add(channel);
                    }
                }
            }
            return list;
        }
    }
}
