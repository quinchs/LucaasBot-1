using Newtonsoft.Json;
using LucaasBot.Handlers;
using LucaasBot.HTTP.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Routes
{
    /// <summary>
    /// This class will handle all the http requests from discord that corilate to discord OAuth2
    /// </summary>
    public class DiscordCallback
    {
        [Route(@"^\/auth", "GET", true)]
        public static async Task auth(HttpListenerContext c, MatchCollection m)
        {
            try
            {
                var code = c.Request.QueryString.Get("code");

                var response = await OAuthManager.ExecuteAsync<TokenResponse>("https://discordapp.com/api/oauth2/token", code, "authorization_code");

                if(response == null)
                {
                    c.Response.StatusCode = 500;
                    c.Response.Close();
                    return;
                }

                var webUser = new WebUser(response);

                DiscordAuthKeeper.AddOrReplace(webUser);

                if (c.Request.QueryString.AllKeys.Contains("state"))
                    c.Response.Redirect(c.Request.QueryString["state"]);

                c.Response.Headers.Add("Set-Cookie", $"csSessionID={webUser.SessionToken}; Expires={DateTime.UtcNow.AddDays(7).ToString("R")}");
                c.Response.Close();
            }
            catch(Exception x)
            {
                Logger.Write(x, Severity.Http, Severity.Critical);
            }
        }
    }
}
