#nullable enable
using System.Text.Json.Nodes;

namespace eShop.Identity.API.Services
{
    /// <summary>
    /// Turns an authenticated subject into OIDC claims from the ASP.NET Identity user store.
    /// </summary>
    /// <remarks>
    /// The library ships no default implementation, so this registration is mandatory: without it
    /// no ID token is issued and the userinfo endpoint answers invalid_token. Only claims the
    /// request actually asked for are produced, and the ask is derived from the granted scopes,
    /// so a claim that no scope declares is never requested and never reaches a token.
    /// </remarks>
    public class UserInfoProvider(UserManager<ApplicationUser> userManager) : IUserInfoProvider
    {
        public async Task<JsonObject?> GetUserInfoAsync(AuthSession authSession, IEnumerable<string> requestedClaims)
        {
            var user = await userManager.FindByIdAsync(authSession.Subject);
            if (user is null)
                return null;

            var claims = new JsonObject();
            foreach (var claim in requestedClaims)
            {
                JsonNode? value = claim switch
                {
                    IanaClaimTypes.PreferredUsername => user.UserName,
                    IanaClaimTypes.Name => user.Name,
                    IanaClaimTypes.Email => user.Email,
                    IanaClaimTypes.EmailVerified => user.EmailConfirmed,
                    IanaClaimTypes.PhoneNumber => user.PhoneNumber,
                    IanaClaimTypes.PhoneNumberVerified => user.PhoneNumber is not null && user.PhoneNumberConfirmed,

                    "last_name" => user.LastName,
                    "address_city" => user.City,
                    "address_country" => user.Country,
                    "address_state" => user.State,
                    "address_street" => user.Street,
                    "address_zip_code" => user.ZipCode,

                    _ => null,
                };

                if (value is not null)
                    claims[claim] = value;
            }

            return claims;
        }
    }
}
