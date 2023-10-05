
namespace SharedLibrary {
        public class TypeOfCommunication {
            public static readonly string SendMessage = "SEND"; // (SEND + MESSAGE CONTENT + CHANNEL ID) RETURNS WHETHER SUCCESSFUL
            public static readonly string FetchMessages = "FETCHMESSAGES"; // (FETCHMESSAGES + CHANNEL ID + MESSAGE ID) RETURNS RECENTLY SENT MESSAGES
            public static readonly string GetID = "GETUSERID"; // (GETUSERID + USERNAME)  RETURNS ID GIVEN USERNAME
            public static readonly string RegisterUser = "CREATE"; // (CREATE + USERNAME + EMAIL + PASSWORD) RETURNS WHETHER SUCCESSFUL
            public static readonly string ValidateUser = "CHECK"; // (CHECK + USERNAME + PASSWORD) RETURNS USER ID ON SUCCESS OR "BAD PASSWORD" ON FAIL
            public static readonly string FetchChannels = "FETCHCHANNELS"; // (FETCHCHANNELS + SERVERID) RETURNS LIST OF CHANNELS
            public static readonly string CreateDMChannel = "CREATEDMCHANNEL"; // (CREATEDMCAHNNEL + USERID + USERID) RETURNS CHANNEL ID
            public static readonly string AddFriend = "ADDFRIEND"; // (ADDFRIEND + FRIENDID)
            public static readonly string RemoveFriend = "REMFRIEND"; // (REMFRIEND + FRIENDID)
            public static readonly string GetFriends = "GETFRIEND"; // (GETFRIEND + USERID)

            public static readonly string NotifyMessage = "MESSAGE:"; // (MESSAGE: + CHANNELID + USERNAME + MESSAGECONTENT)

        }
    
}