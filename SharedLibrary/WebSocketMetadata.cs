using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary {
    public class WebSocketMetadata {

        public const string DELIMITER = "|< delimiter >|"; //TODO replace with something else
        const int PORT = 7256;
        const string IP_ADDRESS = "127.0.0.1";
        public static readonly string SERVER_URL = $"ws://{IP_ADDRESS}:{PORT}";

    }
}
