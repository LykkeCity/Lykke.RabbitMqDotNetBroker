using System.Text;

namespace Lykke.RabbitMqBroker.Deduplication
{
    public static class HashHelper
    {
        private static readonly System.Security.Cryptography.MD5 HashAlgorithm = System.Security.Cryptography.MD5.Create();

        public static string GetMd5Hash(byte[] value)
        {
            return Encoding.UTF8.GetString(HashAlgorithm.ComputeHash(value));
        }
    }
}
