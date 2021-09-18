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
using LucaasBot;
using LucaasBot.DataModels;
using LucaasBot.Handlers;
using LucaasBot.TypeReaders;
using LucaasBot.Services;

namespace LucaasBot.Handlers
{
    public class CommandHandler
    {
        public static DualPurposeCommandService Service
            => _commands;

        // setup fields to be set later in the constructor
        //private readonly IConfiguration _config;
        private static DualPurposeCommandService _commands;
        private readonly DiscordSocketClient _client;
        public ulong guildId = 464733888447643650;
        public Timer a = new Timer();
        public static int autoModMessageCounter = 5;
        //public Timer johnPing = new Timer();

        public CommandHandler(DualPurposeCommandService service, DiscordSocketClient client)
        {
            _commands = service;
            _commands.AddTypeReader(typeof(IGuildUser), UserTypeReader.Instance);
            _client = client;

            _commands.CommandExecuted += CommandExecutedAsync;
            _client.MessageReceived += MessageReceivedAsync;

            //_client.InteractionCreated += InteractionCreated;

            _client.MessageReceived += AutoModMsgRecieved;

            _client.SlashCommandExecuted += _client_SlashCommandExecuted;

            //_client.UserVoiceStateUpdated += VoiceStateUpdate;

            _client.GuildMemberUpdated += GuildMemberUpdated;

            a.Interval = 1000;
            a.Start();
            a.Elapsed += AutoModTimer;

            //johnPing.Interval = 28800000;
            //johnPing.Start();
            //johnPing.Elapsed += JohnPing;

            _commands.RegisterModulesAsync(Assembly.GetEntryAssembly(), null).GetAwaiter().GetResult();

            Logger.Write("Command handler <Green>Initialized</Green>", Severity.Core);
        }

        private async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            var context = new DualPurposeContext(_client, arg);
            await _commands.ExecuteAsync(context, arg.Data.Name, null);
        }

        private Dictionary<ulong, (int messageCount, ulong channelId)> channelMessageCount = new Dictionary<ulong, (int messageCount, ulong channelId)>();

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
                        row.AddComponent(button);
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
                await AutomodMute(user.Id, rawMessage.Channel.Id, "raid");
            }

            if (rawMessage.MentionedUsers.Count >= 5)
            {
                await AutomodMute(user.Id, rawMessage.Channel.Id, "raid");
            }

            if (rawMessage.Content.Contains("http"))
            {
                var lowerCaseRawMessage1 = rawMessage.Content.ToLower();
                string[] steamLinks = new string[]
                {
                    "steem",
                    "sleam",
                    "staem",
                    "comnumnuty",
                    "tradeofer",
                    "hello i am leaving cs:go",
                    "giving away my skins",
                    "staem",
                    "comnumnuty",
                    "tradeofer",
                };

                if (steamLinks.Any(lowerCaseRawMessage1.Contains))
                {
                    await rawMessage.DeleteAsync();
                    await AutomodMute(rawMessage.Author.Id, rawMessage.Channel.Id, "scam link");
                }
            }

            var censorList = Censor.GetCensors(guildId);

            var guildUser = (SocketGuildUser)rawMessage.Author;

            if (guildUser.IsStaff())
            {
                return;
            }

            foreach (var doc in censorList)
            {
                if (rawMessage.ToString().ToLower().Contains(doc.CensorText.ToLower()))
                {
                    await rawMessage.DeleteAsync();
                }
            }
        }

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
                        await AutomodMute(userId, item.Value.channelId, "raid").ConfigureAwait(false);

                    }).ConfigureAwait(false);
                }
            }
            channelMessageCount.Clear();
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

        //public async void JohnPing(object s, EventArgs a)
        //{
        //    var channel = _client.GetGuild(guildId).GetTextChannel(465083688795766785);
        //    await channel.SendMessageAsync("<@561464966427705374> ");
        //}


        public async Task AutomodMute(ulong userId, ulong channelId, string type)
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
                embed.WithDescription($"AutoModeration detected a potential {type}! Slowmode has been set to `3` seconds!");
            }
            catch
            {
                embed.WithDescription($"AutoModeration detected a potential {type}! Slowmode was not set!");
            }
            await guild.GetTextChannel(channelId).SendMessageAsync("", false, embed.Build());
            //var builder = new ComponentBuilder().WithButton("Unmute User", $"unmute_{userId}", ButtonStyle.Primary).WithButton("Slowmode 0", "cancelSlow", ButtonStyle.Primary);
            //await guild.GetTextChannel(channelId).SendMessageAsync("", embed: embed.Build(), component: builder.Build());

            var modlogschannel = _client.GetGuild(guildId).GetTextChannel(581614769237262357);
            var log = new EmbedBuilder();
            log.WithTitle("AutoModeration Mute");
            log.WithDescription($"**Offender:** {user.Mention}\n**Reason:** Detected {type}\n**Moderator:** LucaasBot Automoderation\n**In:** {guild.GetTextChannel(channelId).Mention}");
            log.WithColor(Color.Red);
            await modlogschannel.SendMessageAsync("", false, log.Build());

            Logger.Write($"User {user} was automuted", Severity.Core, Severity.Verbose);
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

            var name = message.Content.Substring(argPos).Split(' ').First();
            var context = await _commands.ResolveContextAsync(_client, name, message);
            await _commands.ExecuteAsync(context, argPos, null, MultiMatchHandling.Best);
        }

        public async Task CommandExecutedAsync(Discord.Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method

            if (!command.IsSpecified)
            {
                Logger.Write("Unknown Command Was Used", Severity.Core, Severity.Verbose);
                return;
            }

            Logger.Write($"Command: [{command.Value.Name}] User: [{context.User.Username}] Result: [{(result.IsSuccess ? "Success" : $"{result.Error}: {result.ErrorReason}")}]", Severity.Core, Severity.Verbose);

            if(result.Error != null && 
                result.Error != CommandError.BadArgCount && 
                result.Error != CommandError.UnknownCommand && 
                result.Error != CommandError.UnmetPrecondition)
            {
                if (result is ExecuteResult executeResult)
                {
                    var embed = new EmbedBuilder();
                    embed.WithAuthor("Command Error", "https://cdn.discordapp.com/emojis/787035973287542854.png?v=1");
                    if (result.Error.HasValue)
                    {
                        embed.WithDescription(executeResult.Exception.Message);
                    }
                    //embed.WithDescription("There was an error, check logs.");
                    embed.WithColor(Color.Red);
                    await context.Channel.SendMessageAsync("", false, embed.Build());

                    Logger.Write($"Command Error: {result.Error} - {result.ErrorReason}{(result.Error.HasValue ? $"\n{executeResult.Exception}" : "")}", Severity.Core, Severity.Warning);
                }
            }
        }
    }
}