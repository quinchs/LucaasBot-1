using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    public static class HapsyService
    {
        public static async Task<string> GetImageLink(string fPath)
        {
            if (!File.Exists(fPath))
            {
                return null;
            }

            var client = new RestClient("https://upload.hapsy.net/upload");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", ConfigService.Config.HapsyToken);
            request.AddFile("file", fPath);

            IRestResponse response = await client.ExecuteAsync(request);
            var json = JsonConvert.DeserializeObject<dynamic>(response.Content);

            return json.files[0].url;
        }

        public static async Task<string> GetImageLink(byte[] data, string fname)
        {
            var client = new RestClient("https://upload.hapsy.net/upload");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", ConfigService.Config.HapsyToken);
            request.AddFile("file", data, fname);

            IRestResponse response = await client.ExecuteAsync(request);
            var json = JsonConvert.DeserializeObject<dynamic>(response.Content);

            return json.files[0].url;
        }
    }
}
