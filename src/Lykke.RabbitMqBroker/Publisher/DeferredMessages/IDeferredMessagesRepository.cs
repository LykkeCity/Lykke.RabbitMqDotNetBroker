using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Lykke.RabbitMqBroker.Publisher.DeferredMessages
{
    /// <summary>
    /// Deferred messages repository, used by deferred messaging core, to temprorary store messages.
    /// Stored messages should survive app restarts
    /// </summary>
    [PublicAPI]
    public interface IDeferredMessagesRepository
    {
        /// <summary>
        /// Implementation should save the message with the given delivery date
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="deliverAt">The date, when message should be delivered</param>
        Task SaveAsync(byte[] message, DateTime deliverAt);

        /// <summary>
        /// Implementation should return messages, which have the overdue delivery date.
        /// If there is a large amount of these messages, it is permissible to return only the part of them,
        /// but messages with older delivery date should be returned first.
        /// </summary>
        /// <param name="forTheMoment">The moment after wich the messages delivery date should be considered as overdue</param>
        Task<IReadOnlyList<DeferredMessageEnvelope>> GetOverdueMessagesAsync(DateTime forTheMoment);

        /// <summary>
        /// Implementation should remove the message with the given key
        /// </summary>
        /// <param name="key">The key</param>
        Task RemoveAsync(string key);
    }
}
