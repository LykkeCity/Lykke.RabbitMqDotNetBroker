// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace Lykke.RabbitMqBroker.Deduplication
{
    /// <inheritdoc/>
    /// <remarks>In-memory implementation.</remarks>
    [PublicAPI]
    public class InMemoryDeduplcator : IDeduplicator
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private readonly TimeSpan _expiration;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="expiration">Cache expiration time.</param>
        public InMemoryDeduplcator(TimeSpan? expiration = null)
        {
            _expiration = expiration ?? TimeSpan.FromDays(1);
        }

        public Task<bool> EnsureNotDuplicateAsync(byte[] value)
        {
            var hash = HashHelper.GetMd5Hash(value);

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
