// Copyright (c) Lykke Corp.
// Licensed under the MIT License. See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Lykke.RabbitMqBroker.Tests.Utils
{
    public sealed class RabbitManagementClient : IRabbitManagementClient
    {
        private readonly HttpClient _client;

        public RabbitManagementClient(string rabbitUrl, string user, string password)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(rabbitUrl)
            };
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}")));
        }

        public IReadOnlyCollection<Vhost> GetVhosts()
        {
            var responce = _client.GetAsync(@"/api/vhosts").Result;
            var vhosts = JsonConvert.DeserializeObject<IReadOnlyCollection<Vhost>>(responce.Content.ReadAsStringAsync().Result);
            return vhosts;
        }

        public void DeleteVhost(string name)
        {
            var result = _client.DeleteAsync($"/api/vhosts/{name}").Result;
            result.EnsureSuccessStatusCode();
        }

        public void AddVhost(string name)
        {
            var result = _client.PutAsync($"/api/vhosts/{name}", null).Result;
            result.EnsureSuccessStatusCode();
        }

        public void SetFullPermissions(string vhost, string user)
        {
            var result = _client.PutAsync(
                $"/api/permissions/{vhost}/{user}",
                new StringContent("{\"configure\":\".*\",\"write\":\".*\",\"read\":\".*\"}", Encoding.UTF8, "application/json")).Result;
            result.EnsureSuccessStatusCode();
        }
    }
}
