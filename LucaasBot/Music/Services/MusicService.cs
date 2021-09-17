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

        public override void Initialize(DiscordSocketClient client)
        {
            _client = client;
            _sc = new SoundCloudApiService();
            Google = new GoogleApiService(new HttpClientFactory());

            try { Directory.Delete(MusicDataPath, true); } catch { }
            Directory.CreateDirectory(MusicDataPath);
        }

        public Task<MusicPlayer> GetOrCreatePlayer(ICommandContext context)
        {
            var gUsr = (IGuildUser)context.User;
            var txtCh = (ITextChannel)context.Channel;
            var vCh = gUsr.VoiceChannel;
            return GetOrCreatePlayer(context.Guild.Id, vCh, txtCh);
        }

        public async Task<MusicPlayer> GetOrCreatePlayer(ulong guildId, IVoiceChannel voiceCh, ITextChannel textCh)
        {
            if (voiceCh == null || voiceCh.Guild != textCh.Guild)
            {
                if (textCh != null)
                {
                    await textCh.SendErrorAsync("You must be in a voice channel to play music!").ConfigureAwait(false);
                }

                return null;
            }

            return MusicPlayers.GetOrAdd(guildId, _ =>
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
                            .WithFooter(ef => ef.WithText(mp.PrettyVolume + " | " + song.Song.PrettyInfo)).Build())
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

            var si = await ResolveSong(related[new Random().Next(related.Length)], _client.CurrentUser.ToString(), MusicType.YouTube).ConfigureAwait(false);
            if (si == null)
                throw new SongNotFoundException();
            var mp = await GetOrCreatePlayer(txtCh.GuildId, vch, txtCh).ConfigureAwait(false);
            mp.Enqueue(si);
        }

        public async Task<SongInfo> ResolveSong(string query, string queuerName, MusicType? musicType = null)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentNullException(nameof(query));

            ISongResolverFactory resolverFactory = new SongResolverFactory(_sc);
            var strategy = await resolverFactory.GetResolveStrategy(query, musicType).ConfigureAwait(false);
            var sinfo = await strategy.ResolveSong(query).ConfigureAwait(false);

            if (sinfo == null)
                return null;

            sinfo.QueuerName = queuerName;

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
