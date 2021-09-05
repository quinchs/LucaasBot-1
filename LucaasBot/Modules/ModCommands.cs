using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LucaasBot;
using MongoDB.Bson;
using LucaasBot.DataModels;
using System.Globalization;
using LucaasBot.Handlers;

namespace LucaasBotBeta.Modules
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        [Command("delmsg")]
        public async Task DelMsg(ulong id)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = Context.Guild.GetRole(563030072026595339);
            var devRole = Context.Guild.GetRole(639547493767446538);

            //if (!user.GuildPermissions.KickMembers)
            if (!user.Roles.Contains(staffRole) && !user.Roles.Contains(devRole))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            var msg = await Context.Channel.GetMessageAsync(id);
            await msg.DeleteAsync();
            await ReplyAsync("Done");
        }

        private ModlogHandler ModlogHandler
            => HandlerService.GetHandlerInstance<ModlogHandler>();

        [Command("warn")]
        public async Task Warn(IGuildUser userAccount = null, [Remainder] string reason = null)
            => ModlogHandler.HandleModCommand(Context, ModlogAction.Warn, userAccount, reason);

        [Command("mute")]
        [Alias("m")]
        public async Task Mute(IGuildUser userAccount = null, string time = null, [Remainder] string reason = null)
        {
            var timespan = time.ToTimespan();

            if (!timespan.HasValue)
            {
                await Context.Channel.SendErrorAsync($"The timespan \"{time}\" is not valid!");
                return;
            }

            ModlogHandler.HandleModCommand(Context, ModlogAction.Mute, userAccount, reason, timespan);
        }

        [Command("unmute")]
        [Alias("um")]
        public async Task Unmute(IGuildUser userAccount = null, [Remainder] string reason = "Normal unmute")
            => ModlogHandler.HandleModCommand(Context, ModlogAction.Unmute, userAccount, reason);

        [Command("kick")]
        [Alias("k")]
        public async Task Kick(IGuildUser userAccount = null, [Remainder] string reason = null)
            => ModlogHandler.HandleModCommand(Context, ModlogAction.Kick, userAccount, reason);

        [Command("ban")]
        [Alias("b")]
        public async Task Ban(IGuildUser userAccount = null, [Remainder] string reason = null)
            => ModlogHandler.HandleModCommand(Context, ModlogAction.Ban, userAccount, reason);

        [Command("aban")]
        [Alias("ab")]
        public async Task AppealBan(IGuildUser userAccount = null, [Remainder] string reason = null)
            => ModlogHandler.HandleModCommand(Context, ModlogAction.Ban, userAccount, reason);

        [Command("modlogs")]
        public async Task Modlogs(IGuildUser userAccount = null)
        {
            if (Context.User is not SocketGuildUser user)
            {
                return;
            }

            if (!user.IsStaff())
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            var discordUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
            var orderedLogs = discordUser.UserModlogs.OrderBy(x => x.DateCreated).ToArray();

            var embed = new EmbedBuilder();
            embed.WithColor(Color.Green);
            embed.WithCurrentTimestamp();
            embed.WithAuthor(new EmbedAuthorBuilder()
            {
                Name = $"{userAccount.Username}'s Modlogs",
                IconUrl = userAccount.GetAvatarUrl()
            });

            if (orderedLogs.Any())
            {
                if (orderedLogs.Count() <= 25)
                {
                    for (int n = 0; n != orderedLogs.Count(); n++)
                    {
                        var logs = orderedLogs[n];

                        embed.AddField($"{n + 1} | {logs.Action}", $"**Reason:** {logs.Reason}\n**Moderator:** <@{logs.ModID}>\n**Date:** {logs.DateCreated}");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This user has over 25 modlogs!");
                }

            }
            else
            {
                embed.WithDescription("This user has no modlogs!");
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("clearlogs")]
        public async Task Clearlogs(IGuildUser userAccount = null, uint logNum = 0)
        {
            if(Context.User is not SocketGuildUser user)
            {
                return;
            }

            if (!user.IsStaff())
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            var discordUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
            var orderedModlogs = discordUser.UserModlogs.OrderBy(x => x.DateCreated).ToArray();

            var embed = new EmbedBuilder();
            embed.WithColor(Color.Green);
            embed.WithCurrentTimestamp();
            embed.WithAuthor(new EmbedAuthorBuilder()
            {
                Name = $"{userAccount.Username}'s Modlogs",
                IconUrl = userAccount.GetAvatarUrl()
            });

            if (orderedModlogs.Any())
            {
                if(orderedModlogs.Length >= logNum)
                {
                    var modlogs = orderedModlogs[logNum - 1];
                    discordUser.DelModlog(modlogs._id);
                    embed.WithDescription($"Removed log: `{logNum}`!");
                }
                else
                {
                    await Context.Channel.SendErrorAsync($"This user does not have a log number {logNum}!");
                    return;
                }
            }
            else
            {
                embed.WithDescription("This user has no modlogs!");
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("slowmode")]
        [Alias("s")]
        public async Task Slowmode(int value = 999)
        {
            if(Context.User is not SocketGuildUser user)
            {
                return;
            }

            if (!user.IsStaff())
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (value == 999)
            {
                await Context.Channel.SendErrorAsync("Please enter a valid number!");
                return;
            }

            var channel = Context.Guild.GetTextChannel(Context.Channel.Id);
            await channel.ModifyAsync(x =>
            {
                x.SlowModeInterval = value;
            });

            await Context.Channel.SendSuccessAsync($"Set the slowmode to `{value}`!");
            return;
        }


        [Command("modlevel"), Alias("verification", "verificationlevel", "vl")]
        public async Task ChangeModLevel(string level = null)
        {
            if(Context.User is not SocketGuildUser user)
            {
                return;
            }

            if (!user.IsStaff())
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (string.IsNullOrEmpty(level))
            {
                await Context.Channel.SendErrorAsync("Please provide a verification level!");
                return;
            }

            // uppercases the first letter.
            level = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(level.ToLower());

            if(!Enum.TryParse(level, out VerificationLevel result))
            {
                await Context.Channel.SendErrorAsync($"Unknown verification level \"{level}\"! the possible options are:\n```\n{string.Join("\n", Enum.GetNames(typeof(VerificationLevel)))}```");
                return;
            }

            await Context.Guild.ModifyAsync(x => x.VerificationLevel = result);
            await Context.Channel.SendSuccessAsync($"Changed verification level to `{result}`!");
        }


        [Command("modstats")]
        public async Task ModStats(IGuildUser userAccount = null)
        {
            if (Context.User is not SocketGuildUser guildUser)
            {
                return;
            }

            if (!guildUser.IsStaff())
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("That user could not be found!");
                return;
            }

            if (!userAccount.IsStaff())
            {
                await Context.Channel.SendErrorAsync("That user isn't a staff member!");
                return;
            }

            // get the mod logs this staff has created

            var logs = (await MongoService.ModlogsCollection.FindAsync(x => x.ModID == userAccount.Id)).ToList();

            var warns = logs.Count(x => x.Action == ModlogAction.Warn);
            var mutes = logs.Count(x => x.Action == ModlogAction.Mute);
            var kicks = logs.Count(x => x.Action == ModlogAction.Kick);
            var bans = logs.Count(x => x.Action == ModlogAction.Ban);
            var unmutes = logs.Count(x => x.Action == ModlogAction.Unmute);

            var warns7 = logs.Count(x => x.Action == ModlogAction.Warn && (DateTime.UtcNow - x.DateCreated).TotalDays <= 7);
            var mutes7 = logs.Count(x => x.Action == ModlogAction.Mute && (DateTime.UtcNow - x.DateCreated).TotalDays <= 7);
            var kicks7 = logs.Count(x => x.Action == ModlogAction.Kick && (DateTime.UtcNow - x.DateCreated).TotalDays <= 7);
            var bans7 = logs.Count(x => x.Action == ModlogAction.Ban && (DateTime.UtcNow - x.DateCreated).TotalDays <= 7);
            var unmutes7 = logs.Count(x => x.Action == ModlogAction.Unmute && (DateTime.UtcNow - x.DateCreated).TotalDays <= 7);

            var warns30 = logs.Count(x => x.Action == ModlogAction.Warn && (DateTime.UtcNow - x.DateCreated).TotalDays <= 30);
            var mutes30 = logs.Count(x => x.Action == ModlogAction.Mute && (DateTime.UtcNow - x.DateCreated).TotalDays <= 30);
            var kicks30 = logs.Count(x => x.Action == ModlogAction.Kick && (DateTime.UtcNow - x.DateCreated).TotalDays <= 30);
            var bans30 = logs.Count(x => x.Action == ModlogAction.Ban && (DateTime.UtcNow - x.DateCreated).TotalDays <= 30);
            var unmutes30 = logs.Count(x => x.Action == ModlogAction.Unmute && (DateTime.UtcNow - x.DateCreated).TotalDays <= 30);

            var embed = new EmbedBuilder()
                .WithAuthor(userAccount)
                .WithTitle($"{userAccount}'s mod stats")
                .AddField("Total Warns", warns, true)
                .AddField("Warns (last 7 days)", warns7, true)
                .AddField("Warns (last 30 days)", warns30, true)
                .AddField("Total Kicks", kicks, true)
                .AddField("Kicks (last 7 days)", kicks7, true)
                .AddField("Kicks (last 30 days)", kicks30, true)
                .AddField("Total Bans", bans, true)
                .AddField("Bans (last 7 days)", bans7, true)
                .AddField("Bans (last 30 days)", bans30, true)
                .AddField("Total Mutes", mutes, true)
                .AddField("Mutes (last 7 days)", mutes7, true)
                .AddField("Mutes (last 30 days)", mutes30, true)
                .AddField("Total Unmutes", unmutes, true)
                .AddField("Unmutes (last 7 days)", unmutes7, true)
                .AddField("Unmutes (last 30 days)", unmutes30, true)
                .WithFooter($"Total actions: {logs.Count}")
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: embed, messageReference: Context.Message.GetReference());
        }

        //[Command("move")]
        public async Task MoveUserVoice(SocketGuildUser userAccount = null, SocketVoiceChannel channel = null)
        {
            if(Context.User is not SocketGuildUser user)
            {
                return;
            }

            if (!user.IsStaff())
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            await userAccount.ModifyAsync(x =>
            {
                x.Channel = channel;
            });
        }


        [Command("testing")]
        public async Task testing()
        {
            var users = Context.Guild.Users;
            var partnered = users.Where(x => x.PublicFlags.HasValue && ((ulong)x.PublicFlags.Value & (ulong)UserProperties.Partner) != 0).ToList();

            foreach (var user in partnered)
            {
                await ReplyAsync(user.Mention);
            }
        }

        [Command("lock")]
        public async Task LockChannel(SocketTextChannel channel = null)
        {
            var user = Context.User as SocketGuildUser;

        }
    }
}
