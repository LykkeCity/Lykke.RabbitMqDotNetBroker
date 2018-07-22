using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.RabbitMqBroker.Deduplication.Azure
{
    public class DuplicateEntity : TableEntity
    {
        internal static string GeneratePartitionKey(string val) => $"{val}_PK";
        internal static string GenerateRowKey(string val) => val;

        internal static DuplicateEntity Create(byte[] value)
        {
            var hash = HashHelper.GetMd5Hash(value);
            return new DuplicateEntity
            {
                PartitionKey = GeneratePartitionKey(hash),
                RowKey = GenerateRowKey(hash)
            };
        }
    }
}
