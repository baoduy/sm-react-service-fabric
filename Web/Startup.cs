using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Web
{
    public class Startup
    {
        public const string ReservedProxyUrl = "/ReactJs/Web";

        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            //Enable Compress
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Optimal);
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;

                options.MimeTypes = new[]
                {
                    // Default
                    "text/plain",
                    "text/css",
                    "application/javascript",
                    "text/html",
                    "application/xml",
                    "text/xml",
                    "application/json",
                    "text/json",
                    // Custom
                    "image/svg+xml"
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            //Enable Compress
            app.UseResponseCompression();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller}/{action=Index}/{id?}");
            //});

            //Enable Reserved Proxy and handle transform
            app.Use(async (context, next) =>
            {
                if (!context.Request.Headers.TryGetValue("X-Forwarded-Host", out var url) ||
                    !url.ToString().Contains("19081"))
                {
                    await next();
                    return;
                }

                //Apply the Preserved Proxy if accessing by the Service Fabric Reserved Proxy
                context.Request.PathBase = ReservedProxyUrl;

                var existingBody = context.Response.Body;

                using (var newBody = new MemoryStream())
                {
                    context.Response.Body = newBody;

                    await next();

                    newBody.Seek(0, SeekOrigin.Begin);
                    var body = new StreamReader(newBody).ReadToEnd();
                    body = body.Replace("src=\"/", $"src =\"{ReservedProxyUrl}/")
                        .Replace("src='/", $"src ='{ReservedProxyUrl}/")
                        //href
                        .Replace("href=\"/", $"href=\"{ReservedProxyUrl}/")
                        .Replace("href='/", $"href='{ReservedProxyUrl}/");

                    context.Response.Body = existingBody;
                    await context.Response.WriteAsync(body);
                }
            });

            app.UseSpa(spa => { });
        }
    }
}
