using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LucaasBot.Music.Entities;
using LucaasBot.Music.Exceptions;
using LucaasBot.Music.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Modules
{
    [RequireContext(ContextType.Guild), RequireChannel(828687366893076480)] // music channel id.
    public class MusicCommands : DualPurposeModuleBase
    {
        public MusicService MusicService
            => HandlerService.GetHandlerInstance<MusicService>();
        public InteractionService InteractionService
            => HandlerService.GetHandlerInstance<InteractionService>();

        public bool CanUseLockedCommands
            => ((Context.User as IGuildUser).RoleIds.Any(x =>
                    x == 639547493767446538 || // dev
                    x == 563030072026595339 || // staff
                    x == 620312075863851039))  // dj )
              || (Context.User as SocketGuildUser)?.VoiceChannel.Users.Count <= 2;

        public bool InVoiceChannel
            => (Context.User as SocketGuildUser).VoiceChannel != null;

        private async Task InternalQueue(MusicPlayer mp, SongInfo songInfo, bool silent, bool queueFirst = false, bool forcePlay = false)
        {
            if (songInfo == null)
            {
                if (!silent)
                    await Context.SendErrorAsync("No song found.").ConfigureAwait(false);
                return;
            }

            int index;
            try
            {
                index = queueFirst
                    ? mp.EnqueueNext(songInfo, forcePlay)
                    : mp.Enqueue(songInfo, forcePlay);
            }
            catch (QueueFullException)
            {
                await Context.SendErrorAsync($"The queue is full at {mp.MaxQueueSize}/{mp.MaxQueueSize}");
                throw;
            }
            if (index != -1)
            {
                if (!silent)
                {
                    try
                    {
                        var embed = new EmbedBuilder().WithColor(Color.Green)
                                        .WithAuthor(eab => eab.WithName($"Queued song #{index + 1}").WithMusicIcon())
                                        .WithDescription($"{songInfo.PrettyName}\nQueue ")
                                        .WithFooter(ef => ef.WithText(songInfo.PrettyProvider));

                        if (Uri.IsWellFormedUriString(songInfo.Thumbnail, UriKind.Absolute))
                            embed.WithThumbnailUrl(songInfo.Thumbnail);

                        if (Context.IsInteraction)
                            await FollowupAsync(embed: embed.Build()).ConfigureAwait(false);
                        else
                            await Context.ReplyAsync(embed: embed.Build()).ConfigureAwait(false);
                        if (mp.Stopped)
                        {
                            await Context.SendErrorAsync("Player is stopped. Use =play command to start playing.").ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        private async Task InternalQueue(MusicPlayer mp, IAsyncEnumerable<SongInfo> songInfos, bool silent, bool queueFirst = false, bool forcePlay = false)
        {

        }

        private async Task InternalPlay(string query, bool forceplay)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var songInfo = await MusicService.ResolveSong(query, Context.User).ConfigureAwait(false);
            try { await InternalQueue(mp, songInfo, false, forcePlay: forceplay).ConfigureAwait(false); } catch (QueueFullException) { return; }
        }

        [Command("play", RunMode = RunMode.Async), Alias("p")]
        public async Task Play([Remainder] string query = null)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(query))
            {
                if (Context.Message?.Attachments.Any() ?? false)
                {
                    await PlayFile();
                    return;
                }

                await Context.SendErrorAsync("Please specify a name or url of a song", true, Context.Message?.Reference);
            }
            else if (int.TryParse(query, out var index))
                if (index >= 1)
                    mp.SetIndex(index - 1);
                else
                    return;
            else
            {
                await Context.DeferAsync();

                try
                {
                    await InternalPlay(query, forceplay: true).ConfigureAwait(false);
                }
                catch { }
            }
        }

        [Command("playfile", RunMode = RunMode.Async)]
        public async Task PlayFile()
        {
            if (!Context.Message.Attachments.Any())
            {
                await Context.Channel.SendErrorAsync("Please provide some audio files!");
                return;
            }

            var client = new WebClient();

            List<string> goodFiles = new();
            List<string> badFiles = new();

            foreach (var item in Context.Message.Attachments)
            {
                var name = $"{Guid.NewGuid().ToString().Replace("-", "")}-{item.Filename}";

                client.DownloadFile(item.ProxyUrl, $"data/musicdata/{name}");

                if(!FFMPEG.IsAudioFile($"data/musicdata/{name}"))
                {
                    badFiles.Add(item.Filename);
                }
                else
                {
                    goodFiles.Add(name);
                }
            }

            if (!goodFiles.Any())
            {
                await Context.Channel.SendErrorAsync("The file(s) you provided we're not audio files!");
                return;
            }

            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            foreach(var item in goodFiles)
            {
                var song = await MusicService.ResolveSong(item, Context.User, MusicType.Local).ConfigureAwait(false);
                await InternalQueue(mp, song, true).ConfigureAwait(false);
            }

            var optS = goodFiles.Count > 1 ? "s" : "";
            var optB = badFiles.Count > 1 ? "s" : "";

            var embed = new EmbedBuilder()
                .WithTitle($"Queued {goodFiles.Count} file{optS}")
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            embed.AddField($"Queued file{optS}", string.Join("\n", goodFiles.Select(x => $"`{x.Replace(x.Split('-').First(), "")}`")));

            if (badFiles.Any())
            {
                embed.AddField($"Unqueued file{optB}", string.Join("\n", badFiles.Select(x => $"`{x}`")));
            }

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("join", RunMode = RunMode.Async), Alias("j"), RequireDJ]
        public async Task Join()
        {
            var vc = (Context.User as SocketGuildUser).VoiceChannel;
            if (vc == null)
            {
                await Context.SendErrorAsync("You must be in a voice channel!", true, Context.Message.Reference);
                return;
            }

            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            await mp.JoinAsync(vc);

            if (Context.IsInteraction)
                await Context.Interaction.RespondAsync($"{new Emoji("👋")} <#{vc.Id}>");
            else
                await Context.Message.AddReactionAsync((Emoji)"👍");
        }

        [Command("queue"), Alias("q")]
        public async Task Queue([Remainder] string query = null)
        {
            if (query == null)
            {
                await ListQueue();
                return;
            }    

            await Context.DeferAsync();
            await InternalPlay(query, forceplay: false);
        }

        [Command("queuenext"), Alias("qn", "queue-next"), RequireDJ]
        public async Task QueueNext([Remainder] string query)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var songInfo = await MusicService.ResolveSong(query, Context.User).ConfigureAwait(false);
            try { await InternalQueue(mp, songInfo, false, true).ConfigureAwait(false); } catch (QueueFullException) { return; }
        }

        [Command("search", RunMode = RunMode.Async), Alias("s")]
        public async Task QueueSearch([Remainder] string query)
        {
            var videos = (await MusicService.Google.GetVideoInfosByKeywordAsync(query, 10).ConfigureAwait(false))
                .ToArray();

            if (!videos.Any())
            {
                await Context.SendErrorAsync($"No song found related to \"{query}\"", true).ConfigureAwait(false);
                return;
            }

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("queue-search")
                .WithPlaceholder("Select an item to add to the queue")
                .WithMaxValues(10)
                .WithMinValues(1);

            for(int i = 0; i != videos.Length; i++)
            {
                var vid = videos.ElementAt(i);
                selectMenu.AddOption(vid.Name.TrimTo(100), $"{i}");
            }

            var msg = await Context.ReplyAsync(embed: new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("Select a song")
                .WithDescription("Select a song to add to a queue, you can select more than one song.")
                .WithFields(videos.Select(x => new EmbedFieldBuilder().WithName(x.Name).WithValue(x.Url))).Build(),
                component: new ComponentBuilder().WithSelectMenu(selectMenu).Build()).ConfigureAwait(false);

            if (msg == null)
                msg = await Context.Interaction.GetOriginalResponseAsync();

            try
            {
                var comp = await InteractionService.NextSelection(msg, Context.User);
                //try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }

                //await comp.DeferLoadingAsync();

                List<SongInfo> infos = new List<SongInfo>();

                var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

                var queries = comp.Data.Values.Select(x => int.Parse(x)).Select(x => videos[x].Url);

                var tasks = queries.Select(x => MusicService.ResolveSong(query, Context.User));

                await comp.DeferAsync();

                var songInfos = await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var songInfo in songInfos)
                {
                    try 
                    { 
                        await InternalQueue(mp, songInfo, true, false).ConfigureAwait(false); 
                        infos.Add(songInfo); 
                    } 
                    catch (QueueFullException) { }

                }

                var embed = new EmbedBuilder()
                    .WithTitle("Queued songs")
                    .WithDescription(string.Join("\n\n", infos.Select((v, i) => $"`{i + mp.Current.Index}.` {v.PrettyFullName}")))
                    .WithColor(Color.Green);

                await comp.Message.ModifyAsync((p) => { p.Embed = embed.Build(); p.Components = null; });
            }
            catch(Exception x)
            {
                Logger.Warn(x, Severity.Music);
            }
        }

        [Command("listqueue"), Alias("lq", "queuelist", "ql")]
        public async Task ListQueue(int page = 0)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var (current, songs) = mp.QueueArray();

            if (!songs.Any())
            {
                await Context.SendErrorAsync("No active music player.").ConfigureAwait(false);
                return;
            }

            if (--page < -1)
                return;

            try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }

            const int itemsPerPage = 10;

            if (page == -1)
                page = current / itemsPerPage;

            //if page is 0 (-1 after this decrement) that means default to the page current song is playing from
            var total = mp.TotalPlaytime;
            var totalStr = total == TimeSpan.MaxValue ? "∞" : $"{(int)total.TotalHours}h {total.Minutes}m {total.Seconds}s";
            var maxPlaytime = mp.MaxPlaytimeSeconds;

            EmbedBuilder printAction(int curPage)
            {
                var startAt = itemsPerPage * curPage;
                var number = 0 + startAt;
                var desc = string.Join("\n", songs
                        .Skip(startAt)
                        .Take(itemsPerPage)
                        .Select(v =>
                        {
                            if (number++ == current)
                                return $"**⇒**`{number}.` {v.PrettyFullName}";
                            else
                                return $"`{number}.` {v.PrettyFullName}";
                        }));

                desc = $"`🔊` {songs[current].PrettyFullName}\n\n" + desc;

                var add = "";
                if (mp.Stopped)
                    add += Format.Bold($"Player is stopped. Use =play command to start playing.") + "\n";
                var mps = mp.MaxPlaytimeSeconds;
                if (mps > 0)
                    add += Format.Bold($"Songs will skip after {TimeSpan.FromSeconds(mps).ToString("HH\\:mm\\:ss")}") + "\n";
                if (mp.RepeatCurrentSong)
                    add += "🔂 " + "Repeating current song" + "\n";
                else if (mp.Shuffle)
                    add += "🔀 " + "Shuffling songs" + "\n";
                else
                {
                    if (mp.Autoplay)
                        add += "↪ " + "Auto-playing." + "\n";
                    if (mp.FairPlay && !mp.Autoplay)
                        add += " " + "Fairplay" + "\n";
                    else if (mp.RepeatPlaylist)
                        add += "🔁 " + "Repeating playlist" + "\n";
                }

                if (!string.IsNullOrWhiteSpace(add))
                    desc = add + "\n" + desc;

                var embed = new EmbedBuilder()
                    .WithAuthor(eab => eab.WithName($"Player queue - Page {curPage + 1}/{(songs.Length / itemsPerPage) + 1}")
                        .WithMusicIcon())
                    .WithDescription(desc)
                    .WithFooter(ef => ef.WithText($"{mp.PrettyVolume} | {songs.Length} " +
                                                  $"{("tracks".SnPl(songs.Length))} | {totalStr}"))
                    .WithColor(Color.Green);

                return embed;
            }

            await InteractionService.SendButtonPaginator(Context, Context.User, page, printAction, songs.Length,
                itemsPerPage).ConfigureAwait(false);
        }

        [Command("next"), Alias("n", "skip", "s")]
        public async Task Next(int skipCount = 1)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            if (!CanUseLockedCommands && InVoiceChannel)
            {
                int users = (Context.User as SocketGuildUser).VoiceChannel.Users.Count(x => !x.IsBot);

                // 51% of users are requred to vote

                int required = checked(Convert.ToInt32(Math.Ceiling(users * 0.51d)));

                if (users == required && users != 2)
                {
                    await Skip(skipCount, mp);
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithAuthor($"{Context.User} has started a vote!", Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                    .WithColor(Color.Green)
                    .WithDescription($"{Context.User.Username} want's to vote to skip {mp.Current.Current.PrettyName}")
                    .AddField("Voters", Context.User.Mention)
                    .WithFooter($"1/{required} votes");

                void OnChange(int cur, List<IGuildUser> voters, EmbedBuilder builder)
                {
                    builder.Fields[0].Value = $"{string.Join("\n", voters.Select(x => x.Mention))}";
                    builder.Footer.Text = $"{cur}/{required} votes";
                };

                await InteractionService.CreateVoteComponentsAsync(Context, embed, Context.User as SocketGuildUser, required, OnChange, async (msg) => 
                {
                    var completeEmbed = new EmbedBuilder()
                        .WithAuthor($"{Context.User} has started a vote!", Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                        .WithColor(Color.Green)
                        .WithDescription($"Vote complete!")
                        .WithFooter($"{required}/{required} votes");

                    await msg.ModifyAsync(x => 
                    {
                        var btn = ((msg.Components.First() as ActionRowComponent).Components.First() as ButtonComponent).ToBuilder();

                        x.Embed = completeEmbed.Build();
                        x.Components = new ComponentBuilder()
                            .WithButton(btn.WithDisabled(true))
                            .Build();
                    });

                    await Skip(skipCount, mp);
                });
            }
            else
                await Skip(skipCount, mp);
        }

        private async Task Skip(int sk, MusicPlayer mp)
        {
            if (sk < 1)
            {
                await Context.SendErrorAsync("Skip count must be greater or equal to 1", true, Context.Message.Reference);
                return;
            }

            await Context.DeferAsync();

            mp.Next(sk);

            if (Context.IsInteraction)
                await Context.Interaction.FollowupAsync($"👍 Skipped {sk} song{(sk > 1 ? "s" : "")}.");
            else
                await Context.Message.AddReactionAsync((Emoji)"👍");
        }

        [Command("stop"), Alias("s"), RequireDJ]
        public async Task Stop()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            mp.Stop();

            await Context.SendSuccessAsync($"Music playback stopped and queue was cleared");
        }

        [Command("destroy"), Alias("dc", "leave", "fuckoff", "disconnect"), RequireDJ]
        public async Task Destroy()
        {
            await MusicService.DestroyPlayer(Context.Guild.Id).ConfigureAwait(false);

            if (Context.Guild.CurrentUser.VoiceChannel != null)
            {
                await Context.Guild.CurrentUser.ModifyAsync(x => x.Channel = null);
                await Context.Guild.CurrentUser.VoiceChannel.DisconnectAsync();
            }

            if (Context.IsInteraction)
                await Context.Interaction.RespondAsync("👍");
            else
                await Context.Message.AddReactionAsync((Emoji)"👍");
        }

        [Command("pause"), Alias("ps"), RequireDJ]
        public async Task Pause()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            if (mp.Paused)
            {
                await Context.SendErrorAsync("Music player already paused!", true, Context.Message.Reference);
                return;
            }

            mp.Pause();

            if (Context.IsInteraction)
                await Context.Interaction.RespondAsync("👍");
            else
                await Context.Message.AddReactionAsync((Emoji)"👍");
        }

        [Command("unpause"), Alias("up", "resume", "rs"), RequireDJ]
        public async Task Unpause()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            if (!mp.Paused)
            {
                await Context.SendErrorAsync("Music player isn't paused!", true, Context.Message.Reference);
                return;
            }

            mp.Unpause();

            if (Context.IsInteraction)
                await Context.Interaction.RespondAsync("👍");
            else
                await Context.Message.AddReactionAsync((Emoji)"👍");
        }

        [Command("volume"), Alias("v"), RequireDJ]
        public async Task Volume(int value)
        {
            if (value < 0 || value > 100)
            {
                await Context.SendErrorAsync("Volume must be between 0 and 100").ConfigureAwait(false);
                return;
            }

            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            mp.SetVolume(value);

            await Context.SendSuccessAsync($"Volume set to {value}%").ConfigureAwait(false);
        }

        [Command("songremove"), Alias("srm", "rm", "qr", "r", "remove", "remove-song"), Priority(1)]
        public async Task SongRemove(int index)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            if (index < 1)
            {
                await Context.SendErrorAsync("Song on that index doesn't exist", true).ConfigureAwait(false);
                return;
            }

            if (!CanUseLockedCommands && InVoiceChannel)
            {
                int users = (Context.User as SocketGuildUser).VoiceChannel.Users.Count(x => !x.IsBot);

                // 51% of users are requred to vote

                int required = checked(Convert.ToInt32(Math.Ceiling(users * 0.51d)));

                if (users <= required && users != 2)
                {
                    await SongRemoveInternal(index, mp);
                    return;
                }

                var currentSongs = mp.QueueArray();

                if(currentSongs.Songs.Length <= index)
                {
                    await Context.ReplyAsync($"No song at index #{index}", ephemeral: true);
                    return;
                }

                var song = currentSongs.Songs[index];

                var embed = new EmbedBuilder()
                    .WithAuthor($"{Context.User} has started a vote!", Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                    .WithColor(Color.Orange)
                    .WithDescription($"{Context.User.Username} want's to vote to remove the song {song.PrettyName}")
                    .AddField("Voters", Context.User.Mention)
                    .WithFooter($"1/{required} votes");

                void OnChange(int cur, List<IGuildUser> voters, EmbedBuilder builder)
                {
                    builder.Fields[0].Value = $"{string.Join("\n", voters.Select(x => x.Mention))}";
                    builder.Footer.Text = $"{cur}/{required} votes";
                };

                await InteractionService.CreateVoteComponentsAsync(Context, embed, Context.User as SocketGuildUser, required, OnChange, async (msg) =>
                {
                    var completeEmbed = new EmbedBuilder()
                        .WithAuthor($"{Context.User} has started a vote!", Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                        .WithColor(Color.Orange)
                        .WithDescription($"Vote complete!")
                        .WithFooter($"{required}/{required} votes");

                    await msg.ModifyAsync(x =>
                    {
                        var btn = ((msg.Components.First() as ActionRowComponent).Components.First() as ButtonComponent).ToBuilder();

                        x.Embed = completeEmbed.Build();
                        x.Components = new ComponentBuilder()
                            .WithButton(btn.WithDisabled(true))
                            .Build();
                    });

                    await SongRemoveInternal(index, mp);
                });
            }
            else
                await SongRemoveInternal(index, mp);
        }

        private async Task SongRemoveInternal(int index, MusicPlayer mp)
        {
            
            try
            {
                var song = mp.RemoveAt(index - 1);
                var embed = new EmbedBuilder()
                            .WithAuthor(eab => eab.WithName($"Removed song #{index}").WithMusicIcon())
                            .WithDescription(song.PrettyName)
                            .WithFooter(ef => ef.WithText(song.PrettyInfo))
                            .WithColor(Color.Red);

                await Context.ReplyAsync(embed: embed.Build()).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException)
            {
                await Context.SendErrorAsync("Song on that index doesn't exist").ConfigureAwait(false);
            }
        }

        public enum All { All }
       
        [Command("songremove"), Alias("srm", "rm", "qr", "r", "remove"), Priority(0), RequireDJ]
        public async Task SongRemove(All _)
        {
            var mp = MusicService.GetPlayerOrDefault(Context.Guild.Id);
            if (mp == null)
                return;
            mp.Stop(true);
            await Context.SendSuccessAsync("Music queue cleared.").ConfigureAwait(false);
        }

        [Command("fairplay"), Alias("fp"), RequireDJ]
        public async Task Fairplay()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var val = mp.FairPlay = !mp.FairPlay;

            if (val)
            {
                await Context.SendSuccessAsync("Fair play enabled.").ConfigureAwait(false);
            }
            else
            {
                await Context.SendSuccessAsync("Fair play disabled.").ConfigureAwait(false);
            }
        }

        // depricated because of soundcloud api
        //[Command("soundcloudqueue"), Alias("sq")]
        public async Task SoundCloudQueue([Remainder] string query)
        {
            await Context.DeferAsync();
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var song = await MusicService.ResolveSong(query, Context.User, MusicType.Soundcloud).ConfigureAwait(false);
            await InternalQueue(mp, song, false).ConfigureAwait(false);
        }

        [Command("nowplaying"), Alias("np", "current", "currentlyplaying", "currentsong", "cs", "now-playing")]
        public async Task NowPlaying()
        {
            await Context.DeferAsync();
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var (_, currentSong) = mp.Current;
            if (currentSong == null)
            {
                await Context.SendErrorAsync("No songs currently playing");
            }    
            try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }

            var embed = new EmbedBuilder().WithColor(Color.Green)
                            .WithAuthor(eab => eab.WithName("Now playing").WithMusicIcon())
                            .WithDescription(currentSong.PrettyName)
                            .WithThumbnailUrl(currentSong.Thumbnail)
                            .WithFooter(ef => ef.WithText(mp.PrettyVolume + " | " + mp.PrettyFullTime + $" | {currentSong.PrettyProvider} | {currentSong.Queuer.Username}"));

            await Context.ReplyAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("shuffle"), Alias("sh"), RequireDJ]
        public async Task ShufflePlaylist()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var val = mp.ToggleShuffle();
            if (val)
                await Context.SendSuccessAsync("Songs will shuffle from now on.").ConfigureAwait(false);
            else
                await Context.SendSuccessAsync("Songs will no longer shuffle.").ConfigureAwait(false);
        }

        [Command("autoplay"), Alias("ap"), RequireDJ]
        public async Task Autoplay()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            if (!mp.ToggleAutoplay())
                await Context.SendSuccessAsync("Autoplay disabled.").ConfigureAwait(false);
            else
                await Context.SendSuccessAsync("Autoplay enabled.").ConfigureAwait(false);
        }
    }
}
