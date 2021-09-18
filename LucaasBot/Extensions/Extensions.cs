using Discord;
using Discord.Rest;
using Discord.WebSocket;
using LucaasBot.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot
{
    public static class Extensions
    {
        public static ApplicationCommandProperties ToProperties(this RestGuildCommand command)
        {
            switch (command.Type)
            {
                case ApplicationCommandType.Slash:
                    var props = new SlashCommandBuilder()
                    {
                        Name = command.Name,
                        DefaultPermission = command.DefaultPermission,
                        Description = command.Description,
                        Options = command.Options?.Any() ?? false ? command.Options.Select(x => new SlashCommandOptionBuilder()
                        {
                            Type = x.Type,
                            Required = x.Required ?? false,
                            Name = x.Name,
                            Default = x.Default,
                            Description = x.Description,
                            Choices = x.Choices?.Any() ?? false ? x.Choices.Select(x => new ApplicationCommandOptionChoiceProperties()
                            {
                                Name = x.Name,
                                Value = x.Value
                            }).ToList() : null,
                            Options = x.Options?.Any() ?? false ? x.Options.Select(x => ToProperties(x)).ToList() : null 
                        }).ToList() : null,
                    };

                    return props.Build();

                default:
                    return null;
            }
        }

        public static SlashCommandOptionBuilder ToProperties(RestApplicationCommandOption x)
        {
            return new SlashCommandOptionBuilder()
            {
                Type = x.Type,
                Required = x.Required ?? false,
                Name = x.Name,
                Default = x.Default,
                Description = x.Description,
                Choices = x.Choices?.Any() ?? false ? x.Choices.Select(y => new ApplicationCommandOptionChoiceProperties()
                {
                    Name = y.Name,
                    Value = y.Value
                }).ToList() : null,
                Options = x.Options?.Any() ?? false ? x.Options.Select(z => ToProperties(z)).ToList() : null
            };
        }

        public static EmbedBuilder AddPaginatedFooter(this EmbedBuilder embed, int curPage, int? lastPage)
        {
            if (lastPage != null)
                return embed.WithFooter(efb => efb.WithText($"{curPage + 1} / {lastPage + 1}"));
            else
                return embed.WithFooter(efb => efb.WithText(curPage.ToString()));
        }

        public static int GetHiearchy(this IGuildUser user)
        {
            if (user.Guild.OwnerId == user.Id)
                return int.MaxValue;

            var orderedRoles = user.Guild.Roles.OrderByDescending(x => x.Position);
            return orderedRoles.Where(x => user.RoleIds.Contains(x.Id)).Max(x => x.Position);
        }
        public static EmbedAuthorBuilder WithMusicIcon(this EmbedAuthorBuilder eab) =>
            eab.WithIconUrl("http://i.imgur.com/nhKS3PT.png");
        public static IMessage DeleteAfter(this IUserMessage msg, int seconds)
        {
            Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000).ConfigureAwait(false);
                try { await msg.DeleteAsync().ConfigureAwait(false); }
                catch { }
            });
            return msg;
        }

        public static async Task<IUserMessage> SendErrorAsync(this DualPurposeContext context, string description = null, bool ephemeral = false, MessageReference @ref = null, Action<EmbedBuilder> builder = null)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Error", "https://cdn.discordapp.com/emojis/312314733816709120.png?v=1")
                .WithDescription(description ?? "There was an error, check logs.")
                .WithColor(Color.Red);

            if (builder != null)
                builder(embed);

            return await context.ReplyAsync(embed: embed.Build(), ephemeral: ephemeral, messageReference: @ref);
        }
        public static async Task<IUserMessage> SendSuccessAsync(this DualPurposeContext context, string description, bool ephemeral = false, MessageReference @ref = null, string footer = null, Action<EmbedBuilder> builder = null)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Success", "https://cdn.discordapp.com/emojis/312314752711786497.png?v=1")
                .WithDescription(description)
                .WithColor(Color.Green);

            if (footer != null)
                embed.WithFooter(footer);

            if (builder != null)
                builder(embed);

            return await context.ReplyAsync(embed: embed.Build(), ephemeral: ephemeral, messageReference: @ref);
        }

        public static async Task<IUserMessage> SendErrorAsync(this IMessageChannel channel, string description = null, MessageReference @ref = null)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Error", "https://cdn.discordapp.com/emojis/312314733816709120.png?v=1")
                .WithDescription(description ?? "There was an error, check logs.")
                .WithColor(Color.Red)
                .Build();

            return await channel.SendMessageAsync(embed: embed, messageReference: @ref);
        }

        public static async Task<IUserMessage> SendSuccessAsync(this IMessageChannel channel, string description, string footer = null)
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Success", "https://cdn.discordapp.com/emojis/312314752711786497.png?v=1")
                .WithDescription(description)
                .WithColor(Color.Green);

            if (footer != null)
                embed.WithFooter(footer);

            return await channel.SendMessageAsync(embed: embed.Build());
        }

        public static async Task<IUserMessage> SendInfractionAsync(this IMessageChannel channel, IGuildUser userAccount, IGuildUser moderator, Modlogs log, bool gotDM)
        {
            var embed = new EmbedBuilder()
                .WithAuthor($"{userAccount} was {log.Action.Format()}", "https://cdn.discordapp.com/emojis/312314752711786497.png?v=1")
                .AddField("Moderator", moderator.Mention, true)
                .AddField("Reason", log.Reason, true)
                .WithColor(Color.Green)
                .Build();

            return await channel.SendMessageAsync(embed: embed);
        }

        public static async Task<IUserMessage> ModlogAsync(this IMessageChannel channel, IGuildUser target, IGuildUser mod, Modlogs log, IMessageChannel targetChannel, bool gotDM)
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

        /// <summary>
        /// returns an IEnumerable with randomized element order
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items)
        {
            using (var provider = RandomNumberGenerator.Create())
            {
                var list = items.ToList();
                var n = list.Count;
                while (n > 1)
                {
                    var box = new byte[(n / Byte.MaxValue) + 1];
                    int boxSum;
                    do
                    {
                        provider.GetBytes(box);
                        boxSum = box.Sum(b => b);
                    }
                    while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                    var k = (boxSum % n);
                    n--;
                    var value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
                return list;
            }
        }
    }
}
