using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMemoryCache _cache;

        public HomeController(IMemoryCache cache) => _cache = cache;

        public async ValueTask<IActionResult> Index()
        {
            const string indexFile = "~/index.html";
            const string type = "text/html";

            if (this.Request.PathBase != Startup.ReservedProxyUrl) return File(indexFile, type);

            var strValue = await _cache.GetOrCreateAsync(indexFile, async entry =>
            {
                //The result can be cache for subsequence use.
                var str = await System.IO.File.ReadAllTextAsync(indexFile.Replace("~/", "wwwroot/"));
                //src
                return str.Replace("src=\"/", $"src =\"{Startup.ReservedProxyUrl}/")
                    .Replace("src='/", $"src ='{Startup.ReservedProxyUrl}/")
                    //href
                    .Replace("href=\"/", $"href=\"{Startup.ReservedProxyUrl}/")
                    .Replace("href='/", $"href='{Startup.ReservedProxyUrl}/");
            });

            return File(Encoding.UTF8.GetBytes(strValue), type);
        }
    }
}
