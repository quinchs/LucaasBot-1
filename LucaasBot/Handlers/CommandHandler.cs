using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Timers;
using MongoDB.Bson;
using MongoDB.Driver;
using Additions;
using LucaasBot.DataModels;

namespace LucaasBot.Services
{
    public class CommandHandler
    {
        // setup fields to be set later in the constructor
        //private readonly IConfiguration _config;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        public ulong guildId = 464733888447643650;
        public Timer t = new Timer();
        public Timer a = new Timer();
        public static int autoModMessageCounter = 5;
        public Timer johnPing = new Timer();

        public CommandHandler(CommandService service, DiscordSocketClient client)
        {
            _commands = service;
            _client = client;

            _commands.CommandExecuted += CommandExecutedAsync;
            _client.MessageReceived += MessageReceivedAsync;

            //_client.InteractionCreated += InteractionCreated;

            _client.MessageReceived += AutoModMsgRecieved;

            //_client.UserVoiceStateUpdated += VoiceStateUpdate;

            _client.GuildMemberUpdated += GuildMemberUpdated;

            t.Interval = 1000;
            t.Start();
            t.Elapsed += MuteTimer;

            a.Interval = 1000;
            a.Start();
            a.Elapsed += AutoModTimer;

            johnPing.Interval = 28800000;
            johnPing.Start();
            johnPing.Elapsed += JohnPing;

            _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }
        
        private Dictionary<ulong, (int messageCount, ulong channelId)> channelMessageCount = new Dictionary<ulong, (int messageCount, ulong channelId)>();

        //private async Task InteractionCreated(SocketInteraction arg)
        //{
        //    // If the type of the interaction is a message component
        //    if (arg.Type == Discord.InteractionType.MessageComponent)
        //    {
        //        // parse the args 
        //        var parsedArg = (SocketMessageComponent)arg;
        //        var userAccount = (SocketGuildUser)parsedArg.User;
        //        var channel = (SocketTextChannel)parsedArg.Channel;
        //        var msg = (SocketUserMessage)parsedArg.Message;

        //        if (parsedArg.Data.CustomId.StartsWith($"unmute_"))
        //        {
        //            ulong userid = ulong.Parse(parsedArg.Data.CustomId.Replace("unmute_", ""));
        //            //ulong userid = ulong.Parse(parsedArg.Data.CustomId.Replace();
        //            var guild = _client.GetGuild(guildId);
        //            var user = guild.GetUser(userid);
        //            var muted = guild.GetRole(465097693379690497);
        //            var staffRole = guild.GetRole(563030072026595339);
        //            var devRole = guild.GetRole(639547493767446538);

        //            if (!user.Roles.Contains(muted))
        //            {
        //                return;
        //            }

        //            if (userAccount.Roles.Contains(staffRole) || userAccount.Roles.Contains(devRole))
        //            {
        //                await user.RemoveRoleAsync(muted);
        //                await parsedArg.Channel.SendSuccessAsync($"Unmuted {user.Mention}", $"Unmuted by {parsedArg.User}");
        //                //await RemoveButton(parsedArg, msg);
        //                return;
        //            }                   
        //        }

        //        if (parsedArg.Data.CustomId == "cancelSlow")
        //        {
        //            var guild = _client.GetGuild(guildId);
        //            var staffRole = guild.GetRole(563030072026595339);
        //            var devRole = guild.GetRole(639547493767446538);

        //            if (channel.SlowModeInterval == 0)
        //            {
        //                return;
        //            }

        //            if (userAccount.Roles.Contains(staffRole) || userAccount.Roles.Contains(devRole))
        //            {
        //                await channel.ModifyAsync(x =>
        //                {
        //                    x.SlowModeInterval = 0;
        //                });
        //                await parsedArg.Channel.SendSuccessAsync($"Set slowmode to `0`", $"Action by {parsedArg.User}");

        //                //await RemoveButton(parsedArg, msg);
        //                return;
        //            }                     
        //        }
        //        // respond with the update message response type. This edits the original message if you have set AlwaysAcknowledgeInteractions to false.
        //        //await parsedArg.RespondAsync($"Clicked {parsedArg.Data.CustomId}!", type: InteractionResponseType.UpdateMessage);

        //        if (parsedArg.Data.CustomId == "test")
        //        {
        //            var role = _client.GetGuild(guildId).GetRole(618852744354332692);
        //            await userAccount.AddRoleAsync(role);
        //        }
        //    }
        //}

        //private async Task VoiceStateUpdate(SocketUser user, SocketVoiceState state1, SocketVoiceState state2)
        //{
        //    var userAccount = (SocketGuildUser)user;
        //    if (user.IsBot)
        //        return;

        //    if (state2.VoiceChannel?.Id == 623534623720472626)
        //    {
        //        await userAccount.VoiceChannel.DisconnectAsync();
        //    }
        //}

        public async Task RemoveButton(SocketMessageComponent parsedArg, SocketUserMessage msg)
        {
            var builder = new ComponentBuilder();
            builder.ActionRows = new List<ActionRowBuilder>();
            foreach (ActionRowComponent item in msg.Components)
            {
                var row = new ActionRowBuilder();

                foreach (var component in item.Components)
                {
                    if (component is ButtonComponent button && button.CustomId != parsedArg.Data.CustomId)
                        row.WithComponent(button);
                }

                builder.ActionRows.Add(row);
            }

            await msg.ModifyAsync(x => x.Components = builder.Build());
        }

        public async Task AutoModMsgRecieved(SocketMessage rawMessage)
        {
            var user = rawMessage.Author;

            if (user.IsBot)
            {
                return;
            }

            if (channelMessageCount.ContainsKey(user.Id))
            {
                var val = channelMessageCount[user.Id];
                channelMessageCount[user.Id] = (val.messageCount + 1, rawMessage.Channel.Id);
            }
            else
            {
                channelMessageCount.Add(user.Id, (1, rawMessage.Channel.Id));
            }

            if (rawMessage.MentionedRoles.Count >= 3)
            {
                await AutomodMute(user.Id, rawMessage.Channel.Id);
            }

            if (rawMessage.MentionedUsers.Count >= 5)
            {
                await AutomodMute(user.Id, rawMessage.Channel.Id);
            }
        }

        static MongoClient Client = new MongoClient(ConfigService.Config.MongoCS);

        public void AutoModTimer(object s, EventArgs a)
        {
            foreach (var item in channelMessageCount)
            {
                if (item.Value.messageCount >= autoModMessageCounter)
                {
                    // Mute the user
                    var userId = item.Key;
                    Task.Run(async () =>
                    {
                        await AutomodMute(userId, item.Value.channelId).ConfigureAwait(false);

                    }).ConfigureAwait(false);
                }
            }
            channelMessageCount.Clear();
        }

        public void MuteTimer(object s, EventArgs a)
        {
            var database = Client.GetDatabase("LucaasBot");
            var collection = database.GetCollection<BsonDocument>("mute-times");

            ////SocketRole muterole = _client.GetGuild(guildId).GetRole(465097693379690497);

            var documents = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
            foreach (var doc in documents)
            {
                long userid1 = (doc["UserID"]).AsInt64;
                ulong userid = (ulong)userid1;
                var mutes = UserMutes.GetOrCreateMute(userid);

                var muterole = _client.GetGuild(guildId).GetRole(465097693379690497);
                var user = _client.GetGuild(guildId).GetUser(userid);

                if (mutes.Type == "s")
                {
                    if ((DateTime.UtcNow - mutes.DateTime).TotalSeconds > mutes.Time)
                    {
                        user.RemoveRoleAsync(muterole);
                        collection.DeleteOne(doc);
                    }
                }

                if (mutes.Type == "m")
                {
                    if ((DateTime.UtcNow - mutes.DateTime).TotalMinutes > mutes.Time)
                    {
                        user.RemoveRoleAsync(muterole);
                        collection.DeleteOne(doc);
                    }
                }

                if (mutes.Type == "h")
                {
                    if ((DateTime.UtcNow - mutes.DateTime).TotalHours > mutes.Time)
                    {
                        user.RemoveRoleAsync(muterole);
                        collection.DeleteOne(doc);
                    }
                }
            }
        }

        public async Task GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> user1, SocketGuildUser user2)
        {
            if (user1.Id == 628246728658911254)
            {
                if (user2.VoiceChannel?.Id == 623534623720472626)
                {
                    await user2.ModifyAsync(x => x.Channel = null);
                }
            }
            else
            {
                return;
            }
        }

        public async void JohnPing(object s, EventArgs a)
        {
            var channel = _client.GetGuild(guildId).GetTextChannel(465083688795766785);
            await channel.SendMessageAsync("<@561464966427705374> ");
        }


        public async Task AutomodMute(ulong userId, ulong channelId)
        {
            var guild = _client.GetGuild(guildId);
            var user = guild.GetUser(userId);
            var channel = guild.GetTextChannel(channelId);

            if (user.IsBot)
            {
                return;
            }

            var muted = guild.GetRole(465097693379690497);

            try
            {
                await user.AddRoleAsync(muted);
            }
            catch
            {
                await channel.SendMessageAsync("Was not able to mute this user!");
            }


            var embed = new EmbedBuilder();
            embed.WithTitle("Automoderation Mute!");
            embed.AddField("User", user.Mention, true);
            embed.AddField("Unmute", "To unmute the user, `!unmute <user>`", true);
            embed.WithColor(Color.Green);

            try
            {
                await channel.ModifyAsync(x =>
                {
                    x.SlowModeInterval = 3;
                });
                embed.WithDescription("AutoModeration detected a potential raid! Slowmode has been set to `3` seconds!");
            }
            catch
            {
                embed.WithDescription("AutoModeration detected a potential raid! Slowmode was not set!");
            }
            await guild.GetTextChannel(channelId).SendMessageAsync("", false, embed.Build());
            //var builder = new ComponentBuilder().WithButton("Unmute User", $"unmute_{userId}", ButtonStyle.Primary).WithButton("Slowmode 0", "cancelSlow", ButtonStyle.Primary);
            //await guild.GetTextChannel(channelId).SendMessageAsync("", embed: embed.Build(), component: builder.Build());

            var modlogschannel = _client.GetGuild(guildId).GetTextChannel(581614769237262357);
            var log = new EmbedBuilder();
            log.WithTitle("AutoModeration Mute");
            log.WithDescription($"**Offender:** {user.Mention}\n**Reason:** Detected Raid\n**Moderator:** LucaasBot Automoderation\n**In:** {guild.GetTextChannel(channelId).Mention}");
            log.WithColor(Color.Red);
            await modlogschannel.SendMessageAsync("", false, log.Build());

            ConsoleLog("Automod Mute", user.Username);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Source != MessageSource.User)
            {
                return;
            }

            if (rawMessage.Author.Id == 199641055837159425)
            {
                if (rawMessage.Content.Contains("https://media1.tenor.com/images/9bf9711f86faa7d136a26b6f09a00687/tenor.gif"))
                {
                    await rawMessage.Channel.SendMessageAsync("https://media.tenor.com/images/7b56e0f8fbc3b6b29353260b7efa2278/tenor.gif", messageReference: new MessageReference(rawMessage.Id, rawMessage.Channel.Id, guildId));
                }
            }
            

            var argPos = 0;
            char prefix = '=';
            char prefix1 = '*';

            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(prefix, ref argPos) || message.HasCharPrefix(prefix1, ref argPos)))
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);
            await _commands.ExecuteAsync(context, argPos, null, MultiMatchHandling.Best);
        }

        public async Task CommandExecutedAsync(Discord.Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method

            if (!command.IsSpecified)
            {
                System.Console.WriteLine("Unknown Command Was Used");

                return;
            }

            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("Command Success\n------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                string time = DateTime.Now.ToString("hh:mm:ss tt");
                System.Console.WriteLine($"Command: [{command.Value.Name}]\nUser: [{context.User.Username}]\nTime: {time}");
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            else if(result.Error != CommandError.UnknownCommand)
            {
                var embed = new EmbedBuilder();
                embed.WithAuthor("Command Error", "https://cdn.discordapp.com/emojis/787035973287542854.png?v=1");
                //embed.WithDescription(result.ErrorReason);
                embed.WithDescription("There was an error, check logs.");
                embed.WithColor(Color.Red);
                await context.Channel.SendMessageAsync("", false, embed.Build());

                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("Command Error\n------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                string time = DateTime.Now.ToString("hh:mm:ss tt");
                System.Console.WriteLine($"Command: [{command.Value.Name}]\nUser: [{context.User.Username}]\nTime: {time}\nError: {result.ErrorReason}");
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static void ConsoleLog(string action, string user, string details = null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("Action Success\n------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            string time = DateTime.Now.ToString("hh:mm:ss tt");
            System.Console.WriteLine($"Action: [{action}]\nUser: [{user}]\nTime: {time}");
            System.Console.WriteLine($"Details: {details}");
            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }
    }
}