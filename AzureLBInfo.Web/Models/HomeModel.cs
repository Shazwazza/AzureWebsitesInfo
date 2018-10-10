using Microsoft.WindowsAzure.Management.WebSites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureLBInfo.Web.Models
{

    public class HomeModel
    {
        public IEnumerable<WebJobInfo> WebJobInfo { get; set; }
        public LocalInfo LocalInfo { get; set; }
        public WebSiteGetResponse WebsiteInfo { get; set; }
        public IEnumerable<string> SiteIds { get; set; }
        public string SlowDrive { get; set; }
        public string FastDrive { get; set; }
        public string CodeGen { get; set; }
        public IEnumerable<RemoteInfo> RemoteInfo { get; set; }
    }
}