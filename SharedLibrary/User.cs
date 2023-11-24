using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary {
    public class User {
        private string username;
        private string userID;

        public string Username { get { return username; } }
        public string UserID { get { return userID; } }


        public User(string username, string userID) {
            this.username = username;
            this.userID = userID;
        }

        public User(string FromString) {
            string[] details = FromString.Split(':');
            this.userID = details[0];

            username = FromString.Substring(userID.Length);

        }

        public override string ToString() {
            string UserToString = userID + ":" + username;
            return UserToString;
        }
    }
}
