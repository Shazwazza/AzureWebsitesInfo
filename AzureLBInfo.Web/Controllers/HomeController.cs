using AzureLBInfo.Web.Models;
using Microsoft.WindowsAzure.Management.WebSites;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AzureLBInfo.Web.Views
{
    public class HomeController : Controller
    {
        private readonly WebSiteManagementClient client;
        
#if DEBUG
        private static HomeModel _homeModel;
#endif

        public HomeController(WebSiteManagementClient client)
        {
            this.client = client;
        }

        public async Task<ActionResult> Index()
        {
            var webspaceName = $"CloudClub-{WebSpaceNames.WestEuropeWebSpace}";
            var websiteName = "CloudClub";
            var webJobInfo = GetWebJobInfoWithRetry();
            var instanceIds = (await client.WebSites.GetInstanceIdsAsync(webspaceName, websiteName)).ToList();

            IEnumerable<RemoteInfo> remoteInfo = new List<RemoteInfo>();
            var localInfo = new LocalInfo
            {
                WebsiteInstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"),
                IpAddress = Request.UserHostAddress
            };
#if DEBUG
            if (_homeModel != null) return View(_homeModel);
#else

            if (Request.QueryString["remote"] != null)
            {
                remoteInfo = await GetRemoteInfo(
                    instanceIds.Except(new[] { localInfo.WebsiteInstanceId }, StringComparer.InvariantCultureIgnoreCase),
                    $"{Request.Url.Host}");
            }
            
#endif      
            
            var model = new HomeModel
                {
                WebJobInfo = webJobInfo,
                LocalInfo = localInfo,
                WebsiteInfo = (await client.WebSites.GetAsync(webspaceName, websiteName, null)),
                SiteIds = instanceIds,
                CodeGen = HttpRuntime.CodegenDir,
                FastDrive = Environment.ExpandEnvironmentVariables("%temp%"),
                SlowDrive = Server.MapPath("~"),
                RemoteInfo = remoteInfo
            };

#if DEBUG
            _homeModel = model;
#endif

            return View(model);
        }

        [HttpGet]
        public ActionResult Info()
        {
            var instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
            var serverName = Environment.MachineName;
            return Json(new RemoteInfo
            {
                FromInstanceId = instanceId,
                ServerName = serverName,
                IsHealthy = true
            }, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public ActionResult ChangeWorkers(string siteId)
        {
            var arrCookie = Request.Cookies.Get("ARRAffinity");
            if (arrCookie == null) throw new InvalidOperationException("No ARRAffinity cookie found");
            Response.Cookies.Remove("ARRAffinity");
            arrCookie.Value = siteId;
            arrCookie.Domain = $".{Request.Url.Host}";
            Response.SetCookie(arrCookie);
            return RedirectToAction("Index");
        }

        private IEnumerable<WebJobInfo> GetWebJobInfoWithRetry()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return GetWebJobInfo();
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            }

            return Enumerable.Empty<WebJobInfo>();
        }

        private async Task<IEnumerable<RemoteInfo>> GetRemoteInfo(IEnumerable<string> instanceIds, string host)
        {
            var result = new List<RemoteInfo>();
            foreach (var s in instanceIds)
            {
                string requestUri = $"https://{host}/home/info";

                var response = await GetFromInstance(new Uri(requestUri), s);
                var str = await response.Content.ReadAsStringAsync();
                var info = JsonConvert.DeserializeObject<RemoteInfo>(str);
                info.ToInstanceId = s;
                result.Add(info);
            }
            return result;
        }

        private async Task<HttpResponseMessage> GetFromInstance(Uri url, string instanceId)
        {
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                using (var httpClient = new HttpClient(handler))
                {
                    cookieContainer.Add(url, new Cookie("ARRAffinity", instanceId));
                    return await httpClient.GetAsync(url);
                }
            }
        }

        private IEnumerable<WebJobInfo> GetWebJobInfo()
        {
            var filePath = Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), "site", "wwwroot", "App_Data", "instances.json");

            if (!System.IO.File.Exists(filePath)) return Enumerable.Empty<WebJobInfo>();

            using (var fs = System.IO.File.OpenRead(filePath))
            using (var sr = new StreamReader(fs))
            using (var jr = new JsonTextReader(sr))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<WebJobInfo[]>(jr);
            }
        }
    }
}