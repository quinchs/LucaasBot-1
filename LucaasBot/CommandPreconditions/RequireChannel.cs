using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    public class RequireChannel : PreconditionAttribute
    {
        private ulong ChannelId { get; }
        public RequireChannel(ulong channelId)
        {
            this.ChannelId = channelId;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if(context.Channel.Id == this.ChannelId)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError($"Not in defined channel {ChannelId}"));
            }
        }
    }
}
