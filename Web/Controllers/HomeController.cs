using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            const string indexFile = "~/index.html";
            const string type = "text/html";

            //Enable Reserved Proxy - handle transform
            if (this.Request.PathBase == Startup.ReservedProxyUrl)
            {
                //The result can be cache for subsequence use.
                var str = System.IO.File.ReadAllText(indexFile.Replace("~/","wwwroot/"));
                //src
                str = str.Replace("src=\"/", $"src =\"{Startup.ReservedProxyUrl}/")
                    .Replace("src='/", $"src ='{Startup.ReservedProxyUrl}/")
                    //href
                    .Replace("href=\"/", $"href=\"{Startup.ReservedProxyUrl}")
                    .Replace("href='/", $"href='{Startup.ReservedProxyUrl}");

                return File(Encoding.UTF8.GetBytes(str), type);
            }

            return File(indexFile, type);
        }
    }
}
