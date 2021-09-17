using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot.TypeReaders
{
    public class UserTypeReader : Discord.Commands.TypeReader
    {
        public static UserTypeReader Instance
            => new UserTypeReader();

        private Regex userIdRegex = new Regex(@"(\d{17,19})");

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if(context.Guild == null)
            {
                return TypeReaderResult.FromError(CommandError.UnmetPrecondition, "Cannot get guilduser in non guild");
            }

            var match = userIdRegex.Match(input);

            if (!match.Success)
            {
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Format not of user");
            }

            var id = ulong.Parse(match.Groups[1].Value);

            var user = await UserService.FindUserAsync(context.Guild, id);

            return TypeReaderResult.FromSuccess(user);
        }
    }
}
