
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.PublisherSubscriber
{

    public class PublisherSubscriberMemory<T> : IMessageProducer<T>, IMessageConsumer<T>
    {

        private readonly List<Func<T, Task>> _eventHandlers = new List<Func<T, Task>>();

        public async Task ProduceAsync(T message)
        {
            foreach(var eventHandler in _eventHandlers){
                await eventHandler(message);
            }
        }

        public void Subscribe(Func<T, Task> callback){
            _eventHandlers.Add(callback);
        }

    }

}