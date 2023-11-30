using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary {
    public class Message {
        private string messageContent;
        private User messageOwner;

        public string MessageContent { get { return messageContent; } }
        public User MessageOwner { get { return messageOwner; } }

        public Message(string content, User owner) {
            this.messageContent = content;
            this.messageOwner = owner;
        }
    }
}
