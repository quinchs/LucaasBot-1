using LucaasBot.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Websocket
{
    public class WebsocketPermissions
    {
        private static string[] staffEvents = new string[]
        {
            "modlog.added",
            "modlog.removed",
            "tickets.added",
        };


        public static bool hasPermissionForEvent(string session, params string[] events)
            => hasPermissionForEvent(DiscordAuthKeeper.GetUser(session));
        public static bool hasPermissionForEvent(WebUser user, params string[] events)
        {
            switch (user.Permission)
            {
                case HTTP.Types.SessionPermission.Staff:
                    foreach (var item in events)
                    {
                        if (!staffEvents.Contains(item))
                            return false;
                    }
                    return true;
                case HTTP.Types.SessionPermission.None:
                    return false;
            }
            return false;
        }
    }
}
