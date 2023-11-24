
namespace SharedLibrary {
        public class TypeOfCommunication {
            public const string SendMessage = "SEND"; // (SEND + MESSAGE CONTENT + CHANNEL ID) RETURNS WHETHER SUCCESSFUL
            public const string FetchMessages = "FETCHMESSAGES"; // (FETCHMESSAGES + CHANNEL ID + MESSAGE ID) RETURNS RECENTLY SENT MESSAGES
            public const string GetID = "GETUSERID"; // (GETUSERID + USERNAME)  RETURNS ID GIVEN USERNAME
            public const string RegisterUser = "CREATE"; // (CREATE + USERNAME + EMAIL + PASSWORD) RETURNS WHETHER SUCCESSFUL
            public const string ValidateUser = "CHECK"; // (CHECK + USERNAME + PASSWORD) RETURNS USER ID ON SUCCESS OR "BAD PASSWORD" ON FAIL
            public const string FetchChannels = "FETCHCHANNELS"; // (FETCHCHANNELS + SERVERID) RETURNS LIST OF CHANNELS
            public const string CreateDMChannel = "CREATEDMCHANNEL"; // (CREATEDMCHANNEL + USERID) RETURNS CHANNEL ID
            public const string CreateGroupChannel = "CREATEGROUPDCHANNEL"; // (CREATEGROUPCHANNEL + USERID) RETURNS CHANNEL ID
            public const string CreateServer = "CREATESERVER"; // (CREATESERVER + SERVER NAME + SERVER DESC + CHANNEL LIST) RETURNS WHETHER SUCCESSFUL
            public const string CreateChannel = "CREATECHANNEL"; // (CREATECHANNEL + CHANNEL NAME + SERVER ID) RETURNS CHANNELID
            public const string FetchServers = "FETCHSERVERS"; // (FETCHSERVERS) RETURNS LIST OF SERVERS
            public const string AddFriend = "ADDFRIEND"; // (ADDFRIEND + FRIENDID)
            public const string RemoveFriend = "REMFRIEND"; // (REMFRIEND + FRIENDID)
            public const string GetFriends = "GETFRIEND"; // (GETFRIEND + USERID)

            public const string NotifyMessage = "MESSAGE:"; // (MESSAGE: + CHANNELID + USERNAME + MESSAGECONTENT)
            public const string NotifyChannel = "CHANNEL:"; // (CHANNEL: + CHANNELID + CHANNELNAME)
            public const string NotifyServer = "SERVER:"; // (CHANNEL: + SERVERID + SERVERNAME)
        }
}