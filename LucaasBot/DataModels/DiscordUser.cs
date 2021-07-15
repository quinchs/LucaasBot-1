using Discord;
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
    public class DiscordUser
    {
        [BsonIgnore]
        public const int MaxModlogs = 25;

        public ulong UserId { get; set; }
        public DiscordUser() { }

        public DiscordUser(IGuildUser user)
        {
            this.UserId = user.Id;
            SaveThis1();
        }

        public static DiscordUser GetOrCreateDiscordUser(IGuildUser user)
        {
            var userResult = MongoService.DiscordUserCollection.Find(x => x.UserId == user.Id);

            if (userResult.Any())
                return userResult.First();
            else
                return new DiscordUser(user);
        }

        private void SaveThis1()
            => MongoService.DiscordUserCollection.InsertOne(this);

        [BsonIgnore]
        public List<Modlogs> UserModlogs
            => Modlogs.GetModlogs(this.UserId);

        public Modlogs AddModlog(ulong modid, string reason, string action)
            => UserModlogs.Count >= MaxModlogs ? null : new Modlogs(modid, this, reason, action);

        public long DelModlog(ObjectId _id)
        {
            var logs = UserModlogs.Where(x => x._id == _id).Select(x => x._id);

            if (!logs.Any())
                return 0;

            var idsFilter = Builders<Modlogs>.Filter.In(d => d._id, logs);

            var result = MongoService.ModlogsCollection.DeleteOne(idsFilter);

            return result.DeletedCount;
        }
    }
}
