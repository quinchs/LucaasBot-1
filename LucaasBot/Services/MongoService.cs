using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using LucaasBot.DataModels;

namespace LucaasBot
{
    public class MongoService
    {
        public static MongoClient Client = new MongoClient(ConfigService.Config.MongoCS);

        public static IMongoDatabase Database
            => Client.GetDatabase("LucaasBot");

        public static IMongoCollection<DiscordUser> DiscordUserCollection
           => Database.GetCollection<DiscordUser>("discord-users");
        public static IMongoCollection<Modlogs> ModlogsCollection
            => Database.GetCollection<Modlogs>("modlogs");
        public static IMongoCollection<UserMutes> MutesCollection
            => Database.GetCollection<UserMutes>("mute-times");
        public static IMongoCollection<Censor> CensoredCollection
            => Database.GetCollection<Censor>("censors");
    }
}
