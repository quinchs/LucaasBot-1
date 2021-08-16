using LucaasBot.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace LucaasBot.HTTP
{
    public static class AuthHelper
    {
        public static string GetUsername(this HttpListenerContext c)
        {
            if (!c.Request.Cookies.Any(x => x.Name == "csSessionID"))
                return null;

            var sesh = c.Request.Cookies["csSessionID"];

            var user = DiscordAuthKeeper.GetUser(sesh.Value);

            if (user == null)
                return null;
            return user.Username;
        }
        public static WebUser GetWebUser(this HttpListenerContext c)
        {
            // Check if they have the discord auth
            if (!c.Request.Cookies.Any(x => x.Name == "csSessionID"))
            {
                return null;
            }

            var sesh = c.Request.Cookies["csSessionID"];

            return GetWebUser(sesh.Value);
        }

        public static WebUser GetWebUser(string session)
        {
            var user = DiscordAuthKeeper.GetUser(session);

            if (user == null)
            {
                return null;
            }

            if (!user.HasPermissions())
            {
                return null;
            }

            return user;
        }

    }
}
