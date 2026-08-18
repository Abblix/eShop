namespace eShop.Identity.API.Configuration
{
    /// <summary>
    /// The shape of the <c>Oidc</c> section of the application configuration.
    /// </summary>
    /// <remarks>
    /// Scopes bind straight into the library's own type: it carries no defaults to inherit and a
    /// parameterless constructor, so a scope that declares no claims - the ordinary shape of an API
    /// scope - survives binding. Clients do not, and for two reasons. The configuration binder
    /// appends to a collection that has a non-empty default rather than replacing it, so a client
    /// bound directly would silently keep a grant type this file does not list. And a client is
    /// described here by a base address plus paths under it, which is what keeps a deployment
    /// override free of list indices.
    /// </remarks>
    public class OidcConfiguration
    {
        public ScopeDefinition[] Scopes { get; set; } = [];

        /// <summary>
        /// The client registry, keyed by client identifier.
        /// </summary>
        /// <remarks>
        /// A map rather than a list, so that a setting can be overridden by naming the client it
        /// belongs to. An array would force overrides to address a client by its position, which
        /// silently retargets them the moment the entries are reordered.
        /// </remarks>
        public Dictionary<string, ClientConfiguration> Clients { get; set; } = [];

        public ClientInfo[] ToClientInfos()
            => [.. Clients.Select(entry => entry.Value.ToClientInfo(entry.Key))];

        /// <summary>
        /// The origins the registered clients answer on, in the form a CORS policy compares against.
        /// </summary>
        public string[] ClientOrigins()
            => [.. Clients.Values
                .Select(client => new Uri(client.BaseAddress, UriKind.Absolute).GetLeftPart(UriPartial.Authority))
                .Distinct(StringComparer.Ordinal)];
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

        /// <summary>How long an authorization code stays usable. Absent means the library's default.</summary>
        /// <remarks>
        /// Nullable on purpose. A bare <see cref="TimeSpan"/> that the file omits binds to zero, and zero
        /// is a value the library honours, so a client added by a settings edit with one line missing
        /// would issue tokens that have already expired.
        /// </remarks>
        public TimeSpan? AuthorizationCodeExpiresIn { get; set; }

        /// <inheritdoc cref="AuthorizationCodeExpiresIn"/>
        public TimeSpan? AccessTokenExpiresIn { get; set; }

        /// <inheritdoc cref="AuthorizationCodeExpiresIn"/>
        public TimeSpan? IdentityTokenExpiresIn { get; set; }

        public ClientInfo ToClientInfo(string clientId)
        {
            var baseAddress = new Uri(BaseAddress, UriKind.Absolute);

            var clientInfo = new ClientInfo(clientId)
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
            };

            if (AuthorizationCodeExpiresIn is { } authorizationCodeExpiresIn)
                clientInfo.AuthorizationCodeExpiresIn = authorizationCodeExpiresIn;

            if (AccessTokenExpiresIn is { } accessTokenExpiresIn)
                clientInfo.AccessTokenExpiresIn = accessTokenExpiresIn;

            if (IdentityTokenExpiresIn is { } identityTokenExpiresIn)
                clientInfo.IdentityTokenExpiresIn = identityTokenExpiresIn;

            return clientInfo;
        }
    }
}
