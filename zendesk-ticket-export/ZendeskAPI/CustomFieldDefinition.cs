using System.Collections.Generic;

namespace zendesk_ticket_export.ZendeskAPI
{
    public class CustomFieldDefinition
    {
        public long id { get; set; }
        public string title { get; set; }
    }

    public class CustomFieldDefinitionJSONOutput
    {
        public List<CustomFieldDefinition> ticket_fields { get; set; }
    }
}