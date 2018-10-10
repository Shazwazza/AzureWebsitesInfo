using System;

namespace AzureLBInfo.Web.Models
{
    public class WebJobInfo
    {
        public string InstanceId { get; set; }
        public string ServerName { get; set; }
        public DateTime LastPing { get; set; }
    }
}