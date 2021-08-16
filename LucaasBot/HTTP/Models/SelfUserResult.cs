using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Models
{
    public class SelfUserResult
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        public SelfUserResult() { }
        public SelfUserResult(WebUser user)
        {
            var guildUser = user.LucaasUser.GetAwaiter().GetResult();

            string color = "#ffffff";
            if (guildUser is SocketGuildUser sgu)
                color = sgu.Guild.Roles.FirstOrDefault(x => x.Position == sgu.Hierarchy).Color.ToString();
            else if (guildUser is RestGuildUser rgu)
                color = guildUser.Guild.Roles.FirstOrDefault(x => x.Position == GetRestHiearchy(rgu)).Color.ToString();

            this.Username = guildUser.Nickname ?? guildUser.Username;
            this.Id = $"{user.Id}";
            this.Color = color;
        }

        private int GetRestHiearchy(RestGuildUser u)
        {
            var guild = (u as IGuildUser).Guild;

            if (guild.OwnerId == u.Id)
                return int.MaxValue;

            int maxPos = 0;
            for (int i = 0; i < u.RoleIds.Count; i++)
            {
                var role = guild.Roles.ElementAt(i);
                if (role != null && role.Position > maxPos)
                    maxPos = role.Position;
            }
            return maxPos;
        }
    }
}
