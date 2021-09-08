using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Handlers
{
    public class AnthonyMemesHandler : DiscordHandler
    {
        public override void Initialize(DiscordSocketClient client)
        {
            client.MessageReceived += Client_MessageReceived;
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            if (arg.Author.Id != 199641055837159425)
                return;

            if (arg.Content == "https://tenor.com/view/trumpwinning-trump-happy-dance-tgif-gif-18968896")
            {
                await arg.Channel.SendMessageAsync("https://tenor.com/view/donald-trump-dancing-maga-trump-gif-18842875", messageReference: arg.GetReference());
            }
            else if (arg.Content == "https://tenor.com/view/donald-trump-dancing-maga-trump-gif-18842875")
            {
                await arg.Channel.SendMessageAsync("https://tenor.com/view/trumpwinning-trump-happy-dance-tgif-gif-18968896", messageReference: arg.GetReference());

            }
        }
    }
}
