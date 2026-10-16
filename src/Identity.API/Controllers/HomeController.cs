using IdentityServerHost.Quickstart.UI;

namespace eShop.Identity.API.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
