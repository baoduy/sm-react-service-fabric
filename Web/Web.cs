using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Web
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class Web : StatelessService
    {
        public Web(StatelessServiceContext context)
            : base(context)
        { }

        private static IWebHostBuilder CreateWebHostBuilder(StatelessServiceContext serviceContext, string url, AspNetCoreCommunicationListener listener)
            => new WebHostBuilder()
            .ConfigureServices(
                services => services
                    .AddSingleton(serviceContext))
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseWebRoot("wwwroot")
            .UseStartup<Startup>()
            .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseReverseProxyIntegration)
            .UseUrls(url);

        private static X509Certificate2 GetCertificateFromStore(string subjectDistinguishedName)
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                var currentCerts = certCollection.Find(X509FindType.FindBySubjectDistinguishedName, $"CN={subjectDistinguishedName}", false);
                return currentCerts.Count == 0 ? null : currentCerts[0];
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                //Http endpoint
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return CreateWebHostBuilder(serviceContext,url,listener)
                                    .UseKestrel()
                                    .Build();

                    }),"ServiceEndpoint"),
                //Use either one of ServiceInstanceListener below for HTTPS
                //Https endpoint using UseKestrel
                //new ServiceInstanceListener(serviceContext =>
                //    new KestrelCommunicationListener(serviceContext, "ServiceHttpsEndpoint", (url, listener) =>
                //    {
                //        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                //        return CreateWebHostBuilder(serviceContext,url,listener)
                //            .UseKestrel(opt =>
                //            {
                //                var port = serviceContext.CodePackageActivationContext.GetEndpoint("ServiceHttpsEndpoint").Port;
                //                opt.Listen(IPAddress.IPv6Any, port, listenOptions =>
                //                {
                //                    listenOptions.UseHttps(GetCertificateFromStore("localhost"));
                //                    listenOptions.NoDelay = true;
                //                });
                //            })
                //            .Build();

                //    }),"ServiceHttpsEndpoint"),
                //Https endpoint using HttpSys
                new ServiceInstanceListener(serviceContext =>
                    new HttpSysCommunicationListener(serviceContext, "ServiceHttpsEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return CreateWebHostBuilder(serviceContext,url,listener)
                            .UseHttpSys()
                            .Build();

                    }),"ServiceHttpsEndpoint")
            };
        }
    }
}
