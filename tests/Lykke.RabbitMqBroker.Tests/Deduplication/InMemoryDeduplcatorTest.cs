using Lykke.RabbitMqBroker.Deduplication;
using NUnit.Framework;

namespace Lykke.RabbitMqBroker.Tests.Deduplication
{
    [TestFixture]
    public class InMemoryDeduplcatorTest
    {
        private readonly InMemoryDeduplcator _deduplcator = new InMemoryDeduplcator();

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
