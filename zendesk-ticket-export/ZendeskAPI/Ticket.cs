using System;
using System.Collections.Generic;

namespace zendesk_ticket_export.ZendeskAPI
{
    public class Ticket
    {
        public long? id { get; set; }
        public String url { get; set; }
        public String external_id { get; set; }
        public String type { get; set; }
        public String subject { get; set; }
        public String raw_subject { get; set; }
        public String description { get; set; }
        public String priority { get; set; }
        public String status { get; set; }
        public String recipient { get; set; }
        public long? requester_id { get; set; }
        public long? submitter_id { get; set; }
        public long? assignee_id { get; set; }
        public long? organization_id { get; set; }
        public long? group_id { get; set; }
        public List<String> collaborator_ids { get; set; }
        public long? forum_topic_id { get; set; }
        public long? problem_id { get; set; }
        public Boolean has_incidents { get; set; }
        public DateTime? due_at { get; set; }
        public List<String> tags { get; set; }
        public Object via { get; set; }
        public List<CustomField> custom_fields { get; set; }
        public long? brand_id { get; set; }
        public List<String> followup_ids { get; set; }
        public Boolean allow_channelback { get; set; }
        public Boolean is_public { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
    public class CustomField
    {
        public long? id { get; set; }
        public String value { get; set; }
    }
    public class TicketComment
    {
        public long? id { get; set; }
        public String type { get; set; }
        public String body { get; set; }
        public String html_body { get; set; }
        public String plain_body { get; set; }
        public Boolean @public { get; set; }
        public long? author_id { get; set; }
        public List<Attachment> attachments { get; set; }
        public Object via { get; set; }
        public DateTime? created_at { get; set; }
        public int? CommentNumber { get; set; }
    }
    public class CommentsJSONResponse
    {
        public List<TicketComment> comments { get; set; }
        public string next_page { get; set; }
    }
    public class TicketsJSONResponse
    {
        public List<Ticket> tickets { get; set; }
        public int? count { get; set; }
        public string next_page { get; set; }
        public string end_time { get; set; }
        public TicketsJSONResponse()
        {
            tickets = new List<Ticket>();
        }
    }
}