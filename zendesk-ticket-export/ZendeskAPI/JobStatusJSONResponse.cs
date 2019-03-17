using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zendesk_ticket_export.ZendeskAPI
{
    public class JobStatus
    {
        public String id { get; set; }
        public String url { get; set; }
        public long? total { get; set; }
        public long? progress { get; set; }
        public String status { get; set; }
        public String message { get; set; }
        public List<JobStatusResult> results { get; set; }
    }

    public class JobStatusResult
    {
        public long? id { get; set; }
        public String title { get; set; }
        public String action { get; set; }
        public String errors { get; set; }
        public Boolean success { get; set; }
        public String status { get; set; }
    }

    public class JobStatusJSONResponse
    {
        public JobStatus job_status { get; set; }
    }

    public class JobStatusesJSONResponse
    {
        public List<JobStatus> job_statuses { get; set; }
    }
}