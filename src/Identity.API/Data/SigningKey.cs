namespace eShop.Identity.API.Data;

/// <summary>
/// A signing key kept in the identity database so that a restart does not invalidate tokens.
/// </summary>
/// <remarks>
/// The library has no key store and no key generation of its own: a signing key is configuration
/// the host supplies, which is why this table exists. Storing the private key in the application
/// database matches the posture of a developer signing credential and is not a production answer;
/// a real deployment keeps the key in a KMS or HSM and reads it through the Vault or Azure key
/// packages instead of this table.
/// </remarks>
public class SigningKey
{
    public string KeyId { get; set; }

    /// <summary>The full JWK, private key included, serialized as JSON.</summary>
    public string Jwk { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
