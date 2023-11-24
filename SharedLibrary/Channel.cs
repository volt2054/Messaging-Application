using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary {
    public class Channel {
        private string channelName;
        private string channelID;

        public string ChannelName { get { return channelName; } }
        public string ChannelID { get { return channelID; } }

        public Channel(string ChannelName, string ChannelID)
        {
            this.channelName = ChannelName;
            this.channelID = ChannelID;
        }


    }
}
