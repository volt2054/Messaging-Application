using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary {
    public class WebSocketMetadata {

        public const string DELIMITER = "|< delimiter >|"; //TODO replace with something else
        public const int PORT = 7256;
        public const string IP_ADDRESS = "100.113.247.67";
        public static readonly string SERVER_URL = $"ws://{IP_ADDRESS}:{PORT}";

    }
}
