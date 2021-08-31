using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    static class UserService
    {
        private static DiscordSocketClient Client;

        public static void Initialize(DiscordSocketClient client)
        {
            Client = client;
        }

        public static async Task<IGuildUser> FindUserAsync(this IGuild guild, ulong userId)
        {
            if(guild is SocketGuild socketGuild)
            {
                var tmp = socketGuild.GetUser(userId);

                if (tmp != null)
                    return tmp;
            }

            return await Client.Rest.GetGuildUserAsync(guild.Id, userId);
        }

        public static bool TryFindUserAsync(this IGuild guild, ulong userId, out IGuildUser user)
        {
            user = FindUserAsync(guild, userId).GetAwaiter().GetResult();

            return user != null;
        }
    }
}
