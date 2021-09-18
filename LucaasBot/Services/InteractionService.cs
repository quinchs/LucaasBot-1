using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    public class InteractionService : DiscordHandler
    {
        private DiscordSocketClient client;

        public override void Initialize(DiscordSocketClient client)
        {
            this.client = client;
        }

        public async Task<SocketMessageComponent> NextSelection(IMessage message, IUser user, string userInvalidText = null)
        {
            var tcs = new TaskCompletionSource<SocketMessageComponent>();

            async Task HandlePrecondition(SocketMessageComponent comp)
            {
                if (comp.Message.Id == message.Id && comp.User.Id == user.Id)
                    tcs.SetResult(comp);
                else if(comp.Message.Id == message.Id)
                {
                    await comp.RespondAsync(userInvalidText ?? $"Only {user.Mention} can use this select menu!", ephemeral: true);
                }
            }

            client.SelectMenuExecuted += HandlePrecondition;

            var comp = await tcs.Task;

            client.SelectMenuExecuted -= HandlePrecondition;

            return comp;
        }

        public async Task CreateVoteComponentsAsync(ICommandContext context, EmbedBuilder builder, IGuildUser executer, int targetVotes, Action<int, List<IGuildUser>, EmbedBuilder> onChange, Func<IUserMessage, Task> onComplete, string buttonText = "Vote")
        {
            var cid = $"vote-{Guid.NewGuid()}";

            int currentVotes = 1;

            List<IGuildUser> voters = new();

            voters.Add(executer);

            var components = new ComponentBuilder()
                .WithButton(buttonText, cid, ButtonStyle.Primary, Emote.Parse("<:upvote:618159115608391709>"));

            IUserMessage message = null;

            async Task HandleButtonClick(SocketMessageComponent comp)
            {
                if (comp.Data.CustomId != cid)
                    return;

                if(voters.Any(x => x.Id == comp.User.Id))
                {
                    await comp.RespondAsync("You have already voted to skip!", ephemeral: true);
                    return;
                }

                await comp.DeferAsync();
                
                voters.Add(comp.User as SocketGuildUser);
                currentVotes++;

                if(currentVotes >= targetVotes)
                {
                    await onComplete(message);
                }
                else
                {
                    onChange(currentVotes, voters, builder);

                    await comp.UpdateAsync(x => x.Embed = builder.Build());
                }
            }

            if (context is DualPurposeContext dContext && dContext.IsInteraction)
            {
                await dContext.Interaction.RespondAsync(embed: builder.Build(), component: components.Build());
                message = await dContext.Interaction.GetOriginalResponseAsync();
            }
            else
                message = await context.Channel.SendMessageAsync(embed: builder.Build(), component: components.Build());

            client.ButtonExecuted += HandleButtonClick;
        }

        public Task SendButtonPaginator(ICommandContext context, IUser executer, int currentPage, Func<int, EmbedBuilder> pageFactory, int totalElements, int itemsPerPage, bool ephemeral = false)
            => SendButtonPaginator(context, executer, currentPage, (Func<int, Task<EmbedBuilder>>)((i) => Task.FromResult(pageFactory(i))), totalElements, itemsPerPage, ephemeral);

        public async Task SendButtonPaginator(ICommandContext context, IUser executer, int currentPage, Func<int, Task<EmbedBuilder>> pageFactory, int totalElements, int itemsPerPage, bool ephemeral = false)
        {
            var embed = await pageFactory(currentPage).ConfigureAwait(false);
            var lastPage = (totalElements - 1) / itemsPerPage;

            embed.AddPaginatedFooter(currentPage, lastPage);

            var components = new ComponentBuilder()
                .WithButton(customId: "page-left",  emote: new Emoji("⬅"))
                .WithButton(customId: "page-right", emote: new Emoji("➡"));

            IUserMessage msg;

            if(context is DualPurposeContext dContext && dContext.IsInteraction)
            {
                await dContext.ReplyAsync(embed: embed.Build(), component: components.Build(), ephemeral: ephemeral);
                msg = await dContext.Interaction.GetOriginalResponseAsync();
            }    
            else
                msg = await context.Channel.SendMessageAsync(embed: embed.Build(), component: components.Build());

            var lastPageChange = DateTime.MinValue;

            async Task changePage(SocketMessageComponent comp)
            {
                try
                {
                    if (comp.User.Id != executer.Id)
                        return;
                    if (DateTime.UtcNow - lastPageChange < TimeSpan.FromSeconds(1))
                        return;

                    if (comp.Data.CustomId == "page-left")
                    {
                        if (currentPage == 0)
                        {
                            await comp.DeferAsync();
                            return;
                        }
                        lastPageChange = DateTime.UtcNow;
                        var toSend = await pageFactory(--currentPage).ConfigureAwait(false);
                        toSend.AddPaginatedFooter(currentPage, lastPage);
                        await comp.UpdateAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                    else if (comp.Data.CustomId == "page-right")
                    {
                        if (lastPage > currentPage)
                        {
                            lastPageChange = DateTime.UtcNow;
                            var toSend = await pageFactory(++currentPage).ConfigureAwait(false);
                            toSend.AddPaginatedFooter(currentPage, lastPage);
                            await comp.UpdateAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                        }
                        else
                            await comp.DeferAsync();
                    }
                }
                catch (Exception)
                {
                    //ignored
                }
            }

            client.ButtonExecuted += changePage;
        }
    }
}
