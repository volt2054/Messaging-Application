using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public User(string[] stringArray) {
            _id = stringArray[0];
            _username = stringArray[1];
        }

    }
}
