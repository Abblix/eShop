using Microsoft.AspNetCore.Authentication.Cookies;

namespace eShop.Identity.API.Services
{
    /// <summary>
    /// Re-checks the user store while validating the server's own session cookie.
    /// </summary>
    /// <remarks>
    /// The library rebuilds the session from this cookie and never consults the user store while doing
    /// so, which is correct for a protocol library and leaves a gap the host has to close: without this
    /// check a user who has just been locked out keeps a working session, and the same browser goes on
    /// obtaining freshly issued tokens for every client until the cookie expires. Identity's own full
    /// registration closes an equivalent gap on its own cookie; this host does not use that cookie.
    /// </remarks>
    public static class SessionValidation
    {
        public static async Task RejectUsersNoLongerAllowedIn(CookieValidatePrincipalContext context)
        {
            var subject = context.Principal?.FindFirstValue(JwtClaimTypes.Subject);
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

            var user = subject is null ? null : await userManager.FindByIdAsync(subject);
            if (user is not null && !await userManager.IsLockedOutAsync(user))
                return;

            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
