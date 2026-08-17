namespace eShop.Identity.API.Configuration
{
    /// <summary>
    /// The shape of the <c>Oidc</c> section of the application configuration.
    /// </summary>
    /// <remarks>
    /// The library's own types are deliberately not bound directly. Two behaviours of the
    /// configuration binder make that unsafe: a collection with a non-empty default is appended to
    /// rather than replaced, so a client would silently keep a grant type the configuration does
    /// not list; and a positional record whose collection parameter is absent is dropped from the
    /// bound array entirely, so a scope carrying no claims would disappear without an error. These
    /// plain types have no defaults to inherit and no constructor to satisfy, and the mapping below
    /// states every value exactly once.
    /// </remarks>
    public class OidcConfiguration
    {
        public ScopeConfiguration[] Scopes { get; set; } = [];

        /// <summary>
        /// The client registry, keyed by client identifier.
        /// </summary>
        /// <remarks>
        /// A map rather than a list, so that a setting can be overridden by naming the client it
        /// belongs to. An array would force overrides to address a client by its position, which
        /// silently retargets them the moment the entries are reordered.
        /// </remarks>
        public Dictionary<string, ClientConfiguration> Clients { get; set; } = [];

        public ScopeDefinition[] ToScopeDefinitions()
            => [.. Scopes.Select(scope => new ScopeDefinition(scope.Scope, scope.ClaimTypes))];

        public ClientInfo[] ToClientInfos()
            => [.. Clients.Select(entry => entry.Value.ToClientInfo(entry.Key))];
    }

    public class ScopeConfiguration
    {
        public string Scope { get; set; }

        /// <summary>
        /// The claims this scope asks for. They are what the server later requests from the user
        /// store, so a claim absent here never reaches a token however the store is implemented.
        /// </summary>
        public string[] ClaimTypes { get; set; } = [];
    }

    public class ClientConfiguration
    {
        public string ClientName { get; set; }

        /// <summary>
        /// Where the client application answers.
        /// </summary>
        /// <remarks>
        /// The address is the one thing about a client that the application itself does not decide:
        /// an orchestrator assigns it at run time, so it is kept as a single scalar an environment
        /// variable can replace. The paths below stay in this file, which is why an override never
        /// has to address a list element by its position.
        /// </remarks>
        public string BaseAddress { get; set; }

        /// <summary>
        /// The shared secret, kept as a SHA-256 hash in Base64 rather than in the clear.
        /// </summary>
        /// <remarks>
        /// The server compares hashes and never reads a plaintext secret, so configuration has no
        /// reason to carry one. Produce the value as the Base64 of the SHA-256 of the secret.
        /// </remarks>
        public string SecretSha256 { get; set; }

        public string TokenEndpointAuthMethod { get; set; }

        public string[] AllowedGrantTypes { get; set; } = [];

        /// <summary>Paths under <see cref="BaseAddress"/> the server may return the user to.</summary>
        public string[] RedirectPaths { get; set; } = [];

        /// <summary>Paths under <see cref="BaseAddress"/> the server may return the user to after logout.</summary>
        public string[] PostLogoutRedirectPaths { get; set; } = [];

        public string[] AllowedScopes { get; set; } = [];

        public bool ForceUserClaimsInIdentityToken { get; set; }

        public TimeSpan AccessTokenExpiresIn { get; set; }

        public TimeSpan IdentityTokenExpiresIn { get; set; }

        public ClientInfo ToClientInfo(string clientId)
        {
            var baseAddress = new Uri(BaseAddress, UriKind.Absolute);

            return new ClientInfo(clientId)
            {
                ClientName = ClientName,
                ClientUri = baseAddress,
                ClientSecrets = [new ClientSecret { Sha256Hash = Convert.FromBase64String(SecretSha256) }],
                TokenEndpointAuthMethod = TokenEndpointAuthMethod,
                AllowedGrantTypes = AllowedGrantTypes,
                RedirectUris = [.. RedirectPaths.Select(path => new Uri(baseAddress, path))],
                PostLogoutRedirectUris = [.. PostLogoutRedirectPaths.Select(path => new Uri(baseAddress, path))],
                AllowedScopes = AllowedScopes,
                ForceUserClaimsInIdentityToken = ForceUserClaimsInIdentityToken,
                AccessTokenExpiresIn = AccessTokenExpiresIn,
                IdentityTokenExpiresIn = IdentityTokenExpiresIn,
            };
        }
    }
}
