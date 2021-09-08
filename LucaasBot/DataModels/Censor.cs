using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LucaasBot;
using MongoDB.Bson;
using Discord;

namespace LucaasBot.DataModels
{

    [BsonIgnoreExtraElements]
    public class Guild
    {
        public ulong GuildId { get; set; }

        public Guild() { }

        public Guild(IGuild guild)
        {
            this.GuildId = guild.Id;
            SaveThis1();
        }

        public static Guild GetOrCreateGuild(IGuild guild)
        {
            var guildResult = MongoService.GuildCollection.Find(x => x.GuildId == guild.Id);

            if (guildResult.Any())
                return guildResult.First();
            else
                return new Guild(guild);
        }

        private void SaveThis1()
            => MongoService.GuildCollection.InsertOne(this);

        [BsonIgnore]
        public List<Censor> Censors
            => Censor.GetCensors(this.GuildId);

        public Censor AddCensor(ulong guildid, string censorText)
            => new Censor(guildid, censorText);

        public long DelCensor(ObjectId _id)
        {
            var censors = Censors.Where(x => x._id == _id).Select(x => x._id);

            if (!censors.Any())
                return 0;

            var idsFilter = Builders<Censor>.Filter.In(d => d._id, censors);

            var result = MongoService.CensoredCollection.DeleteOne(idsFilter);

            return result.DeletedCount;
        }
    }


    public class Censor
    {
        public ObjectId _id { get; set; }
        public ulong GuildID { get; set; }

        public string CensorText { get; set; }

        public Censor() { } // Just used for mongo.

        public Censor(ulong guildid, string censorText)
        {
            this.GuildID = guildid;
            this.CensorText = censorText;
            SaveThis();
        }

        private void SaveThis()
            => MongoService.CensoredCollection.InsertOne(this);

        public static List<Censor> GetCensors(ulong guildid)
        {
            var censors = MongoService.CensoredCollection.Find(x => x.GuildID == guildid);

            if (censors.Any())
                return censors.ToList();
            else return new List<Censor>();
        }
    }
}
