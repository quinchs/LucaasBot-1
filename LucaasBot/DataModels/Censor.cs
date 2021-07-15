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
    public class Censor
    {
        public ulong GuildID { get; set; }
        public string Phrase { get; set; }

        public static Censor GetCensors(ulong guildid)
        {
            var result = MongoService.CensoredCollection.Find(x => x.GuildID == guildid);

            if (result.Any())
            {
                return result.First();
            }
            else
            {
                return null;
            }
        }

        public Censor(ulong guildid)
        {
            this.GuildID = guildid;
            SaveThis();
        }

        public static Censor GetOrCreateCensors(ulong guildid)
        {
            var mute = GetCensors(guildid);

            if (mute == null)
            {
                return new Censor(guildid);
            }
            else
            {
                return mute;
            }
        }
        public void SaveThis()
        {
            MongoService.CensoredCollection.ReplaceOne(x => x.GuildID == this.GuildID, this, new ReplaceOptions() { IsUpsert = true });
        }
    }
}
