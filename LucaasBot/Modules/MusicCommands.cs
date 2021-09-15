using Discord;
using Discord.Commands;
using LucaasBot.Music.Entities;
using LucaasBot.Music.Exceptions;
using LucaasBot.Music.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Modules
{
    [RequireContext(ContextType.Guild), RequireChannel(828687366893076480)] // music channel id.
    public class MusicCommands : ModuleBase<SocketCommandContext>
    {
        public MusicService MusicService
            => HandlerService.GetHandlerInstance<MusicService>();
        public InteractionService InteractionService
            => HandlerService.GetHandlerInstance<InteractionService>();

        private async Task InternalQueue(MusicPlayer mp, SongInfo songInfo, bool silent, bool queueFirst = false, bool forcePlay = false)
        {
            if (songInfo == null)
            {
                if (!silent)
                    await Context.Channel.SendErrorAsync("No song found.").ConfigureAwait(false);
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
                await Context.Channel.SendErrorAsync($"The queue is full at {mp.MaxQueueSize}/{mp.MaxQueueSize}");
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

                        var queuedMessage = await mp.TextChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
                        if (mp.Stopped)
                        {
                            await Context.Channel.SendErrorAsync("Player is stopped. Use =play command to start playing.").ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
        private async Task InternalPlay(string query, bool forceplay)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var songInfo = await MusicService.ResolveSong(query, Context.User.ToString()).ConfigureAwait(false);
            try { await InternalQueue(mp, songInfo, false, forcePlay: forceplay).ConfigureAwait(false); } catch (QueueFullException) { return; }
        }

        [Command("play")]
        public async Task Play([Remainder] string query = null)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(query))
            {
                await Next().ConfigureAwait(false);
            }
            else if (int.TryParse(query, out var index))
                if (index >= 1)
                    mp.SetIndex(index - 1);
                else
                    return;
            else
            {
                try
                {
                    await InternalPlay(query, forceplay: true).ConfigureAwait(false);
                }
                catch { }
            }
        }
        [Command("queue"), Alias("q")]
        public Task Queue([Remainder] string query)
            => InternalPlay(query, forceplay: false);

        [Command("queuenext"), Alias("qn")]
        public async Task QueueNext([Remainder] string query)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var songInfo = await MusicService.ResolveSong(query, Context.User.ToString()).ConfigureAwait(false);
            try { await InternalQueue(mp, songInfo, false, true).ConfigureAwait(false); } catch (QueueFullException) { return; }
        }

        [Command("queuesearch", RunMode = RunMode.Async), Alias("qs")]
        public async Task QueueSearch([Remainder] string query)
        {
            var videos = (await MusicService.Google.GetVideoInfosByKeywordAsync(query, 5).ConfigureAwait(false))
                .ToArray();

            if (!videos.Any())
            {
                await Context.Channel.SendErrorAsync("No song found.").ConfigureAwait(false);
                return;
            }

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("queue-search")
                .WithPlaceholder("Select an item to add to the queue")
                .WithMaxValues(1)
                .WithMinValues(1);

            for(int i = 0; i != videos.Length; i++)
            {
                var vid = videos.ElementAt(i);
                selectMenu.AddOption(vid.Name.TrimTo(100), $"{i}");
            }

            var msg = await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription(string.Join("\n", videos.Select((x, i) => $"`{i + 1}.`\n\t{Format.Bold(x.Name)}\n\t{x.Url}"))).Build(),
                component: new ComponentBuilder().WithSelectMenu(selectMenu).Build()).ConfigureAwait(false);

            try
            {
                var comp = await InteractionService.NextSelection(msg, Context.User);

                var index = int.Parse(comp.Data.Values.First());

                query = videos[index].Url;

                await Queue(query).ConfigureAwait(false);
            }
            finally
            {
                try { await msg.DeleteAsync().ConfigureAwait(false); } catch { }
            }
        }

        [Command("listqueue"), Alias("lq")]
        public async Task ListQueue(int page = 0)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var (current, songs) = mp.QueueArray();

            if (!songs.Any())
            {
                await Context.Channel.SendErrorAsync("No active music player.").ConfigureAwait(false);
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

            await InteractionService.SendButtonPaginator(Context.Channel, Context.User, page, printAction, songs.Length,
                itemsPerPage).ConfigureAwait(false);
        }

        [Command("next"), Alias("n")]
        public async Task Next(int skipCount = 1)
        {
            if (skipCount < 1)
                return;

            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);

            mp.Next(skipCount);
        }

        [Command("stop"), Alias("s")]
        public async Task Stop()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            mp.Stop();
        }

        [Command("destroy"), Alias("d")]
        public async Task Destroy()
        {
            await MusicService.DestroyPlayer(Context.Guild.Id).ConfigureAwait(false);
        }

        [Command("pause"), Alias("p")]
        public async Task Pause()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            mp.TogglePause();
        }

        [Command("volume"), Alias("v")]
        public async Task Volume(int val)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            if (val < 0 || val > 100)
            {
                await Context.Channel.SendErrorAsync("Volume must be between 0 and 100").ConfigureAwait(false);
                return;
            }
            mp.SetVolume(val);
            await Context.Channel.SendSuccessAsync($"Volume set to {val}%").ConfigureAwait(false);
        }

        [Command("songremove"), Alias("srm"), Priority(1)]
        public async Task SongRemove(int index)
        {
            if (index < 1)
            {
                await Context.Channel.SendErrorAsync("Song on that index doesn't exist").ConfigureAwait(false);
                return;
            }
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            try
            {
                var song = mp.RemoveAt(index - 1);
                var embed = new EmbedBuilder()
                            .WithAuthor(eab => eab.WithName($"Removed song #{index}").WithMusicIcon())
                            .WithDescription(song.PrettyName)
                            .WithFooter(ef => ef.WithText(song.PrettyInfo))
                            .WithColor(Color.Red);

                await mp.TextChannel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException)
            {
                await Context.Channel.SendErrorAsync("Song on that index doesn't exist").ConfigureAwait(false);
            }
        }

        public enum All { All }
       
        [Command("songremove"), Alias("srm"), Priority(0)]
        public async Task SongRemove(All _)
        {
            var mp = MusicService.GetPlayerOrDefault(Context.Guild.Id);
            if (mp == null)
                return;
            mp.Stop(true);
            await Context.Channel.SendSuccessAsync("Music queue cleared.").ConfigureAwait(false);
        }

        [Command("fairplay"), Alias("fp")]
        public async Task Fairplay()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var val = mp.FairPlay = !mp.FairPlay;

            if (val)
            {
                await Context.Channel.SendSuccessAsync("Fair play enabled.").ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendSuccessAsync("Fair play disabled.").ConfigureAwait(false);
            }
        }

        [Command("soundcloudqueue"), Alias("sq")]
        public async Task SoundCloudQueue([Remainder] string query)
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var song = await MusicService.ResolveSong(query, Context.User.ToString(), MusicType.Soundcloud).ConfigureAwait(false);
            await InternalQueue(mp, song, false).ConfigureAwait(false);
        }

        [Command("nowplaying"), Alias("np")]
        public async Task NowPlaying()
        {
            var mp = await MusicService.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var (_, currentSong) = mp.Current;
            if (currentSong == null)
                return;
            try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); } catch { }

            var embed = new EmbedBuilder().WithColor(Color.Green)
                            .WithAuthor(eab => eab.WithName("Now playing").WithMusicIcon())
                            .WithDescription(currentSong.PrettyName)
                            .WithThumbnailUrl(currentSong.Thumbnail)
                            .WithFooter(ef => ef.WithText(mp.PrettyVolume + " | " + mp.PrettyFullTime + $" | {currentSong.PrettyProvider} | {currentSong.QueuerName}"));

            await Context.Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("shuffle")]
        public async Task ShufflePlaylist()
        {
            var mp = await _service.GetOrCreatePlayer(Context).ConfigureAwait(false);
            var val = mp.ToggleShuffle();
            if (val)
                await ReplyConfirmLocalizedAsync("songs_shuffle_enable").ConfigureAwait(false);
            else
                await ReplyConfirmLocalizedAsync("songs_shuffle_disable").ConfigureAwait(false);
        }
    }
}
