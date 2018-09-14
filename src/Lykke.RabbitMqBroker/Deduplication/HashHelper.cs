// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;

namespace Lykke.RabbitMqBroker.Deduplication
{
    public static class HashHelper
    {
        public static string GetMd5Hash(byte[] value)
        {
            return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(value)).Replace("-","");
        }
    }
}
