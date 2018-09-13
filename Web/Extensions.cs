using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Web
{
    public static class Extensions
    {
        public static IApplicationBuilder UseBodyTransformer(this IApplicationBuilder app, string forContentType, Func<string, string> transformer, Func<HttpContext, bool> when = null)
        {
            if (forContentType == null) throw new ArgumentNullException(nameof(forContentType));
            if (transformer == null) throw new ArgumentNullException(nameof(transformer));

            return app.Use(async (context, next) =>
            {
                if (when != null && !when(context))
                {
                    await next();
                    return;
                }

                var existingBody = context.Response.Body;

                using (var newBody = new MemoryStream())
                {
                    context.Response.Body = newBody;

                    await next();

                    newBody.Seek(0, SeekOrigin.Begin);
                    var body = new StreamReader(newBody).ReadToEnd();

                    if (context.Response.ContentType == forContentType)
                        body = transformer(body);

                    context.Response.Body = existingBody;
                    await context.Response.WriteAsync(body);
                }
            });
        }
    }
}
