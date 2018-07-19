using System;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace Lykke.RabbitMqBroker.Deduplication
{
    /// <summary>
    /// Checks if the passed value was already received some time ago.
    /// In-memory implementation.
    /// </summary>
    [PublicAPI]
    public class InMemoryDeduplcator : IDeduplicator
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly System.Security.Cryptography.MD5 HashAlgorithm = System.Security.Cryptography.MD5.Create();
        private readonly TimeSpan _expiration;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="expiration">Cache expiration time.</param>
        public InMemoryDeduplcator(TimeSpan? expiration = null)
        {
            _expiration = expiration ?? TimeSpan.FromDays(1);
        }

        /// <summary>
        /// Check if the passed value was already received some time ago.
        /// </summary>
        public Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            var hash = Encoding.UTF8.GetString(HashAlgorithm.ComputeHash(value));

            lock (Cache)
            {
                if (Cache.TryGetValue(hash, out object _))
                    return Task.FromResult(false);
                Cache.Set(hash, true, _expiration);
            }
            return Task.FromResult(true);
        }
    }
}
