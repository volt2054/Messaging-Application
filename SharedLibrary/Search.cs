using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary {
    public class Search {
        public class SearchParameters {
            public string? Username { get; set; }
            public bool IsUsernameNull { get; set; }
            public string? MessageType { get; set; }
            public bool IsMessageTypeNull { get; set; }
            public DateTime? StartDate { get; set; }
            public bool IsStartDateNull { get; set; }
            public DateTime? EndDate { get; set; }
            public bool IsEndDateNull { get; set; }
            public string? SearchText { get; set; }
            public bool IsSearchTextNull { get; set; }
        }

        public class MessageSearchResult {
            public int MessageId { get; set; }
            public string MessageContent { get; set; }
            public int ChannelId { get; set; }
            public int UserId { get; set; }
            public DateTime DateSent { get; set; }
            public int MessageType { get; set; }
        }
    }
}
