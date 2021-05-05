using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using static LucaasBotBeta.Handlers.UserHandler;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Additions;
using MongoDB.Bson;

namespace LucaasBotBeta.Modules
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        [Command("warn")]
        public async Task Warn(SocketGuildUser userAccount = null, [Remainder]string reason = null)
        {
            var user = Context.User as SocketGuildUser;

            if (!user.Roles.Any(x => x.Permissions.ManageMessages))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            if (user.Hierarchy <= userAccount.Hierarchy)
            {
                await Context.Channel.SendErrorAsync("You cannot warn this user!");
                return;
            }

            if (reason == null)
            {
                await Context.Channel.SendErrorAsync("Please provide a reason!");
                return;
            }

            var getUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
            var userObj = getUser.AddModlog(Context.User.Id, reason, "Warned");

            await Context.Channel.SendInfractionAsync("Warned", userAccount, user, reason);

            var warnMsg = new EmbedBuilder();
            warnMsg.WithTitle($"You were warned in `{Context.Guild}`");
            warnMsg.AddField($"Reason", reason, true);
            warnMsg.WithFooter($"Warned by {Context.Message.Author.Username}");
            warnMsg.WithCurrentTimestamp();
            await userAccount.SendMessageAsync("", false, warnMsg.Build());
        }

        [Command("mute")]
        [Alias("m")]
        public async Task Mute(SocketGuildUser userAccount = null, string time = null, [Remainder] string reason = null)
        {
            var user = Context.User as SocketGuildUser;
            var muteRole = Context.Guild.GetRole(0123456789);

            if (!user.Roles.Any(x => x.Permissions.ManageMessages))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            if (user.Hierarchy <= userAccount.Hierarchy)
            {
                await Context.Channel.SendErrorAsync("You cannot mute this user!");
                return;
            }

            if (time == null)
            {
                await Context.Channel.SendErrorAsync("Please provide a time!");
                return;
            }

            if (reason == null)
            {
                await Context.Channel.SendErrorAsync("Please provide a reason!");
                return;
            }

            if (userAccount.Roles.Contains(muteRole))
            {
                await Context.Channel.SendErrorAsync("This user is already muted!");
                return;
            }

            string adjustedTime = time.Remove(time.Length - 1, 1);
            int timeInt = int.Parse(adjustedTime);
            var mutes = UserMutes.GetOrCreateMute(userAccount.Id);

            if (time.EndsWith("s"))
            {
                mutes.DateTime = DateTime.UtcNow;
                mutes.Time = timeInt;
                mutes.Type = "s";
                mutes.SaveThis();
            }

            if (time.EndsWith("m"))
            {
                mutes.DateTime = DateTime.UtcNow;
                mutes.Time = timeInt;
                mutes.Type = "m";
                mutes.SaveThis();
            }

            if (time.EndsWith("h"))
            {
                mutes.DateTime = DateTime.UtcNow;
                mutes.Time = timeInt;
                mutes.Type = "m";
                mutes.SaveThis();
            }

            await userAccount.AddRoleAsync(muteRole);
            var getUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
            var userObj = getUser.AddModlog(Context.User.Id, reason, "Muted");
            await Context.Channel.SendInfractionAsync("Muted", userAccount, user, reason);

            var muteMsg = new EmbedBuilder();
            muteMsg.WithTitle($"You were muted in `{Context.Guild}`");
            muteMsg.AddField($"Reason", reason, true);
            muteMsg.WithFooter($"Muted by {Context.Message.Author.ToString()}");
            muteMsg.WithCurrentTimestamp();
            await userAccount.SendMessageAsync("", false, muteMsg.Build());          
        }

        [Command("unmute")]
        [Alias("um")]
        public async Task Unmute(SocketGuildUser userAccount = null)
        {
            var user = Context.User as SocketGuildUser;
            var muteRole = Context.Guild.GetRole(0123456789);

            if (!user.Roles.Any(x => x.Permissions.ManageMessages))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            if (user.Hierarchy <= userAccount.Hierarchy)
            {
                await Context.Channel.SendErrorAsync("You cannot unmute this user!");
                return;
            }

            if (!userAccount.Roles.Contains(muteRole))
            {
                await Context.Channel.SendErrorAsync("This user is aleady unmuted!");
                return;
            }

            await userAccount.RemoveRoleAsync(muteRole);

            var mute = new EmbedBuilder();
            mute.WithTitle($"{userAccount.Username} has been unmuted.");
            mute.AddField("Moderator", user.Mention);
            mute.WithColor(Color.Green);
            mute.WithCurrentTimestamp();
            await ReplyAsync("", false, mute.Build());

            var muteMsg = new EmbedBuilder();
            muteMsg.WithTitle($"You were unmuted in `{Context.Guild}`");
            muteMsg.WithFooter($"Unmuted by {Context.Message.Author.Username}");
            muteMsg.WithCurrentTimestamp();
            await userAccount.SendMessageAsync("", false, muteMsg.Build());         
        }

        [Command("kick")]
        [Alias("k")]
        public async Task Kick(SocketGuildUser userAccount = null, [Remainder] string reason = null)
        {
            var user = Context.User as SocketGuildUser;

            if (!user.Roles.Any(x => x.Permissions.ManageMessages))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            if (user.Hierarchy <= userAccount.Hierarchy)
            {
                await Context.Channel.SendErrorAsync("You cannot warn this user!");
                return;
            }

            if (reason == null)
            {
                await Context.Channel.SendErrorAsync("Please provide a reason!");
                return;
            }

            if (user.Id == userAccount.Id)
            {
                await Context.Channel.SendErrorAsync("You cannot kick youself!");
                return;
            }

            var getUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
            var userObj = getUser.AddModlog(Context.User.Id, reason, "Kicked");

            var kickMsg = new EmbedBuilder();
            kickMsg.WithTitle($"You were kicked from `{Context.Guild}`");
            kickMsg.AddField($"Reason", reason);
            kickMsg.WithFooter($"Kicked by {user.Username}");
            kickMsg.WithCurrentTimestamp();
            await userAccount.SendMessageAsync($"", false, kickMsg.Build());

            await Context.Channel.SendInfractionAsync("Kicked", userAccount, user, reason);

            await userAccount.KickAsync(reason);
        }

        [Command("ban")]
        [Alias("b")]
        public async Task Ban(SocketGuildUser userAccount = null, [Remainder] string reason = null)
        {
            var user = Context.User as SocketGuildUser;

            if (!user.Roles.Any(x => x.Permissions.ManageMessages))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            if (user.Hierarchy <= userAccount.Hierarchy)
            {
                await Context.Channel.SendErrorAsync("You cannot ban this user!");
                return;
            }

            if (reason == null)
            {
                await Context.Channel.SendErrorAsync("Please provide a reason!");
                return;
            }

            if (user.Id == userAccount.Id)
            {
                await Context.Channel.SendErrorAsync("You cannot ban youself!");
                return;
            }

            var getUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
            var userObj = getUser.AddModlog(Context.User.Id, reason, "Banned");

            var banMsg = new EmbedBuilder();
            banMsg.WithTitle($"You were banned from `{Context.Guild}`");
            banMsg.AddField($"Reason", reason);
            banMsg.WithFooter($"Banned by {Context.Message.Author.Username}");
            banMsg.WithCurrentTimestamp();
            await userAccount.SendMessageAsync("", false, banMsg.Build());

            await Context.Channel.SendInfractionAsync("Banned", userAccount, user, reason);

            await Context.Guild.AddBanAsync(userAccount, 7, reason);

        }

        [Command("modlogs")]
        public async Task Modlogs(SocketGuildUser userAccount = null)
        {
            var user = Context.User as SocketGuildUser;

            if (!user.Roles.Any(x => x.Permissions.ManageMessages))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            var getUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
            var getModlogs = getUser.UserModlogs.OrderBy(x => x.DateCreated).ToArray();

            var embed = new EmbedBuilder();
            embed.WithColor(Color.Green);
            embed.WithCurrentTimestamp();
            embed.WithAuthor(new EmbedAuthorBuilder()
            {
                Name = $"{userAccount.Username}'s Modlogs",
                IconUrl = userAccount.GetAvatarUrl()
            });

            if (getModlogs.Any())
            {
                if (getModlogs.Count() <= 25)
                {
                    for (int n = 0; n != getModlogs.Count(); n++)
                    {
                        var logs = getModlogs[n];

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

        static MongoClient Client = new MongoClient(Additions.Additions.mongoCS);

        static IMongoDatabase Database
            => Client.GetDatabase("LucaasBot");

        static IMongoCollection<Modlogs> ModlogsCollection
            => Database.GetCollection<Modlogs>("modlogs");

        [Command("clearlogs")]
        public async Task Clearlogs(SocketGuildUser userAccount = null, int logNum = 0)
        {
            var user = Context.User as SocketGuildUser;

            if (!user.Roles.Any(x => x.Permissions.ManageMessages))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (userAccount == null)
            {
                await Context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            if (logNum == 999)
            {
                await Context.Channel.SendErrorAsync("Please provide a number!");
                return;
            }

            var getUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
            var getModlogs = getUser.UserModlogs.OrderBy(x => x.DateCreated).ToArray();

            var embed = new EmbedBuilder();
            embed.WithColor(Color.Green);
            embed.WithCurrentTimestamp();
            embed.WithAuthor(new EmbedAuthorBuilder()
            {
                Name = $"{userAccount.Username}'s Modlogs",
                IconUrl = userAccount.GetAvatarUrl()
            });

            if (getModlogs.Any())
            {
                if (getModlogs.Count() <= 25)
                {
                    var modlogs = getModlogs[logNum - 1];
                    getUser.DelModlog(modlogs._id);
                    embed.WithDescription($"Removed log: `{logNum}`!");
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
    }
}
