using LucaasBot.HTTP;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP
{
    public class DiscordAuthKeeper
    {
        private static IMongoCollection<WebUser> WebUsers
            => MongoService.WebUsers;

        public static WebUser GetUser(string session)
            => WebUsers.Find(x => x.SessionToken == session).FirstOrDefault();

        public static void LogoutUser(string session)
            => WebUsers.FindOneAndDelete(x => x.SessionToken == session);


        public static void AddOrReplace(WebUser u)
        {
            WebUsers.ReplaceOne(x => x.SessionToken == u.SessionToken, u, new ReplaceOptions() { IsUpsert = true});
        }
    }
}
