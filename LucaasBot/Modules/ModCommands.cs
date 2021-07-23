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
using Additions;
using MongoDB.Bson;
using LucaasBot.DataModels;

namespace LucaasBot.Modules
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        [Command("warn")]
        public async Task Warn(SocketGuildUser userAccount = null, [Remainder] string reason = null)
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

            var modlogs = Context.Guild.GetTextChannel(663060075740659715);
            await modlogs.ModlogAsync("Warning", userAccount, reason, user, Context.Channel);

            var warnMsg = new EmbedBuilder();
            warnMsg.WithTitle($"You were warned in `{Context.Guild}`");
            warnMsg.AddField($"Reason", reason, true);
            warnMsg.WithFooter($"Warned by {Context.Message.Author.Username}");
            warnMsg.WithCurrentTimestamp();
            try
            {
                await userAccount.SendMessageAsync("", false, warnMsg.Build());
            }
            catch
            {
                await ReplyAsync("Was unable to inform this user of their infraction.");
            }
        }

        [Command("mute")]
        [Alias("m")]
        public async Task Mute(SocketGuildUser userAccount = null, string time = null, [Remainder] string reason = null)
        {
            var user = Context.User as SocketGuildUser;
            var muteRole = Context.Guild.GetRole(465097693379690497);
            var staffRole = Context.Guild.GetRole(563030072026595339);
            var devRole = Context.Guild.GetRole(639547493767446538);

            //if (!user.GuildPermissions.KickMembers)
            if (!user.Roles.Contains(staffRole) && !user.Roles.Contains(devRole))
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

            var modlogs = Context.Guild.GetTextChannel(663060075740659715);
            await modlogs.ModlogAsync("Mute", userAccount, reason, user, Context.Channel);

            var muteMsg = new EmbedBuilder();
            muteMsg.WithTitle($"You were muted in `{Context.Guild}`");
            muteMsg.AddField($"Reason", reason, true);
            muteMsg.WithFooter($"Muted by {Context.Message.Author.ToString()}");
            muteMsg.WithCurrentTimestamp();
            try
            {
                await userAccount.SendMessageAsync("", false, muteMsg.Build());
            }
            catch
            {
                await ReplyAsync("Was unable to inform this user of their infraction.");
            }
        }

        [Command("unmute")]
        [Alias("um")]
        public async Task Unmute(SocketGuildUser userAccount = null)
        {
            var user = Context.User as SocketGuildUser;
            var muteRole = Context.Guild.GetRole(465097693379690497);
            var staffRole = Context.Guild.GetRole(563030072026595339);
            var devRole = Context.Guild.GetRole(639547493767446538);

            if (userAccount.Id == 619241308912877609)
            {
                await userAccount.RemoveRoleAsync(muteRole);
                await Context.Message.DeleteAsync();
                return;
            }

            //if (!user.GuildPermissions.KickMembers)
            if (!user.Roles.Contains(staffRole) && !user.Roles.Contains(devRole))
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

            var modlogs = Context.Guild.GetTextChannel(663060075740659715);
            await modlogs.ModlogAsync("Unmute", userAccount, "None" , user, Context.Channel);

            var muteMsg = new EmbedBuilder();
            muteMsg.WithTitle($"You were unmuted in `{Context.Guild}`");
            muteMsg.WithFooter($"Unmuted by {Context.Message.Author.Username}");
            muteMsg.WithCurrentTimestamp();
            try
            {
                await userAccount.SendMessageAsync("", false, muteMsg.Build());
            }
            catch
            {
                await ReplyAsync("Was unable to inform this user of their infraction.");
            }
        }

        [Command("kick")]
        [Alias("k")]
        public async Task Kick(SocketGuildUser userAccount = null, [Remainder] string reason = null)
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

            var modlogs = Context.Guild.GetTextChannel(663060075740659715);
            await modlogs.ModlogAsync("Kick", userAccount, reason, user, Context.Channel);

            var kickMsg = new EmbedBuilder();
            kickMsg.WithTitle($"You were kicked from `{Context.Guild}`");
            kickMsg.AddField($"Reason", reason);
            kickMsg.WithFooter($"Kicked by {user.Username}");
            kickMsg.WithCurrentTimestamp();
            try
            {
                await userAccount.SendMessageAsync("", false, kickMsg.Build());
            }
            catch
            {
                await ReplyAsync("Was unable to inform this user of their infraction.");
            }

            await Context.Channel.SendInfractionAsync("Kicked", userAccount, user, reason);

            await userAccount.KickAsync(reason);
        }

        [Command("ban")]
        [Alias("b")]
        public async Task Ban(SocketGuildUser userAccount = null, [Remainder] string reason = null)
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

            var modlogs = Context.Guild.GetTextChannel(663060075740659715);
            await modlogs.ModlogAsync("Ban", userAccount, reason, user, Context.Channel);

            var banMsg = new EmbedBuilder();
            banMsg.WithTitle($"You were banned from `{Context.Guild}`");
            banMsg.AddField($"Reason", reason);
            banMsg.WithFooter($"Banned by {Context.Message.Author.Username}");
            banMsg.WithCurrentTimestamp();
            try
            {
                await userAccount.SendMessageAsync("", false, banMsg.Build());
            }
            catch
            {
                await ReplyAsync("Was unable to inform this user of their infraction.");
            }

            await Context.Channel.SendInfractionAsync("Banned", userAccount, user, reason);

            await Context.Guild.AddBanAsync(userAccount, 7, reason);

        }

        [Command("modlogs")]
        public async Task Modlogs(SocketGuildUser userAccount = null)
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

        [Command("clearlogs")]
        public async Task Clearlogs(SocketGuildUser userAccount = null, int logNum = 0)
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

        //[Command("censor")]
        //public async Task CensorCommand(string type = null, [Remainder]string text = null)
        //{
        //    var user = Context.User as SocketGuildUser;
        //    var getCensors = Censor.GetCensors(Context.Guild.Id);

        //    if (!user.GuildPermissions.KickMembers)
        //    {
        //        await Context.Channel.SendErrorAsync("You do not have access to use this command!");
        //        return;
        //    }

        //    if (type == null)
        //    {
        //        await Context.Channel.SendErrorAsync("Would you like to `add` or `remove` a phrase?");
        //        return;
        //    }

        //    if (type == null)
        //    {
        //        await Context.Channel.SendErrorAsync("Please provide the phrase you would like to add or remove!");
        //        return;
        //    }

        //    switch (type.ToLower())
        //    {
        //        case "add":
        //            getCensors.Phrase = text;
        //            break;

        //        case "remove":
        //            break;

        //        default:
        //            break;
        //    }
        //}

        [Command("terminate")]
        public async Task TerminateClient()
        {
            var devRole = Context.Guild.GetRole(639547493767446538);
            var user = Context.User as SocketGuildUser;

            if (user.Roles.Contains(devRole) || user.GuildPermissions.Administrator || user.Id == 619241308912877609)
            {
                await Context.Client.LogoutAsync();
            }
        }

        [Command("restart")]
        public async Task restart()
        {
            var devRole = Context.Guild.GetRole(639547493767446538);
            var user = Context.User as SocketGuildUser;

            if (user.Roles.Contains(devRole) || user.GuildPermissions.Administrator || user.Id == 619241308912877609)
            {
                try
                {
                    await Context.Client.LogoutAsync();
                    await Context.Client.StopAsync();

                    await Task.Delay(2000);

                    await Context.Client.LoginAsync(TokenType.Bot, ConfigService.Config.Token);
                    await Context.Client.StartAsync();
                }
                catch
                {
                    await Context.Channel.SendMessageAsync("Was unable to restart...");
                }

            }
        }

        [Command("ping")]
        public async Task ping()
        {
            var ping = new EmbedBuilder();
            ping.WithTitle("Pinging...");
            ping.WithColor(Color.Blue);
            var msg = await ReplyAsync("", false, ping.Build());
            await msg.ModifyAsync(x => x.Embed = new EmbedBuilder()
            {
                Description = $"🏓 Pong: `{Context.Client.Latency}`ms!\n[Check Discord Status](https://status.discord.com)",
                Color = Color.Blue,
            }.Build());
        }

        [Command("slowmode")]
        [Alias("s")]
        public async Task Slowmode(int value = 999)
        {
            var user = Context.User as SocketGuildUser;
            var roleStaff = Context.Guild.GetRole(563030072026595339);
            var devRole = Context.Guild.GetRole(639547493767446538);

            if (!user.Roles.Contains(roleStaff) && !user.Roles.Contains(devRole))
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

        //[Command("reason")]
        //public async Task ChangeReason(SocketGuildUser userAccount = null, int logNum = 0, [Remainder] string reason = null)
        //{
        //    var user = Context.User as SocketGuildUser;
        //    var staffRole = Context.Guild.GetRole(563030072026595339);

        //    //if (!user.GuildPermissions.KickMembers)
        //    //if (!user.Roles.Contains(staffRole))
        //    //{
        //    //    await Context.Channel.SendErrorAsync("You do not have access to use this command!");
        //    //    return;
        //    //}

        //    if (userAccount == null)
        //    {
        //        await Context.Channel.SendErrorAsync("Please mention a user!");
        //        return;
        //    }

        //    if (logNum == 999)
        //    {
        //        await Context.Channel.SendErrorAsync("Please provide a number!");
        //        return;
        //    }

        //    if (reason == null)
        //    {
        //        await Context.Channel.SendErrorAsync("Please provide a reason!");
        //        return;
        //    }

        //    var getUser = DiscordUser.GetOrCreateDiscordUser(userAccount);
        //    var getModlogs = getUser.UserModlogs.OrderBy(x => x.DateCreated).ToArray();

        //    //var embed = new EmbedBuilder();
        //    //embed.WithColor(Color.Green);
        //    //embed.WithCurrentTimestamp();
         
        //    if (getModlogs.Any())
        //    {
        //        if (getModlogs.Count() <= 25)
        //        {
        //            //if (getModlogs.Count() < logNum)
        //            //{
        //            //    await Context.Channel.SendErrorAsync("Please enter a valid number!");
        //            //    return;
        //            //}

        //            var logs = getModlogs[logNum - 1];
        //            logs.Reason = reason;

        //            var filter = ModlogsCollection.Find(x => x._id == logs._id);

        //            ModlogsCollection.FindOneAndReplace(filter, )
        //            //await ReplyAsync(logs.Reason);
        //        }
        //        else
        //        {
        //            await Context.Channel.SendMessageAsync("This user has over 25 modlogs!");
        //        }
        //    }
        //    else
        //    {
        //        //embed.WithDescription("This user has no modlogs!");
        //    }
        //    //await Context.Channel.SendMessageAsync("", false, embed.Build());
        //}

        [Command("modlevel")]
        public async Task ChangeModLevel(string level = null)
        {
            var user = Context.User as SocketGuildUser;
            var roleStaff = Context.Guild.GetRole(563030072026595339);
            var devRole = Context.Guild.GetRole(639547493767446538);
            var guild = Context.Guild;

            if (!user.Roles.Contains(roleStaff) && !user.Roles.Contains(devRole))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (level == null)
            {
                await Context.Channel.SendErrorAsync("Please provide a verification level!");
                return;
            }

            switch (level.ToLower())
            {
                case "medium":
                    await guild.ModifyAsync(x =>
                    {
                        x.VerificationLevel = VerificationLevel.Medium;
                    });
                    await Context.Channel.SendSuccessAsync("Changed verification level to `medium`!");
                    return;

                case "high":
                    await guild.ModifyAsync(x =>
                    {
                        x.VerificationLevel = VerificationLevel.High;
                    });
                    await Context.Channel.SendSuccessAsync("Changed verification level to `high`!");
                    return;
            }           
        }

        [Command("position")]
        public async Task RolePos(SocketRole role)
        {
            await ReplyAsync(role.Position.ToString());
        }

        DateTime _date { get; set; }

        [Command("modstats")]
        [Obsolete]
        public async Task ModStats(SocketGuildUser userAccount = null)
        {
            var user = Context.User as SocketGuildUser;
            var roleStaff = Context.Guild.GetRole(563030072026595339);
            var devRole = Context.Guild.GetRole(639547493767446538);
            var guild = Context.Guild;

            if (!user.Roles.Contains(roleStaff) && !user.Roles.Contains(devRole))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            var database = MongoService.Client.GetDatabase("LucaasBot");
            var collection = database.GetCollection<BsonDocument>("modlogs");
            var documents = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();

            int warns = 0;
            int warns7 = 0;
            int warns30 = 0;

            int mutes = 0;
            int mutes7 = 0;
            int mutes30 = 0;

            int kicks = 0;
            int kicks7 = 0;
            int kicks30 = 0;

            int bans = 0;
            int bans7 = 0;
            int bans30 = 0;

            var embed = new EmbedBuilder();

            if (userAccount == null)
            {
                foreach (var doc in documents)
                {
                    long modid1 = (doc["ModID"]).AsInt64;
                    ulong modid = (ulong)modid1;
                    string type = (doc["Action"]).AsString;
                    DateTime date = doc["DateCreated"].AsDateTime;
                    //_date = date;

                    if (modid == Context.User.Id)
                    {
                        if (type == "Warned")
                        {
                            warns++;

                            var days7 = DateTime.UtcNow - date;
                            if (days7.TotalDays <= 7)
                            {
                                warns7++;
                            }

                            var days30 = DateTime.UtcNow - date;
                            if (days30.TotalDays <= 30)
                            {
                                warns30++;
                            }
                        }

                        if (type == "Muted")
                        {
                            mutes++;

                            var days7 = DateTime.UtcNow - date;
                            if (days7.TotalDays <= 7)
                            {
                                mutes7++;
                            }

                            var days30 = DateTime.UtcNow - date;
                            if (days30.TotalDays <= 30)
                            {
                                mutes30++;
                            }
                        }

                        if (type == "Kicks")
                        {
                            kicks++;

                            var days7 = DateTime.UtcNow - date;
                            if (days7.TotalDays <= 7)
                            {
                                kicks7++;
                            }

                            var days30 = DateTime.UtcNow - date;
                            if (days30.TotalDays <= 30)
                            {
                                kicks30++;
                            }
                        }

                        if (type == "Banned")
                        {
                            bans++;

                            var days7 = DateTime.UtcNow - date;
                            if (days7.TotalDays <= 7)
                            {
                                bans7++;
                            }

                            var days30 = DateTime.UtcNow - date;
                            if (days30.TotalDays <= 30)
                            {
                                bans30++;
                            }
                        }
                    }
                }             
                
                
                embed.WithAuthor(Context.User);

                embed.AddField("Warnings (7 days)", warns7.ToString(), true);
                embed.AddField("Warnings (30 days)", warns30.ToString(), true);
                embed.AddField("Warnings (all time)", warns.ToString(), true);

                embed.AddField("Mutes (7 days)", mutes7.ToString(), true);
                embed.AddField("Mutes (30 days)", mutes30.ToString(), true);
                embed.AddField("Mutes (all time)", mutes.ToString(), true);

                embed.AddField("Kicks (7 days)", kicks7.ToString(), true);
                embed.AddField("Kicks (30 days)", kicks30.ToString(), true);
                embed.AddField("Kicks (all time)", kicks.ToString(), true);

                embed.AddField("Bans (7 days)", bans7.ToString(), true);
                embed.AddField("Bans (30 days)", bans30.ToString(), true);
                embed.AddField("Bans (all time)", bans.ToString(), true);

                embed.WithColor(Color.Blue);
                await ReplyAsync("", false, embed.Build());
            }

            if (!userAccount.Roles.Contains(roleStaff) && !userAccount.Roles.Contains(devRole))
            {
                await Context.Channel.SendErrorAsync("This user is not a staff member!");
                return;
            }         

            foreach (var doc in documents)
            {
                long modid1 = (doc["ModID"]).AsInt64;
                ulong modid = (ulong)modid1;
                string type = (doc["Action"]).AsString;
                DateTime date = doc["DateCreated"].AsDateTime;
                //_date = date;

                if (modid == userAccount.Id)
                {
                    if (type == "Warned")
                    {
                        warns++;

                        var days7 = DateTime.UtcNow - date;
                        if (days7.TotalDays <= 7)
                        {
                            warns7++;
                        }

                        var days30 = DateTime.UtcNow - date;
                        if (days30.TotalDays <= 30)
                        {
                            warns30++;
                        }
                    }

                    if (type == "Muted")
                    {
                        mutes++;

                        var days7 = DateTime.UtcNow - date;
                        if (days7.TotalDays <= 7)
                        {
                            mutes7++;
                        }

                        var days30 = DateTime.UtcNow - date;
                        if (days30.TotalDays <= 30)
                        {
                            mutes30++;
                        }
                    }

                    if (type == "Kicks")
                    {
                        kicks++;

                        var days7 = DateTime.UtcNow - date;
                        if (days7.TotalDays <= 7)
                        {
                            kicks7++;
                        }

                        var days30 = DateTime.UtcNow - date;
                        if (days30.TotalDays <= 30)
                        {
                            kicks30++;
                        }
                    }

                    if (type == "Banned")
                    {
                        bans++;

                        var days7 = DateTime.UtcNow - date;
                        if (days7.TotalDays <= 7)
                        {
                            bans7++;
                        }

                        var days30 = DateTime.UtcNow - date;
                        if (days30.TotalDays <= 30)
                        {
                            bans30++;
                        }
                    }
                }
            }

            embed.WithAuthor(userAccount);

            embed.AddField("Warnings (7 days)", warns7.ToString(), true);
            embed.AddField("Warnings (30 days)", warns30.ToString(), true);
            embed.AddField("Warnings (all time)", warns.ToString(), true);

            embed.AddField("Mutes (7 days)", mutes7.ToString(), true);
            embed.AddField("Mutes (30 days)", mutes30.ToString(), true);
            embed.AddField("Mutes (all time)", mutes.ToString(), true);

            embed.AddField("Kicks (7 days)", kicks7.ToString(), true);
            embed.AddField("Kicks (30 days)", kicks30.ToString(), true);
            embed.AddField("Kicks (all time)", kicks.ToString(), true);

            embed.AddField("Bans (7 days)", bans7.ToString(), true);
            embed.AddField("Bans (30 days)", bans30.ToString(), true);
            embed.AddField("Bans (all time)", bans.ToString(), true);

            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("move")]
        public async Task MoveUserVoice(SocketGuildUser userAccount = null, SocketVoiceChannel channel = null)
        {
            var user = Context.User as SocketGuildUser;
            var roleStaff = Context.Guild.GetRole(563030072026595339);
            var devRole = Context.Guild.GetRole(639547493767446538);

            if (!user.Roles.Contains(roleStaff) && !user.Roles.Contains(devRole))
            {
                await Context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            await userAccount.ModifyAsync(x =>
            {
                x.Channel = channel;
            });
        }

        [Command("join")]
        public async Task Join(SocketVoiceChannel channel)
        {
            var bot1 = Context.Guild.GetUser(688272456930033712);
            var bot = Context.Client.CurrentUser;

            await channel.ConnectAsync();
        }
    }
}
