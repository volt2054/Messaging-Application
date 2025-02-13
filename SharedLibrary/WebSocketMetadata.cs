﻿namespace SharedLibrary {
    public class WebSocketMetadata {
        public const string DELIMITER = "|< delimiter >|"; //TODO replace with something else
        public const string PORT = "7256";
        public const string IP_ADDRESS = "127.0.0.1";
        public const string SERVER_URL = $"ws://{IP_ADDRESS}:{PORT}";

        public const string CDPORT = "7257";
        public const string CDSERVER_URL = $"http://{IP_ADDRESS}:{CDPORT}/";
    }
}