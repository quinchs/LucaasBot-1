using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using LucaasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    public abstract class DualPurposeModuleBase
    {
        public DualPurposeContext Context { get; private set; }

        public void SetContext(ICommandContext context)
        {
            this.Context = (DualPurposeContext)context;
        }


        //public Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent component = null)
        //    => ReplyAsync(message, isTTS: isTTS, embed: embed, options: options, allowedMentions: allowedMentions, messageReference: messageReference, component: component);

        public async Task<IUserMessage> ReplyAsync(string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageReference messageReference = null, RequestOptions options = null, MessageComponent component = null, Embed embed = null)
        {
            if (this.Context.Interaction == null)
            {
                return await Context.Channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference, component);
            }
            else
            {
                await Context.Interaction.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, options, component, embed);
            }

            return null;
        }

        public Task DeferAsync(bool ephemeral = false, RequestOptions options = null)
            => Context.Interaction.DeferAsync(ephemeral, options);

        public Task<RestFollowupMessage> FollowupAsync(string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, RequestOptions options = null, MessageComponent component = null, Embed embed = null)
            => Context.Interaction.FollowupAsync(text, embeds, isTTS, ephemeral, allowedMentions, options, component, embed);

        public object[] CreateInteractionArgs(CommandInfo info, DualPurposeCommandService service, object[] args)
        {
            var returnParams = new List<object>();
            if (!Context.IsInteraction)
                return args;

            for (int i = 0; i != info.Parameters.Count; i++)
            {
                var param = info.Parameters[i];

                if (Context.Interaction is SocketSlashCommand slash)
                {
                    var slashParam = slash.Data.Options?.FirstOrDefault(x => x.Name == param.Name);

                    if (slashParam == null)
                    {
                        returnParams.Add(Type.Missing);
                        continue;
                    }

                    var tp = slashParam.Value.GetType();

                    object value = null;

                    if (InternalConverters.ContainsKey((tp, param.Type)))
                    {
                        value = InternalConverters[(tp, param.Type)].Invoke(slashParam.Value);
                    }
                    else if (tp.IsAssignableTo(param.Type))
                        value = slashParam.Value;

                    returnParams.Add(value);
                }
            }

            return returnParams.ToArray();
        }

        private Dictionary<(Type from, Type to), Func<object, object>> InternalConverters = new Dictionary<(Type from, Type to), Func<object, object>>()
        {
            {(typeof(long), typeof(int)), (v) => { return Convert.ToInt32(v); } }
        };
    }
}
