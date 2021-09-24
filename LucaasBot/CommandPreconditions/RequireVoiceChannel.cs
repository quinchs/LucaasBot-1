using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    public class RequireVoiceChannel : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is IGuildUser u && u.VoiceChannel != null)
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError(new VoiceChannelErrorResult());
        }
    }

    public class VoiceChannelErrorResult : IResult
    {
        public CommandError? Error => CommandError.UnmetPrecondition;

        public string ErrorReason => "Not in voice channel";

        public bool IsSuccess => false;
    }
}
