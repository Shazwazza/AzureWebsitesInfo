namespace AzureLBInfo.Web.Models
{
    public class RemoteInfo
    {
        public string FromInstanceId { get; set; }
        public string ServerName { get; set; }
        public bool IsHealthy { get; set; }
        public string ToInstanceId { get; set; }
    }
}