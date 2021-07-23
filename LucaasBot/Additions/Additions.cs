using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Additions
{
    public static class Additions
    {
        public static async Task<IMessage> SendErrorAsync(this ISocketMessageChannel channel, string description)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Command Error", "https://cdn.discordapp.com/emojis/312314733816709120.png?v=1")
                .WithDescription("There was an error, check logs.")
                .WithColor(Color.Red)
                .Build();

            return await channel.SendMessageAsync(embed: embed);
        }

        public static async Task<IMessage> SendSuccessAsync(this ISocketMessageChannel channel, string description, string footer = null)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Command Success", "https://cdn.discordapp.com/emojis/312314752711786497.png?v=1")
                .WithDescription(description)
                .WithColor(Color.Green);

            if (footer != null)
                embed.WithFooter(footer);

            return await channel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task<IMessage> SendInfractionAsync(this ISocketMessageChannel channel, string type, SocketGuildUser userAccount, SocketGuildUser moderator, string reason)
        {
            var embed = new EmbedBuilder()
                .WithAuthor($"{userAccount.Username} was {type}", "https://cdn.discordapp.com/emojis/312314752711786497.png?v=1")
                .AddField("Moderator", moderator.Mention, true)
                .AddField("Reason", reason, true)
                .WithColor(Color.Green)
                .Build();

            return await channel.SendMessageAsync(embed: embed);
        }

        public static async Task<IMessage> ModlogAsync(this ISocketMessageChannel channel1, string type, SocketGuildUser userAccount, string reason, SocketGuildUser moderator, ISocketMessageChannel channel)
        {
            var embed = new EmbedBuilder()
                .WithTitle(type)
                .WithDescription($"**Offender:** {userAccount}\n**Reason:** {reason}\n**Moderator:** {moderator}\n**In:** <#{channel.Id}>")
                .WithColor(Color.Red)
                .Build();

            return await channel.SendMessageAsync(embed: embed);
        }
    }
}
