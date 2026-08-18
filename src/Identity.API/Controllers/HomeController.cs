namespace eShop.Identity.API.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
