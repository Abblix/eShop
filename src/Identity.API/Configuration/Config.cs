using System.Security.Cryptography;
using System.Text;

namespace eShop.Identity.API.Configuration
{
    public class Config
    {
        // The API scopes eShop protects. Abblix registers the six standard OIDC scopes
        // (openid, profile, email, address, phone, offline_access) on its own, so only the
        // application-specific ones belong here. A scope absent from this list is refused as
        // invalid_scope no matter what a client lists in AllowedScopes: AllowedScopes narrows
        // the set, it never introduces a scope.
        //
        // A scope also declares which claims it asks for, and that ask is what reaches
        // IUserInfoProvider. The checkout form and the chatbot read the address and card claims
        // from the signed-in principal, so those claims hang off the orders scope: it is the one
        // both the web app and the mobile app request, and it is the flow the data serves.
        public static ScopeDefinition[] GetScopes() =>
        [
            new("orders",
                "last_name",
                "card_number",
                "card_holder",
                "card_security_number",
                "card_expiration",
                "address_city",
                "address_country",
                "address_state",
                "address_street",
                "address_zip_code"),
            new("basket"),
            new("webhooks"),
        ];

        // Clients that may ask this server for tokens.
        public static ClientInfo[] GetClients(IConfiguration configuration) =>
        [
            new ClientInfo("maui")
            {
                ClientName = "eShop MAUI OpenId Client",
                ClientSecrets = [Secret("secret")],

                // IdentityModel's OidcClient, which the MAUI app uses, sends the secret in the
                // Authorization header. Abblix matches the registered method exactly, so naming
                // the wrong one here fails the token exchange after a successful login.
                TokenEndpointAuthMethod = ClientAuthenticationMethods.ClientSecretBasic,

                // offline_access grants the refresh TOKEN; refresh_token grants the right to
                // spend it. The token endpoint checks this list on every call, so a client that
                // refreshes has to name both.
                AllowedGrantTypes = [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken],
                OfflineAccessAllowed = true,

                RedirectUris = [new Uri(configuration.GetRequiredValue("MauiCallback"), UriKind.Absolute)],
                PostLogoutRedirectUris =
                    [new Uri($"{configuration["MauiCallback"]}/Account/Redirecting", UriKind.Absolute)],

                AllowedScopes =
                [
                    Scopes.OpenId,
                    Scopes.Profile,
                    Scopes.OfflineAccess,
                    "orders",
                    "basket",
                    "webhooks",
                ],

                ForceUserClaimsInIdentityToken = true,
                AccessTokenExpiresIn = TimeSpan.FromHours(2),
                IdentityTokenExpiresIn = TimeSpan.FromHours(2),
            },

            new ClientInfo("webapp")
            {
                ClientName = "WebApp Client",
                ClientUri = new Uri(configuration.GetRequiredValue("WebAppClient"), UriKind.Absolute),
                ClientSecrets = [Secret("secret")],

                // AddOpenIdConnect puts client_id and client_secret in the token request body,
                // which is client_secret_post rather than the client_secret_basic default.
                TokenEndpointAuthMethod = ClientAuthenticationMethods.ClientSecretPost,

                AllowedGrantTypes = [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken],
                OfflineAccessAllowed = true,

                RedirectUris =
                    [new Uri($"{configuration["WebAppClient"]}/signin-oidc", UriKind.Absolute)],
                PostLogoutRedirectUris =
                    [new Uri($"{configuration["WebAppClient"]}/signout-callback-oidc", UriKind.Absolute)],

                AllowedScopes =
                [
                    Scopes.OpenId,
                    Scopes.Profile,
                    Scopes.OfflineAccess,
                    "orders",
                    "basket",
                    "webhooks",
                ],

                ForceUserClaimsInIdentityToken = true,
                AccessTokenExpiresIn = TimeSpan.FromHours(2),
                IdentityTokenExpiresIn = TimeSpan.FromHours(2),
            },

            new ClientInfo("webhooksclient")
            {
                ClientName = "Webhooks Client",
                ClientUri = new Uri(configuration.GetRequiredValue("WebhooksWebClient"), UriKind.Absolute),
                ClientSecrets = [Secret("secret")],
                TokenEndpointAuthMethod = ClientAuthenticationMethods.ClientSecretPost,

                AllowedGrantTypes = [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken],
                OfflineAccessAllowed = true,

                RedirectUris =
                    [new Uri($"{configuration["WebhooksWebClient"]}/signin-oidc", UriKind.Absolute)],
                PostLogoutRedirectUris =
                    [new Uri($"{configuration["WebhooksWebClient"]}/signout-callback-oidc", UriKind.Absolute)],

                AllowedScopes = [Scopes.OpenId, Scopes.Profile, Scopes.OfflineAccess, "webhooks"],

                ForceUserClaimsInIdentityToken = true,
                AccessTokenExpiresIn = TimeSpan.FromHours(2),
                IdentityTokenExpiresIn = TimeSpan.FromHours(2),
            },
        ];

        private static ClientSecret Secret(string value)
            => new() { Sha256Hash = SHA256.HashData(Encoding.UTF8.GetBytes(value)) };
    }
}
