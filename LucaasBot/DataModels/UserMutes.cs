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
        public DateTime DateTime { get; set; }
        public string Type { get; set; }
        public int Time { get; set; }


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

        public UserMutes(ulong userid)
        {
            this.UserID = userid;
            SaveThis();
        }

        public static UserMutes GetOrCreateMute(ulong userid)
        {
            var mute = GetMute(userid);

            if (mute == null)
            {
                return new UserMutes(userid);
            }
            else
            {
                return mute;
            }
        }
        public void SaveThis()
        {
            MongoService.MutesCollection.ReplaceOne(x => x.UserID == this.UserID, this, new ReplaceOptions() { IsUpsert = true });
        }
    }
}
