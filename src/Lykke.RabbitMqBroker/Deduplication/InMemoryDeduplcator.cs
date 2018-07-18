using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Lykke.RabbitMqBroker.Deduplication
{
    public class InMemoryDeduplcator : IDeduplicator
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly System.Security.Cryptography.MD5 HashAlgorithm = System.Security.Cryptography.MD5.Create();

        public InMemoryDeduplcator(TimeSpan? expiration = null)
        {
            if (!expiration.HasValue)
                expiration = TimeSpan.FromDays(1);
        }

        public Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            var hash = Encoding.UTF8.GetString(HashAlgorithm.ComputeHash(value));

            lock (Cache)
            {
                if (Cache.TryGetValue(hash, out object _))
                    return Task.FromResult(false);
                Cache.Set(hash, true);
            }
            return Task.FromResult(true);
        }
    }
}
