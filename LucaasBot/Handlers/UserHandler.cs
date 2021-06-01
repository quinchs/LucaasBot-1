using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;

namespace LucaasBotBeta.Handlers
{
    public class UserHandler
    {
        static MongoClient Client = new MongoClient(Additions.Additions.mongoCS);

        static IMongoDatabase Database
            => Client.GetDatabase("LucaasBot");

        static IMongoCollection<DiscordUser> DiscordUserCollection
           => Database.GetCollection<DiscordUser>("discord-users");
        static IMongoCollection<Modlogs> ModlogsCollection
            => Database.GetCollection<Modlogs>("modlogs");
        static IMongoCollection<UserMutes> MutesCollection
            => Database.GetCollection<UserMutes>("mute-times");
        static IMongoCollection<Censor> CensoredCollection
            => Database.GetCollection<Censor>("censors");

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
                var userResult = DiscordUserCollection.Find(x => x.UserId == user.Id);

                if (userResult.Any())
                    return userResult.First();
                else
                    return new DiscordUser(user);
            }

            private void SaveThis1()
                => DiscordUserCollection.InsertOne(this);

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

                var result = ModlogsCollection.DeleteOne(idsFilter);

                return result.DeletedCount;
            }
        }

        //[BsonIgnoreExtraElements]
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
                => ModlogsCollection.InsertOne(this);

            public static List<Modlogs> GetModlogs(ulong userAccountId)
            {
                var modlogs = ModlogsCollection.Find(x => x.UserID == userAccountId);

                if (modlogs.Any())
                    return modlogs.ToList();
                else return new List<Modlogs>();
            }

        }

        [BsonIgnoreExtraElements]
        public class UserMutes
        {
            public ulong UserID { get; set; }
            public DateTime DateTime { get; set; }
            public string Type { get; set; }
            public int Time { get; set; }


            public static UserMutes GetMute(ulong userid)
            {
                var result = MutesCollection.Find(x => x.UserID == userid);

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
                MutesCollection.ReplaceOne(x => x.UserID == this.UserID, this, new ReplaceOptions() { IsUpsert = true });
            }
        }

        [BsonIgnoreExtraElements]
        public class Censor
        {
            public ulong GuildID { get; set; }
            public string Phrase { get; set; }

            public static Censor GetCensors(ulong guildid)
            {
                var result = CensoredCollection.Find(x => x.GuildID == guildid);

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
                CensoredCollection.ReplaceOne(x => x.GuildID == this.GuildID, this, new ReplaceOptions() { IsUpsert = true });
            }
        }
    }
}
