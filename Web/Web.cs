using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

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

        #region Static Methods
        private static IWebHostBuilder CreateWebHostBuilder(StatelessServiceContext serviceContext, string url, AspNetCoreCommunicationListener listener)
            => new WebHostBuilder()
                .ConfigureServices(
                    services => services
                        .AddSingleton(serviceContext))
                .UseContentRoot(Directory.GetCurrentDirectory())
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
        #endregion

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            var config = new ConfigHelper(this.Context);

            const string httpEndpointName = "ServiceEndpoint";
            const string httpsEndpointName = "ServiceHttpsEndpoint";
            var protocol = EndpointProtocols.Both;

            if (Enum.TryParse(typeof(EndpointProtocols), config.GetValue("Default", "EndpointProtocol"), true,
                out var obj))
            {
                protocol = (EndpointProtocols)obj;
            }

            if (protocol.HasFlag(EndpointProtocols.Http))
            {
                yield return new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, httpEndpointName, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return CreateWebHostBuilder(serviceContext, url, listener)
                            .UseKestrel()
                            .Build();

                    }), httpEndpointName);
            }

            if (protocol.HasFlag(EndpointProtocols.Https))
            {
                //Use either one of ServiceInstanceListener below for HTTPS

                //Https endpoint using UseKestrel
                //yield return new ServiceInstanceListener(serviceContext =>
                //    new KestrelCommunicationListener(serviceContext, httpsEndpointName, (url, listener) =>
                //    {
                //        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                //        return CreateWebHostBuilder(serviceContext, url, listener)
                //            .UseKestrel(opt =>
                //            {
                //                var port = serviceContext.CodePackageActivationContext
                //                    .GetEndpoint("ServiceHttpsEndpoint").Port;
                //                opt.Listen(IPAddress.IPv6Any, port, listenOptions =>
                //                {
                //                    listenOptions.UseHttps(GetCertificateFromStore("localhost"));
                //                    listenOptions.NoDelay = true;
                //                });
                //            })
                //            .Build();

                //    }), httpsEndpointName);

                //Https endpoint using HttpSys
                yield return new ServiceInstanceListener(serviceContext =>
                    new HttpSysCommunicationListener(serviceContext, httpsEndpointName, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return CreateWebHostBuilder(serviceContext, url, listener)
                            .UseHttpSys()
                            .Build();

                    }), httpsEndpointName);
            }
        }
    }
}
