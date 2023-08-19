
namespace SharedLibrary {
        public class TypeOfCommunication {
            public static readonly string SendMessage = "SEND"; // (SEND + MESSAGE CONTENT + CHANNEL ID + USER ID) RETURNS WHETHER SUCCESSFUL
            public static readonly string GetMessages = "GET"; // (GET + CHANNEL ID + MESSAGE ID) RETURNS RECENTLY SENT MESSAGES
            public static readonly string GetID = "GETUSERID"; // (GETUSERID + USERNAME)  RETURNS ID GIVEN USERNAME
            public static readonly string RegisterUser = "CREATE"; // (CREATE + USERNAME + EMAIL + PASSWORD) RETURNS WHETHER SUCCESSFUL
            public static readonly string ValidateUser = "CHECK"; // (CHECK + USERNAME + PASSWORD) RETURNS WHETHER SUCCESSFUL
            public static readonly string DeleteUser = "DELETEUSER"; // (DELETE + USERID) RETURNS WHETHER SUCCESSFUL
            public static readonly string FetchChannels = "FETCHCHANNELS"; // (FETCHCHANNELS + USERID) RETURNS LIST OF CHANNELS
            public static readonly string CreateDMChannel = "CREATEDMCHANNEL"; // (CREATEDMCAHNNEL + USERID + USERID) RETURNS CHANNEL ID
        }
    
}