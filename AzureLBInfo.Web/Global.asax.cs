using LightInject;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Management.WebSites;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace AzureLBInfo.Web
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            var container = new ServiceContainer();
            container.RegisterControllers();

            container.Register<SubscriptionCloudCredentials>(x =>
            {
                string subscriptionId = ConfigurationManager.AppSettings["subscriptionId"];

#if !DEBUG
                using (var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    certStore.Open(OpenFlags.ReadOnly);
                    var certCollection = certStore.Certificates.Find(
                            X509FindType.FindByThumbprint,
                            ConfigurationManager.AppSettings["WEBSITE_LOAD_CERTIFICATES"],
                            false);
                    // Get the first cert with the thumbprint
                    if (certCollection.Count > 0)
                    {
                        var cert = certCollection[0];
                        return new CertificateCloudCredentials(subscriptionId, cert);
                    }

                    throw new InvalidOperationException("Could not find certificate");
                }
#else
                string certKey = ConfigurationManager.AppSettings["cert"];
                return new CertificateCloudCredentials(subscriptionId,
                    new X509Certificate2(
                        Convert.FromBase64String(certKey)));
#endif

            }, new PerContainerLifetime());

            container.Register<WebSiteManagementClient>(x =>
            {
                var client = new WebSiteManagementClient(x.GetInstance<SubscriptionCloudCredentials>());
                return client;
            }, new PerContainerLifetime());

            container.EnableMvc();
        }

    }
}