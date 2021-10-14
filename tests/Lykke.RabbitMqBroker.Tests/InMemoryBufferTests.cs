using System;
using System.Linq;
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
        public async Task ShouldWaitForEnqueue()
        {
            var buffer = new InMemoryBuffer();
            var cts = new CancellationTokenSource();
            var attemptsToRead = 0;
            
            var thread = new Thread(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var message = buffer.WaitOneAndPeek(cts.Token);
                        Thread.Sleep(50);
                        attemptsToRead++;
                        if (message != null)
                        {
                            buffer.Dequeue(cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        //that's ok )
                    }
                }
            });
            
            thread.Start();

            var writeTasks = Enumerable.Range(0, 10).Select(i =>
                Task.Factory.StartNew(() =>
                {
                    buffer.Enqueue(new RawMessage(new byte[0], string.Empty, null), cts.Token);
                    buffer.Enqueue(new RawMessage(new byte[0], string.Empty, null), cts.Token);
                })
            );

            await Task.WhenAll(writeTasks);
            await Task.Delay(TimeSpan.FromSeconds(2));

            cts.Cancel();
            
            Assert.AreEqual( 20, attemptsToRead);
        }
    }
}
