using eShop.Identity.API.Configuration;
using Microsoft.Extensions.Configuration;

namespace eShop.Application.UnitTests;

[TestClass]
public class IdentityConfigurationTests
{
    [TestMethod]
    public void ClientsUseConfiguredCallbackUrlsAndExpectedScopes()
    {
        var values = new Dictionary<string, string?>
        {
            ["Oidc:Clients:webapp:BaseAddress"] = "https://webapp.test",
            ["Oidc:Clients:webapp:RedirectPaths:0"] = "/signin-oidc",
            ["Oidc:Clients:webapp:PostLogoutRedirectPaths:0"] = "/signout-callback-oidc",
            ["Oidc:Clients:webapp:Client:AllowedScopes:0"] = "openid",
            ["Oidc:Clients:webapp:Client:AllowedScopes:1"] = "basket",
            ["Oidc:Clients:webapp:Client:AllowedScopes:2"] = "orders",
            ["Oidc:Clients:webhooksclient:BaseAddress"] = "https://webhooks-client.test",
            ["Oidc:Clients:webhooksclient:Client:AllowedScopes:0"] = "webhooks",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var clients = configuration.GetSection("Oidc").Get<OidcConfiguration>()!
            .ToClientInfos()
            .ToDictionary(client => client.ClientId);

        CollectionAssert.Contains(clients["webapp"].RedirectUris!.ToList(), new Uri("https://webapp.test/signin-oidc"));
        CollectionAssert.Contains(clients["webapp"].PostLogoutRedirectUris!.ToList(), new Uri("https://webapp.test/signout-callback-oidc"));
        CollectionAssert.Contains(clients["webapp"].AllowedScopes!.ToList(), "basket");
        CollectionAssert.Contains(clients["webapp"].AllowedScopes!.ToList(), "orders");
        CollectionAssert.Contains(clients["webhooksclient"].AllowedScopes!.ToList(), "webhooks");
    }
}
