using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.DataModels
{
    public class Modlogs
    {
        public ObjectId _id { get; set; }

        public ulong ModID { get; set; }

        public ulong UserID { get; set; }

        public string Reason { get; set; }

        public DateTime DateCreated { get; set; }
        public string Action { get; set; }

        public Modlogs() { } // Just used for mongo.

        public Modlogs(ulong modid, DiscordUser userAccount, string reason, string action)
        {
            this.ModID = modid;
            this.UserID = userAccount.UserId;
            this.Reason = reason;
            this.DateCreated = DateTime.UtcNow;
            this.Action = action;
            SaveThis();
        }

        private void SaveThis()
            => MongoService.ModlogsCollection.InsertOne(this);

        public static List<Modlogs> GetModlogs(ulong userAccountId)
        {
            var modlogs = MongoService.ModlogsCollection.Find(x => x.UserID == userAccountId);

            if (modlogs.Any())
                return modlogs.ToList();
            else return new List<Modlogs>();
        }

    }
}
