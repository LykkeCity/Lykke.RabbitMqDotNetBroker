using System;
using System.Text;
using System.Threading.Tasks;
using Lykke.RabbitMqBroker.Deduplication;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Subscriber.Middleware.Deduplication
{
    public class InMemoryDeduplicationMiddleware<T> : IEventMiddleware<T>
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        private readonly TimeSpan _expiration;
        private readonly string _deduplicatorHeader;

        public InMemoryDeduplicationMiddleware(
            TimeSpan? expiration = null,
            string deduplicatorHeader = null)
        {
            _expiration = expiration ?? TimeSpan.FromDays(1);
            _deduplicatorHeader = deduplicatorHeader;
        }

        public Task ProcessAsync(IEventContext<T> context)
        {
            var containsHeader = context.BasicProperties?.Headers.ContainsKey(_deduplicatorHeader) ?? false;
            
            var deduplicationHeaderBytes = string.IsNullOrEmpty(_deduplicatorHeader) || !containsHeader
                ? Array.Empty<byte>()
                : Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        context.BasicProperties.Headers[_deduplicatorHeader],
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            var deduplicationBytes = deduplicationHeaderBytes.Length == 0
                ? context.Body.ToArray()
                : deduplicationHeaderBytes;

            var hash = HashHelper.GetMd5Hash(deduplicationBytes);

            lock (Cache)
            {
                if (Cache.TryGetValue(hash, out object _))
                {
                    context.MessageAcceptor.Accept();
                    return Task.CompletedTask;
                }

                Cache.Set(hash, true, _expiration);
                return context.InvokeNextAsync();
            }
        }
    }
}
