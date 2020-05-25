// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Lykke.RabbitMqBroker.Tests.Utils
{
    public interface IRabbitManagementClient
    {
        IReadOnlyCollection<Vhost> GetVhosts();
        void DeleteVhost(string name);
        void AddVhost(string name);
        void SetFullPermissions(string vhost, string user);
    }
}
