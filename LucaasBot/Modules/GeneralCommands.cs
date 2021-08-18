using LucaasBot;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LucaasBot.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot.Modules
{
    public class GeneralCommands : ModuleBase<SocketCommandContext>
    {
        [Command("married")]
        public async Task married() 
        {
            await ReplyAsync("Congrats to <@259053800755691520> and <@790546298306166794> for getting married", messageReference: Context.Message.GetReference(), allowedMentions: new AllowedMentions(AllowedMentionTypes.None));
        }

        [Command("terminate")]
        public async Task TerminateClient()
        {
            if (Context.User.Id == 619241308912877609 || (Context.User is SocketGuildUser user && user.Roles.Any(x => x.Id == 639547493767446538 || x.Permissions.Administrator)))
            {
                await Context.Client.LogoutAsync();
            }
        }

        [Command("restart")]
        public async Task restart()
        {
            if (Context.User.Id == 619241308912877609 || (Context.User is SocketGuildUser user && user.Roles.Any(x => x.Id == 639547493767446538 || x.Permissions.Administrator)))
            {
                await Context.Client.LogoutAsync();
                await Context.Client.StopAsync();

                await Task.Delay(2000);

                await Context.Client.LoginAsync(TokenType.Bot, ConfigService.Config.Token);
                await Context.Client.StartAsync();
            }
        }

        [Command("position")]
        public async Task RolePos(SocketRole role)
        {
            await ReplyAsync(role.Position.ToString());
        }

        [Command("join")]
        public async Task Join(SocketVoiceChannel channel)
        {
            var bot1 = Context.Guild.GetUser(688272456930033712);
            var bot = Context.Client.CurrentUser;

            await channel.ConnectAsync();
        }

        [Command("ping")]
        public async Task ping()
        {
            var msg = await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
            {
                Title = "Discord Ping and Status",
                Color = Color.Green,
                Description = $"You can view Discord's status page [here](https://status.discord.com/)\n" +
                               $"```\nGateway:     Fetching...\n" +
                               $"Api Latest:  Fetching...\n" +
                               $"Api Average: Fetching...```",
                Footer = new EmbedFooterBuilder()
                {
                    Text = "Last Updated: Fetching..."
                }
            }.Build());

            var embed = await HandlerService.GetHandlerInstance<PingGraphHandler>().GenerateGraphEmbed();

            await msg.ModifyAsync(x => 
            {
                x.Embed = embed;
                x.Components = new ComponentBuilder().WithButton("Refresh", "refresh_ping").Build();
            });
        }


        [Command("whois"), Alias("u")]
        public async Task whoIs(SocketGuildUser target = null)
        {
            if(target == null && Context.User is SocketGuildUser user)
            {
                target = user;
            }

            var embed = new EmbedBuilder()
                .WithAuthor(target)
                .AddField("Mention", target.Mention)
                .AddField("Id", target.Id)
                .AddField("Roles", string.Join("\n", target.Roles.Select(x => x.Mention)))
                .AddField("Nickname?", target.Nickname ?? "None")
                .AddField("Status", target.Status)
                .AddField("Created at UTC", target.CreatedAt.UtcDateTime.ToString("r"))
                .AddField("Joined at UTC?", target.JoinedAt.HasValue ? target.JoinedAt.Value.UtcDateTime.ToString("r") : "No value :/")
                .WithColor(Color.DarkPurple)
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: embed, messageReference: Context.Message.GetReference());
        }

        [Command("uwu"), Alias("owo")]
        public async Task uwu([Remainder] string text)
        {
            await ReplyAsync(oWoTextLmao(text));
        }

        [Command("8ball"), Alias("8b")]
        public async Task EightBall([Remainder] string question)
        {
            if (string.IsNullOrEmpty(question))
                return;
            
            var rand = new Random(question.GetHashCode());

            string[] answers = { "As I see it, yes.", "Ask again later.", "It is certain.", "It is decidedly so.", "Don't count on it.", "Better not tell you now.", "Concentrate and ask again.", " Cannot predict now.",
            "Most likely.", "My reply is no", "Yes.", "You may rely on it.", "Yes - definitely.", "Very doubtful.", "Without a doubt.", " My sources say no.", " Outlook not so good.", "Outlook good.", "Reply hazy, try again",
            "Signs point to yes"};

            int index = rand.Next(answers.Length);

            await ReplyAsync(answers[index], messageReference: Context.Message.GetReference());
        }

        private string oWoTextLmao(string text)
        {
            string final = Regex.Replace(text, "(?:r|l)", "w");
            final = Regex.Replace(final, "(?:R|L)", "W");
            final = Regex.Replace(final, "n([aeiou])", "ny");
            final = Regex.Replace(final, "N([aeiou])", "Ny");
            final = Regex.Replace(final, "N([AEIOU])", "Ny");
            final = Regex.Replace(final, "ove", "uv");
            final = final.Replace("@here", "@h3re");
            final = final.Replace("@everyone", "@every0ne");
            //final = Regex.Replace(final, @"/\!+/g", "");
            return final;
        }
    }
}
