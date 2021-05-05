using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Additions
{
    public static class Additions
    {
        public static string token = "ODM5MTI2MzMxNTcyNDg2MTg0.YJFHSw._ZiWn129zF1jV_Vv6xHWv70_KqM";
        public static string mongoCS = "mongodb://liege:LiegeData72@68.183.30.85";

        public static async Task<IMessage> SendErrorAsync(this ISocketMessageChannel channel, string description)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Command Error", "https://cdn.discordapp.com/emojis/787035973287542854.png?v=1")
                .WithDescription(description)
                .WithColor(Color.Red)
                .Build();
            var message = await channel.SendMessageAsync(embed: embed);
            return message;
        }

        public static async Task<IMessage> SendSuccessAsync(this ISocketMessageChannel channel, string description)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Command Success", "https://cdn.discordapp.com/emojis/787034785583333426.png?v=1")
                .WithDescription(description)
                .WithColor(Color.Green)
                .Build();
            var message = await channel.SendMessageAsync(embed: embed);
            return message;
        }

        public static async Task<IMessage> SendInfractionAsync(this ISocketMessageChannel channel, string type, SocketGuildUser userAccount, SocketGuildUser moderator, string reason)
        {
            var embed = new EmbedBuilder()
                .WithAuthor($"{userAccount.Username} was {type}", "https://cdn.discordapp.com/emojis/787034785583333426.png?v=1")
                .AddField("Moderator", moderator.Mention, true)
                .AddField("Reason", reason, true)
                .WithColor(Color.Green)
                .Build();
            var message = await channel.SendMessageAsync(embed: embed);
            return message;
        }

        public static async Task<IMessage> ModlogAsync(this ISocketMessageChannel channel1, string type, SocketGuildUser userAccount, string reason, SocketGuildUser moderator, ISocketMessageChannel channel)
        {
            var embed = new EmbedBuilder()
                .WithTitle(type)
                .WithDescription($"**Offender:** {userAccount.Mention}\n**Reason:** {reason}\n**Moderator:** {moderator.Mention}\n**In:** <#{channel.Id}>")
                .WithColor(Color.Red)
                .Build();
            var message = await channel1.SendMessageAsync(embed: embed);
            return message;
        }
    }
}