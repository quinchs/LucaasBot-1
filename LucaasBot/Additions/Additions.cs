using Discord;
using Discord.Rest;
using Discord.WebSocket;
using LucaasBot.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot
{
    public static class Additions
    {
        public static int GetHiearchy(this IGuildUser user)
        {
            if (user.Guild.OwnerId == user.Id)
                return int.MaxValue;

            var orderedRoles = user.Guild.Roles.OrderByDescending(x => x.Position);
            return orderedRoles.Where(x => user.RoleIds.Contains(x.Id)).Max(x => x.Position);
        }

        public static async Task<IMessage> SendErrorAsync(this ISocketMessageChannel channel, string description = null)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Command Error", "https://cdn.discordapp.com/emojis/312314733816709120.png?v=1")
                .WithDescription(description ?? "There was an error, check logs.")
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

        public static async Task<IMessage> SendInfractionAsync(this ISocketMessageChannel channel, IGuildUser userAccount, IGuildUser moderator, Modlogs log, bool gotDM)
        {
            var embed = new EmbedBuilder()
                .WithAuthor($"{userAccount} was {log.Action.Format()}", "https://cdn.discordapp.com/emojis/312314752711786497.png?v=1")
                .AddField("Moderator", moderator.Mention, true)
                .AddField("Reason", log.Reason, true)
                .WithColor(Color.Green)
                .Build();

            return await channel.SendMessageAsync(embed: embed);
        }

        public static async Task<IMessage> ModlogAsync(this ISocketMessageChannel channel, IGuildUser target, IGuildUser mod, Modlogs log, ISocketMessageChannel targetChannel, bool gotDM)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"{log.Action.Format()}")
                .WithDescription($"**Offender:** {target}\n**Reason:** {log.Reason}\n**Moderator:** {mod}\n**In:** <#{targetChannel.Id}>\n**Notified in DM's:** {gotDM}")
                .WithColor(log.Action.GetColor())
                .Build();

            return await channel.SendMessageAsync(embed: embed);
        }

        public static bool IsStaff(this IGuildUser user)
        {
            return user.RoleIds.Any(x => x == 563030072026595339 || x == 639547493767446538);
        }

        public static string Format(this ModlogAction action)
        {
            return action switch
            {
                ModlogAction.Ban => "Banned",
                ModlogAction.Kick => "Kicked",
                ModlogAction.Mute => "Muted",
                ModlogAction.Warn => "Warned",
                ModlogAction.Unmute => "Unmuted",
                _ => "Unknown action"
            };
        }

        public static Color GetColor(this ModlogAction action)
        {
            return action switch
            {
                ModlogAction.Warn => Color.Blue,
                ModlogAction.Kick => Color.Orange,
                ModlogAction.Ban => Color.Red,
                ModlogAction.Mute => Color.Magenta,
                ModlogAction.Unmute => Color.Purple,
                _ => Color.Red,
            };
        }

        public static MessageReference GetReference(this IMessage message)
        {
            if(message.Channel is IGuildChannel gc)
                return new MessageReference(message.Id, message.Channel.Id, gc.GuildId);

            return new MessageReference(message.Id, message.Channel.Id);
        }

        public static Severity ToLogSeverity(this LogSeverity sev)
        {
            return sev switch
            {
                LogSeverity.Critical => Severity.Critical,
                LogSeverity.Debug => Severity.Debug,
                LogSeverity.Error => Severity.Error,
                LogSeverity.Info => Severity.Log,
                LogSeverity.Verbose => Severity.Verbose,
                LogSeverity.Warning => Severity.Warning,
                _ => Severity.Log
            };
        }

        /// <summary>
        /// Converts the current string into a timespan.
        /// </summary>
        /// <param name="str">The string matching the below regex: <code>(?>(\d+?)([h|m|s|d]))</code></param>
        /// <returns>A timespan created from the string.</returns>
        public static TimeSpan? ToTimespan(this string str)
        {
            TimeSpan t = new TimeSpan(0);
            List<TimeSpan> spans = new List<TimeSpan>();

            var matches = Regex.Matches(str, @"(?>(\d+?)([h|m|s|d]))");

            foreach (Match match in matches)
            {
                switch (match.Groups[2].Value)
                {
                    case "h":
                        spans.Add(TimeSpan.FromHours(int.Parse(match.Groups[1].Value)));
                        break;
                    case "m":
                        spans.Add(TimeSpan.FromMinutes(int.Parse(match.Groups[1].Value)));
                        break;
                    case "s":
                        spans.Add(TimeSpan.FromSeconds(int.Parse(match.Groups[1].Value)));
                        break;
                    case "d":
                        spans.Add(TimeSpan.FromDays(int.Parse(match.Groups[1].Value)));
                        break;
                }
            }

            if (spans.Count == 0)
                return null;

            foreach (var ts in spans)
            {
                t = t.Add(ts);
            }

            return t;
        }
    }
}
