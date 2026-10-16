using Abblix.Oidc.Server.Features.RandomGenerators;
using Abblix.Oidc.Server.Model;
using Abblix.Oidc.Server.Common.Configuration;
using Abblix.Oidc.Server.Common.Interfaces;
using IdentityServerHost.Quickstart.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuthSessionService authSessionService,
        ISessionIdGenerator sessionIdGenerator,
        IOidcEndpointResolver endpointResolver,
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

            // Resume the interrupted authorization request by its reference. The library answers with the
            // path it mapped the endpoint on, so a moved path or a prefix needs no change here; it answers
            // nothing only for a disabled endpoint, and a server without its authorization endpoint has no
            // request to resume.
            var authorizationEndpoint = endpointResolver.Resolve(OidcEndpoints.Authorize)
                ?? throw new InvalidOperationException("The authorization endpoint is not mapped.");

            var authorizeUrl = new UriBuilder(authorizationEndpoint)
            {
                Query = { [AuthorizationRequest.Parameters.RequestUri] = model.RequestUri },
            };

            return Redirect(authorizeUrl);
        }

        /// <summary>
        /// Asks the user to confirm a logout the server would not perform on its own, such as one whose ID token hint
        /// names a session other than the current one.
        /// </summary>
        /// <remarks>
        /// <see cref="LogoutConfirmationPage"/> sends the browser here with the parameters of the original request
        /// and the value the server issued for the answer; the page posts them back to the end-session endpoint.
        /// </remarks>
        [HttpGet]
        public IActionResult Logout()
        {
            var endSessionEndpoint = endpointResolver.Resolve(OidcEndpoints.EndSession)
                ?? throw new InvalidOperationException("The end-session endpoint is not mapped.");

            return View(new LogoutViewModel
            {
                EndSessionEndpoint = endSessionEndpoint,
                Parameters =
                [
                    .. Request.Query
                        .Where(parameter => parameter.Key != LogoutConfirmationPage.AskedAgain)
                        .Select(parameter => KeyValuePair.Create(parameter.Key, parameter.Value.ToString())),
                ],
                AskedAgain = Request.Query.ContainsKey(LogoutConfirmationPage.AskedAgain),
            });
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
