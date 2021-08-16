using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP
{
    public class OAuthManager
    {
        public static async Task<TResponse> ExecuteAsync<TResponse>(string uri, string code, string grantType)
        {
            // form the url
            string parameters = 
                $"client_id={Program.Client.CurrentUser.Id}&" +
                $"client_secret={ConfigService.Config.OAuth.ClientSecret}&" +
                $"grant_type={grantType}&" +
                $"{(grantType == "authorization_code" ? "code" : "refresh_token")}={code}&" +
                $"redirect_uri={ConfigService.Config.OAuth.RedirectUri}";

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Method = "POST";

            byte[] byteArray = Encoding.UTF8.GetBytes(parameters);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;
            Stream postStream = webRequest.GetRequestStream();

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();
            HttpWebResponse response = (await webRequest.GetResponseAsync()) as HttpWebResponse;
            postStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(postStream);

            string responseFromServer = reader.ReadToEnd();

            if(response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<TResponse>(responseFromServer);
            }
            else
            {
                Logger.Write($"Failed to execute oauth'd request to {uri} : {responseFromServer}", Severity.Http, Severity.Critical);
                return default(TResponse);
            }
        }
    }
}
