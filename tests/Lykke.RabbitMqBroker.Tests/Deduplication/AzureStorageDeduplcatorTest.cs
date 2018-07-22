using AzureStorage.Tables;
using Lykke.RabbitMqBroker.Deduplication.Azure;
using NUnit.Framework;

namespace Lykke.RabbitMqBroker.Tests.Deduplication
{
    [TestFixture]
    public class AzureStorageDeduplcatorTest
    {
        private readonly AzureStorageDeduplicator _deduplcator = new AzureStorageDeduplicator(new NoSqlTableInMemory<DuplicateEntity>());

        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void EnsureNotDuplicateAsync()
        {
            var value = new byte[] {1, 2, 3 };

            var notDuplicate = _deduplcator.EnsureNotDuplicateAsync(value).Result;
            Assert.True(notDuplicate);

            notDuplicate = _deduplcator.EnsureNotDuplicateAsync(value).Result;
            Assert.False(notDuplicate);
        }
    }
}
