// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using RabbitMQ.Client;

namespace Lykke.RabbitMqBroker.Subscriber
{
    internal sealed class MessageAcceptor : IMessageAcceptor
    {
        private readonly IModel _model;
        private readonly ulong _deliveryTag;
        private bool _alreadyProcessed;

        public MessageAcceptor(IModel model, ulong deliveryTag)
        {
            _model = model;
            _deliveryTag = deliveryTag;
        }

        public void Accept()
        {
            if (_alreadyProcessed)
                return;

            _model.BasicAck(_deliveryTag, false);

            _alreadyProcessed = true;
        }

        public void Reject(bool requeue = false)
        {
            if (_alreadyProcessed)
                return;

            _model.BasicReject(_deliveryTag, requeue);

            _alreadyProcessed = true;
        }
    }
}
