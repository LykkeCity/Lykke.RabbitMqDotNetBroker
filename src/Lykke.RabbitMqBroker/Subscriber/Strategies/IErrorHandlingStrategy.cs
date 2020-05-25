// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Lykke.RabbitMqBroker.Subscriber.Strategies
{
	public interface IErrorHandlingStrategy
	{
		void Execute(Action handler, IMessageAcceptor ma, CancellationToken cancellationToken);
	}
}
