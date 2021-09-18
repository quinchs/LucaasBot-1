using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LucaasBot.Music.Entities;
using LucaasBot.Music.Exceptions;
using LucaasBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Music.Services
{
    public class MusicService : DiscordHandler
    {
        public const string MusicDataPath = "data/musicdata";
        public const float DefaultMusicVolume = 0.5f;
        public GoogleApiService Google { get; private set; }
        public ConcurrentDictionary<ulong, MusicPlayer> MusicPlayers { get; } = new ConcurrentDictionary<ulong, MusicPlayer>();

        private SoundCloudApiService _sc;
        private DiscordSocketClient _client;

        public override async Task InitializeAsync(DiscordSocketClient client)
        {
            _client = client;
            _sc = new SoundCloudApiService();
            Google = new GoogleApiService(new HttpClientFactory());

            try { Directory.Delete(MusicDataPath, true); } catch { }
            Directory.CreateDirectory(MusicDataPath);

            _ = Task.Run(async () => await PrecheckVoiceState());
        }

        private async Task PrecheckVoiceState()
        {
            // since when the bot restarts and the bot is in a voice channel the player freaks out and dies. Lets just dc ourself if were in a voice channel
            var currentUser = _client.GetGuild(464733888447643650).CurrentUser;

            if (currentUser.VoiceChannel != null)
            {
                await currentUser.VoiceChannel.DisconnectAsync();
                await currentUser.ModifyAsync(x => x.Channel = null);
            }
        }

        public Task<MusicPlayer> GetOrCreatePlayer(ICommandContext context)
        {
            var gUsr = (IGuildUser)context.User;
            var txtCh = (ITextChannel)context.Channel;
            var vCh = gUsr.VoiceChannel;
            return GetOrCreatePlayer(context.Guild.Id, vCh, txtCh, context);
        }

        public async Task<MusicPlayer> GetOrCreatePlayer(ulong guildId, IVoiceChannel voiceCh, ITextChannel textCh, ICommandContext context)
        {
            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (textCh != null)
                {
                    await textCh.SendErrorAsync("You must be in a voice channel to play music!").ConfigureAwait(false);
                }

                return null;
            }

            var player = MusicPlayers.GetOrAdd(guildId, _ =>
            {
                var vol = DefaultMusicVolume;

                var mp = new MusicPlayer(_client, this, Google, voiceCh, textCh, vol);

                IUserMessage playingMessage = null;
                IUserMessage lastFinishedMessage = null;

                mp.OnCompleted += async (s, song) =>
                {
                    try
                    {
                        lastFinishedMessage?.DeleteAfter(0);

                        try
                        {
                            lastFinishedMessage = await mp.TextChannel.SendMessageAsync(embed: new EmbedBuilder().WithColor(Color.Green)
                                .WithAuthor(eab => eab.WithName("Finished song").WithMusicIcon())
                                .WithDescription(song.PrettyName)
                                .WithFooter(ef => ef.WithText(song.PrettyInfo)).Build())
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }

                        var (Index, Current) = mp.Current;
                    }
                    catch
                    {
                        // ignored
                    }
                };
                mp.OnStarted += async (player, song) =>
                {
                    //try { await mp.UpdateSongDurationsAsync().ConfigureAwait(false); }
                    //catch
                    //{
                    //    // ignored
                    //}
                    var sender = player;
                    if (sender == null)
                        return;
                    try
                    {
                        playingMessage?.DeleteAfter(0);

                        playingMessage = await mp.TextChannel.SendMessageAsync(embed: new EmbedBuilder().WithColor(Color.Green)
                            .WithAuthor(eab => eab.WithName($"Playing song #{song.Index + 1}").WithMusicIcon())
                            .WithDescription(song.Song.PrettyName)
                            .WithFooter(mp.PrettyVolume + " | " + song.Song.PrettyInfo, song.Song.Queuer.GetAvatarUrl() ?? song.Song.Queuer.GetDefaultAvatarUrl()).Build())
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // ignored
                    }
                };
                Logger.Log("Done creating", Severity.Music);
                return mp;
            });

            //if(context != null)
            //    await player.UpdateFromContext(context);

            return player;
        }


        public MusicPlayer GetPlayerOrDefault(ulong guildId)
        {
            if (MusicPlayers.TryGetValue(guildId, out var mp))
                return mp;
            else
                return null;
        }

        public async Task TryQueueRelatedSongAsync(SongInfo song, ITextChannel txtCh, IVoiceChannel vch)
        {
            var related = (await Google.GetRelatedVideosAsync(song.VideoId, 4).ConfigureAwait(false)).ToArray();
            if (!related.Any())
                return;

            var si = await ResolveSong(related[new Random().Next(related.Length)], _client.CurrentUser, MusicType.YouTube).ConfigureAwait(false);
            if (si == null)
                throw new SongNotFoundException();
            var mp = await GetOrCreatePlayer(txtCh.GuildId, vch, txtCh, null).ConfigureAwait(false);
            mp.Enqueue(si);
        }

        public async Task<SongInfo> ResolveSong(string query, IUser queuer, MusicType? musicType = null)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentNullException(nameof(query));

            ISongResolverFactory resolverFactory = new SongResolverFactory(_sc);
            var strategy = await resolverFactory.GetResolveStrategy(query, musicType).ConfigureAwait(false);
            var sinfo = await strategy.ResolveSong(query).ConfigureAwait(false);

            if (sinfo == null)
                return null;

            sinfo.Queuer = queuer;

            return sinfo;
        }

        public async Task DestroyAllPlayers()
        {
            foreach (var key in MusicPlayers.Keys)
            {
                await DestroyPlayer(key).ConfigureAwait(false);
            }
        }

        public async Task DestroyPlayer(ulong id)
        {
            if (MusicPlayers.TryRemove(id, out var mp))
                await mp.Destroy().ConfigureAwait(false);
        }
    }
}
