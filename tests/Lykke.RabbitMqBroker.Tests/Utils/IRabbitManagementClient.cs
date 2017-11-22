using System.Collections.Generic;

namespace RabbitMqBrokerTests
{
    public interface IRabbitManagementClient
    {
        IReadOnlyCollection<Vhost> GetVhosts();
        void DeleteVhost(string name);
        void AddVhost(string name);
        void SetFullPermissions(string vhost, string user);
    }
}