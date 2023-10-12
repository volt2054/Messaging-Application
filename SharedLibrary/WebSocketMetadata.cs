﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary {
    public class WebSocketMetadata {

        public const string DELIMITER = "|< delimiter >|"; //TODO replace with something else
        public static readonly string PORT = "7256";
        public static readonly string IP_ADDRESS = "localhost";
        public static readonly string SERVER_URL = $"ws://{IP_ADDRESS}:{PORT}";

    }
}
