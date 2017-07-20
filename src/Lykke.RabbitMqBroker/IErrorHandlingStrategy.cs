using System;

namespace Lykke.RabbitMqBroker
{
	public interface IErrorHandlingStrategy
	{
		void Execute(Action handler, IMessageAcceptor ma);
	}
}