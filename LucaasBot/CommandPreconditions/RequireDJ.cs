using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    public class RequireDJ : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Guild == null)
                return PreconditionResult.FromError("Not in guild");

            var guildUser = context.User as SocketGuildUser;

            if (guildUser.Roles.Any(x =>
                 x.Id == 639547493767446538 || // dev
                 x.Id == 563030072026595339 || // staff
                 x.Id == 620312075863851039 )) // dj 
            {
                return PreconditionResult.FromSuccess();
            }
            else
            {
                if(guildUser.VoiceChannel.Users.Count > 2)
                    return PreconditionResult.FromError("No DJ role");
                else 
                    return PreconditionResult.FromSuccess();
            }
        }
    }
}
