// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker
{
    internal sealed class MessageAcceptor : IMessageAcceptor
    {
        private readonly IModel _model;
        private readonly ulong _deliveryTag;

        public MessageAcceptor(IModel model, ulong deliveryTag)
        {
            _model = model;
            _deliveryTag = deliveryTag;
        }

        public void Accept()
        {
            _model.BasicAck(_deliveryTag, false);
        }

        public void Reject(bool requeue = false)
        {
            _model.BasicReject(_deliveryTag, requeue);
        }
    }
}
