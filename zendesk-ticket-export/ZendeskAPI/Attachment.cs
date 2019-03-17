using System;

namespace zendesk_ticket_export.ZendeskAPI
{
    public class Attachment
    {
        public long? id { get; set; }
        public String file_name { get; set; }
        public String content_url { get; set; }
        public String content_type { get; set; }
        public long? size { get; set; }
        public Boolean inline { get; set; }
        public long? TicketID { get; set; }
        public int? CommentNumber { get; set; }
    }
}