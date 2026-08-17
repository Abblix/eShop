using System.Text.Json;
using Abblix.Oidc.Server.Common.Interfaces;

namespace eShop.Identity.API.Services
{
    /// <summary>
    /// Serves the signing keys from the identity database, generating one on first use.
    /// </summary>
    /// <remarks>
    /// The library reads keys through this seam rather than owning them, so key lifetime,
    /// storage and rotation are decisions the deployment makes. This implementation keeps the
    /// pair stable across restarts, which is what lets a token issued before a restart keep
    /// validating afterwards.
    /// </remarks>
    public class DatabaseKeysProvider(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TimeProvider clock) : IAuthServiceKeysProvider
    {
        public IAsyncEnumerable<JsonWebKey> GetEncryptionKeys(bool includePrivateKeys = false)
            => AsyncEnumerable.Empty<JsonWebKey>();

        public async IAsyncEnumerable<JsonWebKey> GetSigningKeys(bool includePrivateKeys = false)
        {
            await using var db = await contextFactory.CreateDbContextAsync();

            var stored = await db.SigningKeys.OrderBy(key => key.CreatedAt).ToListAsync();
            if (stored.Count == 0)
                stored = [await GenerateAsync(db)];

            foreach (var record in stored)
            {
                var key = JsonSerializer.Deserialize<JsonWebKey>(record.Jwk);
                if (key is not null)
                    yield return key.Sanitize(includePrivateKeys);
            }
        }

        private async Task<SigningKey> GenerateAsync(ApplicationDbContext db)
        {
            var key = JsonWebKeyFactory.CreateRsa(PublicKeyUsages.Signature);
            var record = new SigningKey
            {
                KeyId = key.KeyId!,
                Jwk = JsonSerializer.Serialize<JsonWebKey>(key),
                CreatedAt = clock.GetUtcNow(),
            };

            db.SigningKeys.Add(record);
            await db.SaveChangesAsync();
            return record;
        }
    }
}
