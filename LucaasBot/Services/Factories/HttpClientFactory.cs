using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Services
{
    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var c = new HttpClient();

            c.DefaultRequestHeaders.Add("User-Agent", $"LucaasBot/v{Environment.Version}");

            return c;
        }
    }
}
