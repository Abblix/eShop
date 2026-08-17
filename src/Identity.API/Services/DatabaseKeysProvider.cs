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

        /// <summary>
        /// Serializes first-use key generation across instances. The table's primary key is the
        /// key id, which is random per generated key, so no constraint stops two replicas that
        /// both read an empty table from each minting a key - and then consumers disagree about
        /// which one signs. The advisory lock is transaction-scoped and keyed on this constant,
        /// so exactly one instance generates and the others re-read what it wrote.
        /// </summary>
        private const long SigningKeyGenerationLockId = 0x65_53_68_6F_70_4B_65_79; // "eShopKey"

        private Task<SigningKey> GenerateAsync(ApplicationDbContext db)
        {
            // The Npgsql retrying execution strategy the host wires in refuses user-initiated
            // transactions outside ExecuteAsync, so the lock-and-insert runs as one retriable unit.
            var strategy = db.Database.CreateExecutionStrategy();
            return strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({SigningKeyGenerationLockId})");

                // Re-read under the lock: the instance that lost the race finds the winner's key here.
                var existing = await db.SigningKeys.OrderBy(key => key.CreatedAt).FirstOrDefaultAsync();
                if (existing is not null)
                {
                    await transaction.CommitAsync();
                    return existing;
                }

                var key = JsonWebKeyFactory.CreateRsa(PublicKeyUsages.Signature);
                var record = new SigningKey
                {
                    KeyId = key.KeyId!,
                    Jwk = JsonSerializer.Serialize<JsonWebKey>(key),
                    CreatedAt = clock.GetUtcNow(),
                };

                db.SigningKeys.Add(record);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return record;
            });
        }
    }
}
