using IdentityServerHost.Quickstart.UI;

namespace eShop.Identity.API.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class HomeController(IWebHostEnvironment environment, ILogger<HomeController> logger) : Controller
    {
        public IActionResult Index()
        {
            if (environment.IsDevelopment())
            {
                // only show in development
                return View();
            }

            logger.LogInformation("Homepage is disabled in production. Returning 404.");
            return NotFound();
        }
    }
}
