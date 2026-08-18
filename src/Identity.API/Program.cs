using Abblix.Oidc.Server.AspNetCore;
using Abblix.Oidc.Server.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllersWithViews();

builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

// The keys provider is a singleton and opens a context per read, so the scoped context
// registered above is not enough on its own.
builder.Services.AddDbContextFactory<ApplicationDbContext>(lifetime: ServiceLifetime.Singleton);

// Apply database migration automatically. Note that this approach is not
// recommended for production scenarios. Consider generating SQL scripts from
// migrations instead.
builder.Services.AddMigration<ApplicationDbContext, UsersSeed>();

// Identity owns users and credentials. AddIdentityCore brings the user store, password hashing
// and lockout without Identity's own cookie schemes: the OIDC session registered below is the
// only session, so two cookie stacks can never disagree about who is signed in.
builder.Services.AddIdentityCore<ApplicationUser>()
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

// Identity stores a PBKDF2-HMAC-SHA512 hash with a per-user random salt. The work factor is bound
// from configuration so it can be raised to current guidance without a recompile; left implicit it
// silently keeps whatever the framework default was on the day the app was written.
builder.Services.Configure<PasswordHasherOptions>(builder.Configuration.GetSection("PasswordHasher"));

// The cookie the server writes at sign-in and reads back on every authorization request.
builder.Services.AddAuthentication()
        .AddCookie(options => options.Events.OnValidatePrincipal = SessionValidation.RejectUsersNoLongerAllowedIn);

// How long that session lives is a deployment decision rather than a property of the code, so it is
// bound from configuration. The validation above is not: it is behaviour, and a deployment that could
// switch it off would be a deployment that can silently keep locked-out users signed in.
builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    builder.Configuration.GetSection("Session"));

// Clients and scopes are configuration, not code: the whole registry lives in the Oidc section of
// appsettings, so a deployment can add a client or change a redirect path without a rebuild.
var oidcConfiguration = builder.Configuration.GetSection("Oidc").Get<OidcConfiguration>()
    ?? throw new InvalidOperationException("The Oidc configuration section is missing.");

builder.Services.AddOidcServices(options =>
{
    options.LoginUri = new Uri("/Account/Login", UriKind.Relative);
    options.Scopes = oidcConfiguration.Scopes;
    options.Clients = oidcConfiguration.ToClientInfos();
});

// The library registers the CORS policy the protocol endpoints name, and an empty origin list
// means any origin may read them. eShop has no browser-based client, so the complete list of
// origins that could ever need them is the addresses its own clients answer on.
builder.Services.Configure<OidcCorsOptions>(
    options => Array.ForEach(oidcConfiguration.ClientOrigins(), options.AllowedOrigins.Add));

// Signing keys come from the identity database rather than from the options above, so a restart
// does not invalidate tokens that are still within their lifetime.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAuthServiceKeysProvider, DatabaseKeysProvider>();

// Claims are produced from the Identity user store. The library ships no default here, so
// without this registration no ID token is issued at all.
builder.Services.AddScoped<IUserInfoProvider, UserInfoProvider>();

// Authorization codes and pushed requests live here. In-memory is single-node only: across more
// than one replica or a restart this cache has to be shared and durable, or a code issued by one
// instance is unknown to the next.
builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStaticFiles();

// This cookie policy fixes login issues with Chrome 80+ using HTTP
app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });
app.UseRouting();

// The protocol endpoints carry CORS metadata, because a browser-based client reads the discovery
// document and the JWKS cross-origin. Without this call every one of them answers 500: routing
// finds the metadata and no middleware honours it. The policy itself is registered by the library.
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// The protocol endpoints are attribute-routed controllers supplied by the library, so they are
// mapped the same way any other controller is.
app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();
