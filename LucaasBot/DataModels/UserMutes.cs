using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.DataModels
{
    [BsonIgnoreExtraElements]
    public class UserMutes
    {
        public ulong UserID { get; set; }

        public DateTime UnmuteTime { get; set; }


        public static UserMutes GetMute(ulong userid)
        {
            var result = MongoService.MutesCollection.Find(x => x.UserID == userid);

            if (result.Any())
            {
                return result.First();
            }
            else
            {
                return null;
            }
        }

        public UserMutes(ulong userid, DateTime unmuteTime)
        {
            this.UserID = userid;
            this.UnmuteTime = unmuteTime;
            SaveThis();
        }

        public DeleteResult Delete()
            => MongoService.MutesCollection.DeleteOne(x => x.UserID == this.UserID);

        public void SaveThis()
        {
            MongoService.MutesCollection.ReplaceOne(x => x.UserID == this.UserID, this, new ReplaceOptions() { IsUpsert = true });
        }
    }
}
