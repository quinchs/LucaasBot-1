using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot.Modules
{
    /// <summary>
    ///     Represents a class that contains the methods for dealing with reporting users.
    /// </summary>
    public class ReportModule : DiscordHandler
    {
        /// <summary>
        ///     The report channel id.
        /// </summary>
        public readonly ulong ReportChannelId = 867993269097873439;

        private DiscordSocketClient client;
        private SocketTextChannel ReportChannel
            => client.GetGuild(464733888447643650).GetTextChannel(ReportChannelId);

        public override async Task InitializeAsync(DiscordSocketClient client)
        {
            this.client = client;

            this.client.InteractionCreated += Client_InteractionCreated;

            await RegisterReportCommand();
        }

        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            if (arg is SocketSlashCommand command && command.Data.Name == "report")
            {
                await command.RespondAsync(embed: null, type: InteractionResponseType.DeferredChannelMessageWithSource, ephemeral: true);

                _ = Task.Run(async () =>
                {
                    var guildUser = (SocketGuildUser)command.Data.Options.FirstOrDefault(x => x.Name == "user").Value;
                    var description = (string)command.Data.Options.FirstOrDefault(x => x.Name == "description").Value;
                    var evidence = (string)command.Data.Options.FirstOrDefault(x => x.Name == "evidence").Value;
                    var message = (string)command.Data.Options.FirstOrDefault(x => x.Name == "message").Value;

                    var embed = new EmbedBuilder()
                        .WithAuthor(command.User)
                        .WithTitle("New user report")
                        .WithDescription($"{arg.User} has reported {guildUser}")
                        .WithCurrentTimestamp()
                        .AddField("Description", description);

                    if (evidence != null)
                        embed.AddField("Evidence", evidence);

                    if (message != null)
                    {
                        List<IMessage> msgs = new();

                        var msgLinks = message.Split(' ');

                        foreach (var msg in msgLinks)
                        {
                            var discordMessage = await GetMessageFromUrl(msg);

                            if (discordMessage != null)
                                msgs.Add(discordMessage);
                        }

                        for (int i = 0; i != msgs.Count; i++)
                        {
                            var msg = msgs[i];
                            List<string> attachments = new();

                            if (msg.Attachments.Any())
                            {
                                foreach (var file in msg.Attachments)
                                {
                                    var bytes = await new WebClient().DownloadDataTaskAsync(file.ProxyUrl);

                                    var url = await HapsyService.GetImageLink(bytes, file.Filename);

                                    attachments.Add(url);
                                }
                            }

                            string Attachments = "None.";

                            for (int x = 0; x != attachments.Count; x++)
                            {
                                Attachments += $"Attachment {x}: {attachments[x]}\n";
                            }

                            embed.AddField($"Message {i}",
                                $"**Author**: {msg.Author.Mention} ({msg.Author})\n" +
                                $"**Content**: {msg.Content}\n" +
                                $"**Date**: {TimestampTag.FromDateTime(msg.CreatedAt.UtcDateTime, TimestampTagStyles.Relative)}\n" +
                                $"**Attachments**: {Attachments}"
                            );
                        }
                    }

                    await ReportChannel.SendMessageAsync(embed: embed.Build());

                    await command.FollowupAsync(embed: new EmbedBuilder()
                        .WithAuthor("Success!", "https://cdn.discordapp.com/emojis/312314752711786497.png?v=1")
                        .WithDescription("Your report has been filed! You may be contacted by staff for further questions about your report.")
                        .WithColor(Color.Green)
                    .WithCurrentTimestamp().Build(), ephemeral: true);
                });
            }
        }

        private Regex linkRegex = new Regex(@"channels\/(\d{17,18})\/(\d{17,18})\/(\d{17,18})");
        private Task<IMessage> GetMessageFromUrl(string url)
        {
            if (url == null)
                return null;

            var match = linkRegex.Match(url);

            if (!match.Success)
                return null;

            if (match.Groups.Count != 4)
                return null;

            var guild = ulong.Parse(match.Groups[1].Value);
            var channel = ulong.Parse(match.Groups[2].Value);
            var message = ulong.Parse(match.Groups[3].Value);

            return client.GetGuild(guild)?.GetTextChannel(channel)?.GetMessageAsync(message);
        }

        private async Task RegisterReportCommand()
        {
            var slashCommand = new SlashCommandBuilder()
                .WithName("report")
                .WithDescription("Report a user within the server")
                .AddOption("user", ApplicationCommandOptionType.User, "The user to report", true)
                .AddOption("description", ApplicationCommandOptionType.String, "Why you are reporting this user", true)
                .AddOption("evidence", ApplicationCommandOptionType.String, "Any evidence you have collected on this user, you can provide message id or links etc.")
                .AddOption("message", ApplicationCommandOptionType.String, "The message link(s) that support your report. Please seperate by a space.");

            await client.Rest.CreateGuildCommand(slashCommand.Build(), 464733888447643650);
        }
    }
}
