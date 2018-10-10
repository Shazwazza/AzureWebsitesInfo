using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureLBInfo.Web.Jobs
{
    class Program
    {
        static void Main(string[] args)
        {
            string instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
            string serverName = Environment.MachineName;
            var baseDir = Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), "site", "wwwroot");

            while (true)
            {
                Console.WriteLine($"{instanceId} - {serverName} - {baseDir}");
                try
                {
                    UpdateFile(baseDir, instanceId, serverName);
                    Console.WriteLine("File updated");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex}");
                }
                Thread.Sleep(30000); //30 seconds
            }
        }

        private static void UpdateFile(string currDir, string instanceId, string serverName)
        {
            List<WebsiteInstance> items;
            var serializer = new JsonSerializer();
            var filePath = Path.Combine(currDir, "App_Data", "instances.json");

            using (var fs = new FileStream(filePath, FileMode.OpenOrCreate))
            using (var sr = new StreamReader(fs))
            using (var jr = new JsonTextReader(sr))
            {
                items = serializer.Deserialize<List<WebsiteInstance>>(jr);
            }

            if (items == null)
            {
                items = new List<WebsiteInstance>
                    {
                        new WebsiteInstance
                        {
                            InstanceId = instanceId,
                            ServerName = serverName,
                            LastPing = DateTime.UtcNow
                        }
                    };
            }
            else
            {
                var found = items.FirstOrDefault(x => x.InstanceId == instanceId);
                if (found != null)
                {
                    found.LastPing = DateTime.UtcNow;
                    found.ServerName = serverName;
                }
                else
                {
                    items.Add(new WebsiteInstance
                    {
                        InstanceId = instanceId,
                        ServerName = serverName,
                        LastPing = DateTime.UtcNow
                    });
                }
            }

            using (var fs = File.OpenWrite(filePath))
            using (var sw = new StreamWriter(fs))
            using (var jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, items);
            }
        } 

        private class WebsiteInstance
        {
            public string InstanceId { get; set; }
            public string ServerName { get; set; }
            public DateTime LastPing { get; set; }
        }
    }
}
