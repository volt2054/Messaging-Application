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

        public override string ToString() {
            string UserToString = _id + ":" + _username;
            return UserToString;
        }
    }
}
