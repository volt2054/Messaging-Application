﻿
namespace SharedLibrary {
    public class TypeOfCommunication {
        public const string SendMessage = "SEND"; // (SEND + MESSAGE CONTENT + CHANNEL ID) RETURNS WHETHER SUCCESSFUL
        public const string SendAttachment = "SENDATTACHMENT"; // (SEND + FILE ID + CHANNEL ID) RETURNS WHETHER SUCCESSFUL
        public const string FetchMessages = "FETCHMESSAGES"; // (FETCHMESSAGES + CHANNEL ID + MESSAGE ID) RETURNS RECENTLY SENT MESSAGES
        public const string GetID = "GETUSERID"; // (GETUSERID + USERNAME)  RETURNS ID GIVEN USERNAME
        public const string RegisterUser = "CREATE"; // (CREATE + USERNAME + PASSWORD) RETURNS WHETHER SUCCESSFUL
        public const string ValidateUser = "CHECK"; // (CHECK + USERNAME + PASSWORD) RETURNS USER ID ON SUCCESS OR "BAD PASSWORD" ON FAIL
        public const string FetchChannels = "FETCHCHANNELS"; // (FETCHCHANNELS + SERVERID) RETURNS LIST OF CHANNELS
        public const string CreateDMChannel = "CREATEDMCHANNEL"; // (CREATEDMCHANNEL + USERID) RETURNS CHANNEL ID
        public const string CreateGroupChannel = "CREATEGROUPCHANNEL"; // (CREATEGROUPCHANNEL + USERID) RETURNS CHANNEL ID
        public const string CreateServer = "CREATESERVER"; // (CREATESERVER + SERVER NAME + SERVER DESC + CHANNEL LIST) RETURNS WHETHER SUCCESSFUL
        public const string CreateChannel = "CREATECHANNEL"; // (CREATECHANNEL + CHANNEL NAME + SERVER ID) RETURNS CHANNELID
        public const string FetchServers = "FETCHSERVERS"; // (FETCHSERVERS) RETURNS LIST OF SERVERS
        public const string AddFriend = "ADDFRIEND"; // (ADDFRIEND + FRIENDID)
        public const string RemoveFriend = "REMFRIEND"; // (REMFRIEND + FRIENDID)
        public const string GetFriends = "GETFRIENDS"; // (GETFRIEND + USERID)
        public const string GetRequests = "GETREQUESTS"; // (GETREQUESTS + USERID)
        public const string AcceptRequest = "ACCEPTREQUEST"; // (ACCEPTREQUEST + USERTOACCEPT + USERID)
        public const string RejectRequest = "REJECTREQUEST"; // (REJECTREQUEST + USERTOREJECT + USERID)
        public const string GetUsersInServer = "GETUSERSINSERVER"; // (GETUSERSINSERVER + SERVERID)
        public const string GetUsername = "GETUSERNAME"; // (GETUSERNAME + ID) gets username given user id
        public const string GetProfilePicture = "GETPFP"; // (GETPFP + ID) gets link to user profile pic
        public const string SetProfilePicture = "SETPFP"; // (SETPFP + FILE NAME) // sets pfp of user id to file name
        public const string ChangeRole = "CHANGEROLE"; // (CHANGEROLE + USERID + ROLE NUMBER + SERVERID) // Change role to value given
        public const string CheckRole = "CHECKROLE"; // (CHECKROLE + USERID + CHANNELID) // Check Role
        public const string ChangeUsername = "CHANGEUSERNAME"; // CHANGEUSERNAME + NEWUSERNAME // Changes Username
        public const string ChangePassword = "CHANGEPASSWORD"; // CHANGEPASSWORD + NEWPASSWORD // Changes Password
        public const string AddToServer = "ADDTOSERVER"; // ADDTOSERVER + SERVERID + USERID
        public const string RemoveFromServer = "REMOVEFROMSERVER"; // REMOVEFROMSERVER + SERVERID + USERID
        public const string SearchMessages = "SEARCH"; // SEARCH + SearchParamatersObject


        public const string NotifyMessage = "MESSAGE:"; // (MESSAGE: + CHANNELID + USERNAME + MESSAGECONTENT)
        public const string NotifyAttachment = "ATTACHMENT:"; // (ATTACHMENT: + CHANNELID + USERNAME + FILEID)
        public const string NotifyChannel = "CHANNEL:"; // (CHANNEL: + CHANNELID + CHANNELNAME)
        public const string NotifyServer = "SERVER:"; // (CHANNEL: + SERVERID + SERVERNAME)
    }
}