using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary {
    public class Server {
        private string serverName;
        private string serverID;

        public string ServerName { get { return serverName;  } }
        public string ServerID { get { return serverID; } }

        public Server(string ServerName, string ServerID) {
            this.serverName = ServerName;
            this.serverID = ServerID;
        }
    }
}
