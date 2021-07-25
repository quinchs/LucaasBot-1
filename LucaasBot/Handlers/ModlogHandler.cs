using LucaasBot;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LucaasBot.DataModels;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LucaasBot.Handlers
{
    public class ModlogHandler : DiscordHandler
    {
        private DiscordSocketClient Client;
        private Timer MuteTimer;

        private SocketGuild Guild
            => Client.GetGuild(464733888447643650);

        public override void Initialize(DiscordSocketClient client)
        {
            this.Client = client;

            this.MuteTimer = new(1000);

            MuteTimer.Elapsed += CheckUnmutes;
        }

        private async void CheckUnmutes(object sender, ElapsedEventArgs e)
        {
            var unmutes = MongoService.MutesCollection.Find(x => DateTime.UtcNow > x.UnmuteTime).ToList();

            foreach(var mute in unmutes)
            {
                if (Guild.TryFindUserAsync(mute.UserID, out var user))
                {
                    await UnmuteUserAsync(user, mute);
                }
            }
        }

        private async Task UnmuteUserAsync(IGuildUser user, UserMutes mute)
        {
            try
            {
                await user.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("You were Unmuted!")
                .WithDescription($"Your mute has expired, you can now talk in {Guild.Name} again.")
                .WithColor(Color.Green)
                .WithCurrentTimestamp().Build());
            }
            finally
            {
                await user.ModifyAsync(x => x.RoleIds = user.RoleIds.Where(x => x != 465097693379690497).ToArray());
                mute.Delete();
            }
        }

        public void HandleModCommand(SocketCommandContext context, ModlogAction action, SocketGuildUser target, string reason, TimeSpan? muteDir = null)
        {
            _ = Task.Run(async () => await HandleModCommandAsync(context, action, target, reason, muteDir));
        }



        private async Task HandleModCommandAsync(SocketCommandContext context, ModlogAction action, SocketGuildUser target, string reason, TimeSpan? muteDir = null)
        {
            if (context.User is not SocketGuildUser user)
            {
                return;
            }

            if (!user.IsStaff())
            {
                await context.Channel.SendErrorAsync("You do not have access to use this command!");
                return;
            }

            if (target == null)
            {
                await context.Channel.SendErrorAsync("Please mention a user!");
                return;
            }

            if (user.Hierarchy <= target.Hierarchy)
            {
                await context.Channel.SendErrorAsync("You cannot ban this user!");
                return;
            }

            if (reason == null)
            {
                await context.Channel.SendErrorAsync("Please provide a reason!");
                return;
            }

            if (user.Id == target.Id)
            {
                await context.Channel.SendErrorAsync("You cannot ban youself!");
                return;
            }

            var discordUser = DiscordUser.GetOrCreateDiscordUser(target);
            var log = discordUser.AddModlog(context.User.Id, reason, action);

            bool gotDM = true;

            // try to dm the user
            try
            {
                await target.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle($"You have been {action.Format()}!")
                    .AddField("Reason", reason, true)
                    .AddField("Moderator", user, true)
                    .AddField("Guild", context.Guild?.Name ?? "Unknown", true)
                    .WithColor(action.GetColor())
                .WithCurrentTimestamp().Build());
            }
            catch(Exception x)
            {
                gotDM = false;
            }

            switch (action)
            {
                case ModlogAction.Ban:
                    await target.BanAsync(7, reason);
                    break;

                case ModlogAction.Kick:
                    await target.KickAsync(reason);
                    break;

                case ModlogAction.Mute:
                    var _ = new UserMutes(target.Id, DateTime.UtcNow.Add(muteDir.GetValueOrDefault()));
                    break;

                case ModlogAction.Unmute:
                    var mute = UserMutes.GetMute(target.Id);
                    await UnmuteUserAsync(target, mute);
                    break;

            }

            var modlogsChannel = context.Guild.GetTextChannel(663060075740659715);
            await modlogsChannel.ModlogAsync(target, user, log, context.Channel, gotDM);

            await context.Channel.SendInfractionAsync(target, user, log, gotDM);
        }
    }
}
