using System;
using System.Threading;

namespace Lykke.RabbitMqBroker
{
	public interface IErrorHandlingStrategy
	{
		void Execute(Action handler, IMessageAcceptor ma, CancellationToken cancellationToken);
	}
}
