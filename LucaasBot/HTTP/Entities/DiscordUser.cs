using Newtonsoft.Json;
using LucaasBot.Handlers;
using LucaasBot.HTTP.Types;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using Discord;
using System.Threading.Tasks;
using Discord.Rest;

namespace LucaasBot.HTTP
{
    public class WebUser
    {
        public readonly string SessionToken;
        public readonly SessionPermission Permission = SessionPermission.None;
        public User User { get; private set; }
        public string CurrentToken { get; private set; }
        public string Refresh { get; private set; }
        public string AuthType { get; private set; }

        public RestSelfUser CurrentUser
            => RestClient.CurrentUser;

        public Task<IGuildUser> LucaasUser
            => UserService.FindUserAsync(Program.LucaasGuild, Id);

        public readonly DiscordRestClient RestClient;

        public ulong Id
            => User.Id;
        public string Username
            => $"{User.Username}#{User.Discriminator}";


        public bool HasPermissions()
            => LucaasUser == null ? false : LucaasUser.Result.IsStaff();

        public WebUser() { }
        public WebUser(TokenResponse resp)
        {
            this.CurrentToken = resp.AccessToken;
            this.Refresh = resp.RefreshToken;
            this.AuthType = resp.TokenType;

            SessionToken = GenerateToken();

            getId();

            if (HasPermissions())
            {
                Permission = SessionPermission.Staff;
            }

            RestClient = new DiscordRestClient();
            RestClient.LoginAsync(TokenType.Bearer, this.CurrentToken).GetAwaiter().GetResult();
        }

        private static RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
        public static string GenerateToken()
        {
            byte[] token = new byte[32];

            random.GetBytes(token);

            return BitConverter.ToString(token).Replace("-", "");
        }

        private bool UseRefresh()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://discordapp.com/api/oauth2/token");
            webRequest.Method = "POST";
            string parameters = "client_id=772314985979969596&client_secret=s-y9VIm0gEScVqnL2pTN6O7gXUtxYpIP&grant_type=refresh_token&refresh_token=" + Refresh + "&redirect_uri=https://api.swissdev.team/apprentice/v1/auth";
            byte[] byteArray = Encoding.UTF8.GetBytes(parameters);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;
            Stream postStream = webRequest.GetRequestStream();

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();
            WebResponse response = webRequest.GetResponse();
            postStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(postStream);
            string responseFromServer = reader.ReadToEnd();

            TokenResponse t = JsonConvert.DeserializeObject<TokenResponse>(responseFromServer);

            CurrentToken = t.access_token;
            Refresh = t.refresh_token;

            return CurrentToken == t.access_token;
        }

        private bool isSecond = false;
        private ulong getId()
        {
            HttpClient c = new HttpClient();
            c.DefaultRequestHeaders.Add("Authorization", $"{AuthType} {CurrentToken}");
            var resp = c.GetAsync("https://discord.com/api/users/@me").Result;


            if(resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Resfresh our token

                if (!isSecond)
                {
                    UseRefresh();
                    isSecond = true;
                    return getId();
                }
                else
                {
                    isSecond = false;
                    throw new Exception("Bad Token");
                }
            }
            else
            {
                if (resp.IsSuccessStatusCode)
                {
                    string data = resp.Content.ReadAsStringAsync().Result;
                    User = JsonConvert.DeserializeObject<User>(data);
                    return User.id;
                }
            }

            throw new Exception("Bad Token");
        }

        /// <summary>
        ///     Converts the current object to a string
        /// </summary>
        /// <returns>The users username</returns>
        public override string ToString()
        {
            return this.Username;
        }
    }
}
