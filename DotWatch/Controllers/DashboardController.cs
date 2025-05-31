using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace DotWatch.Controllers
{
    public class DashboardController : Controller
    {
        // Define a custom counter metric
        private static readonly Counter LoginCounter =
            Metrics.CreateCounter("user_login_total", "Number of user login attempts");

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("login")]
        public IActionResult Login()
        {
            LoginCounter.Inc(); // Increase login counter
            return Ok("Login Tracked!");
        }
    }
}
