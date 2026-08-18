namespace eShop.Identity.API.Configuration
{
    /// <summary>
    /// The shape of the <c>Oidc</c> section of the application configuration.
    /// </summary>
    /// <remarks>
    /// Scopes and clients both bind into the library's own types. What is left here is the one thing
    /// those types do not express: a client is described by a base address plus paths under it, so
    /// that a deployment overrides a single scalar instead of addressing a list element by position.
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

        /// <summary>Paths under <see cref="BaseAddress"/> the server may return the user to.</summary>
        public string[] RedirectPaths { get; set; } = [];

        /// <summary>Paths under <see cref="BaseAddress"/> the server may return the user to after logout.</summary>
        public string[] PostLogoutRedirectPaths { get; set; } = [];

        /// <summary>
        /// Everything else about the client, bound straight into the library's own model.
        /// </summary>
        /// <remarks>
        /// The binder fills this instance rather than building a new one, which is what makes the
        /// library's defaults apply to whatever the file leaves out: a lifetime nobody states keeps
        /// the value the library ships, instead of binding to zero as a local copy of the property
        /// would. The identifier is not stated here either, because the key of the entry is it.
        /// </remarks>
        public ClientInfo Client { get; set; } = new(string.Empty);

        public ClientInfo ToClientInfo(string clientId)
        {
            var baseAddress = new Uri(BaseAddress, UriKind.Absolute);

            Client.ClientId = clientId;
            Client.ClientUri = baseAddress;
            Client.RedirectUris = [.. RedirectPaths.Select(path => new Uri(baseAddress, path))];
            Client.PostLogoutRedirectUris = [.. PostLogoutRedirectPaths.Select(path => new Uri(baseAddress, path))];

            return Client;
        }
    }
}
