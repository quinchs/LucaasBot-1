using Discord;
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

        public Task SendButtonPaginator(IMessageChannel channel, IUser executer, int currentPage, Func<int, EmbedBuilder> pageFactory, int totalElements, int itemsPerPage)
            => SendButtonPaginator(channel, executer, currentPage, (Func<int, Task<EmbedBuilder>>)((i) => Task.FromResult(pageFactory(i))), totalElements, itemsPerPage);

        public async Task SendButtonPaginator(IMessageChannel channel, IUser executer, int currentPage, Func<int, Task<EmbedBuilder>> pageFactory, int totalElements, int itemsPerPage)
        {
            var embed = await pageFactory(currentPage).ConfigureAwait(false);
            var lastPage = (totalElements - 1) / itemsPerPage;

            embed.AddPaginatedFooter(currentPage, lastPage);

            var components = new ComponentBuilder()
                .WithButton(customId: "page-left",  emote: (Emoji)"⬅")
                .WithButton(customId: "page-right", emote: (Emoji)"➡");

            var msg = await channel.SendMessageAsync(embed: embed.Build(), component: components.Build());

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
                            return;
                        lastPageChange = DateTime.UtcNow;
                        var toSend = await pageFactory(--currentPage).ConfigureAwait(false);
                        toSend.AddPaginatedFooter(currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                    else if (comp.Data.CustomId == "page-right")
                    {
                        if (lastPage > currentPage)
                        {
                            lastPageChange = DateTime.UtcNow;
                            var toSend = await pageFactory(++currentPage).ConfigureAwait(false);
                            toSend.AddPaginatedFooter(currentPage, lastPage);
                            await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                        }
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
