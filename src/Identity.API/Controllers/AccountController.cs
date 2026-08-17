using Abblix.Oidc.Server.Features.RandomGenerators;
using Abblix.Oidc.Server.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Path = Abblix.Oidc.Server.Mvc.Path;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using UriBuilder = Abblix.Utils.UriBuilder;

namespace eShop.Identity.API.Controllers
{
    /// <summary>
    /// The sign-in screen the OIDC server hands unauthenticated users to.
    /// </summary>
    /// <remarks>
    /// The host's whole duty here is to establish a session: verify the credentials it owns, then
    /// tell the library who signed in. It never decides the outcome of the authorization request
    /// and never inspects it, which is why nothing in this controller parses a return URL.
    /// </remarks>
    [AllowAnonymous]
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuthSessionService authSessionService,
        ISessionIdGenerator sessionIdGenerator,
        IUriResolver uriResolver,
        TimeProvider clock) : Controller
    {
        [HttpGet]
        public IActionResult Login(
            [FromQuery(Name = AuthorizationRequest.Parameters.RequestUri)] string requestUri)
            => View(new LoginViewModel { RequestUri = requestUri });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByNameAsync(model.Username);

            // Checks the password and applies lockout without issuing an Identity cookie: the
            // session below is the only one, so Identity verifies and the library remembers.
            var result = user is not null
                ? await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true)
                : SignInResult.Failed;

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            var authSession = new AuthSession(
                user.Id,
                sessionIdGenerator.GenerateSessionId(),
                clock.GetUtcNow(),
                CookieAuthenticationDefaults.AuthenticationScheme)
            {
                Email = user.Email,
                EmailVerified = user.EmailConfirmed,
                AuthenticationMethodReferences = [AuthenticationMethodReferences.Password],
            };

            await authSessionService.SignInAsync(authSession);

            // Resume the interrupted authorization request by its reference.
            var authorizeUrl = new UriBuilder(uriResolver.Content(Path.Authorize))
            {
                Query = { [AuthorizationRequest.Parameters.RequestUri] = model.RequestUri },
            };

            return Redirect(authorizeUrl);
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
