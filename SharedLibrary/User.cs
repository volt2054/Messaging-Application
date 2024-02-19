using System.Reflection.Metadata;

namespace SharedLibrary {
    public class User {
        private string _id;
        private string _username;

        public string ID { get { return _id; } }
        public string username { get { return _username; } }

        public User(string id, string username) {
            _id = id;
            _username = username;
        }
        static public List<User> StringListToUserList(List<string[]> list) {
            List<User> users = new List<User>();
            foreach (string[] User in list) {
                users.Add(new User(User[0], User[1]));
            }
            return users;
        }

    }
}
