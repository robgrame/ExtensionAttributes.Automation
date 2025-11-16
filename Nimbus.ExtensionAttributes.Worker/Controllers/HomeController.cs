using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nimbus.ExtensionAttributes.WorkerSvc.Config;
using System.Diagnostics;

namespace Nimbus.ExtensionAttributes.WorkerSvc.Controllers
{
    /// <summary>
    /// MVC Controller for main web interface pages
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _appSettings;

        public HomeController(ILogger<HomeController> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// Splash/Landing page
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Login page
        /// </summary>
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Real-time changes dashboard with SignalR
        /// </summary>
        public IActionResult Dashboard()
        {
            return View();
        }

        /// <summary>
        /// Configuration summary panel
        /// </summary>
        public IActionResult Configuration()
        {
            ViewBag.AppSettings = _appSettings;
            return View();
        }

        /// <summary>
        /// About page
        /// </summary>
        public IActionResult About()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.3.0";
            ViewBag.Version = version;
            ViewBag.MachineName = Environment.MachineName;
            ViewBag.OsVersion = Environment.OSVersion.ToString();
            ViewBag.Framework = Environment.Version.ToString();
            ViewBag.StartTime = Process.GetCurrentProcess().StartTime;
            return View();
        }

        /// <summary>
        /// Change detail page
        /// </summary>
        public IActionResult ChangeDetail(string id)
        {
            ViewBag.EventId = id;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
