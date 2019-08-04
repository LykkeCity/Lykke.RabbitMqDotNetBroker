using System.Threading;
using System.Threading.Tasks;
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
                    var message = buffer.WaitOneAndPeek(CancellationToken.None);
                    Thread.Sleep(1);
                    attemptsToRead++;
                    if (message != null)
                    {
                        buffer.Dequeue(CancellationToken.None);
                    }
                }
            });
            
            thread.Start();

            for (int i = 0; i < 10; i++)
            {
                Task.Factory.StartNew(() =>
                    {
                        buffer.Enqueue(new RawMessage(new byte[0], string.Empty), CancellationToken.None);
                        buffer.Enqueue(new RawMessage(new byte[0], string.Empty), CancellationToken.None);
                    });    
            }
            
            Thread.Sleep(25);
            stop = true;
            Assert.AreEqual( 20, attemptsToRead);
        }
    }
}