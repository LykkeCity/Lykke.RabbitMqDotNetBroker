using System.Threading;
using Lykke.RabbitMqBroker.Publisher;
using NUnit.Framework;

namespace Lykke.RabbitMqBroker.Tests
{
    [TestFixture]
    public class InMemoryBufferTests
    {
        [Test]
        public void ShouldWaitForEnqueue()
        {
            var buffer = new InMemoryBuffer();
            var stop = false;
            var attemptsToRead = 0;
            
            var thread = new Thread(() =>
            {
                while (!stop)
                {
                    var message = buffer.WaitOneAndPeek();
                    attemptsToRead++;
                    if (message != null)
                    {
                        buffer.Dequeue(CancellationToken.None);
                    }
                }
            });
            
            thread.Start();
            Thread.Sleep(10);
            buffer.Enqueue(new RawMessage(new byte[0], string.Empty), CancellationToken.None);
            Thread.Sleep(10);
            buffer.Enqueue(new RawMessage(new byte[0], string.Empty), CancellationToken.None);
            stop = true;
            Thread.Sleep(10);
            Assert.AreEqual( 2, attemptsToRead);
        }
    }
}