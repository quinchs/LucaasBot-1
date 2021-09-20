using LucaasBot.Music.Entities;
using LucaasBot.Music.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace LucaasBot.Music
{
    public class YoutubeResolveStrategy : IResolveStrategy
    {
        private Regex playlistId = new Regex(@"/[&?]list=([^&]+)/i");

        public virtual async Task<SongInfo> ResolveSong(string query)
        {
            var plMatch = playlistId.Match(query);

            if (plMatch.Success)
            {
                var videos = ResolvePlaylistAsync(plMatch.Groups[1].Value);

                return new PlaylistInfo(videos);
            }
            else
            {
                return await ResolveInternalAsync(query);
            }
        }

        private async IAsyncEnumerable<SongInfo> ResolvePlaylistAsync(PlaylistId id)
        {
            var client = new YoutubeClient();

            var videos = client.Playlists.GetVideosAsync(id).GetAsyncEnumerator();

            while (await videos.MoveNextAsync())
                yield return await GetInfoWithYtExplode(client, videos.Current.Id);

            yield break;
        }

        private async Task<SongInfo> ResolveInternalAsync(string query)
        {
            try
            {
                return await ResolveWithYtExplode(query).ConfigureAwait(false);
            }
            catch (Exception ex) { Logger.Warn(ex, Severity.Music); }

            try
            {
                var s = await ResolveWithYtDl(query).ConfigureAwait(false);
                if (s != null)
                    return s;
            }
            catch (Exception ex) { Logger.Warn(ex, Severity.Music); }

            return null;
        }

        private async Task<SongInfo> ResolveWithYtExplode(string query)
        {
            var client = new YoutubeClient();

            Logger.Write("Searching for video", Severity.Music, Severity.Log);
            var video = await client.Search.GetVideosAsync(query).FirstOrDefaultAsync();

            if (video == null)
                return null;
            
            Logger.Write("Video found", Severity.Music, Severity.Log);

            return await GetInfoWithYtExplode(client, video.Id);
        }

        private async Task<SongInfo> GetInfoWithYtExplode(YoutubeClient client, VideoId id)
        {
            var streamInfo = await client.Videos.Streams.GetManifestAsync(id).ConfigureAwait(false);
            var stream = streamInfo
                .GetAudioStreams()
                .OrderByDescending(x => x.Bitrate)
                .FirstOrDefault();

            Logger.Write("Got stream info", Severity.Music, Severity.Log);

            if (stream == null)
                return null;

            var video = await client.Videos.GetAsync(id);

            return new SongInfo()
            {
                Provider = "YouTube",
                ProviderType = MusicType.YouTube,
                Query = "https://youtube.com/watch?v=" + video.Id,
                Thumbnail = video.Thumbnails.OrderByDescending(x => x.Resolution.Height).FirstOrDefault()?.Url,
                TotalTime = video.Duration ?? TimeSpan.Zero,
                Uri = async () =>
                {
                    await Task.Yield();
                    return stream.Url;
                },
                VideoId = video.Id,
                Title = video.Title,
            };
        }

        private async Task<SongInfo> ResolveWithYtDl(string query)
        {
            string[] data;
            try
            {
                var ytdl = new Ytdl();
                data = (await ytdl.GetDataAsync(query).ConfigureAwait(false)).Split('\n');

                if (data.Length < 6)
                {
                    Logger.Write("No song found. Data less than 6", Severity.Music, Severity.Log);
                    return null;
                }

                if (!TimeSpan.TryParseExact(data[4],
                    new[] {"ss", "m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss", "hhh\\:mm\\:ss"},
                    CultureInfo.InvariantCulture, out var time))
                    time = TimeSpan.FromHours(24);

                return new SongInfo()
                {
                    Title = data[0],
                    VideoId = data[1],
                    Uri = async () =>
                    {
                        var ytdlo = new Ytdl();
                        data = (await ytdlo.GetDataAsync(query).ConfigureAwait(false)).Split('\n');
                        if (data.Length < 6)
                        {
                            Logger.Write("No song found. Data less than 6", Severity.Music, Severity.Log);
                            return null;
                        }

                        return data[2];
                    },
                    Thumbnail = data[3],
                    TotalTime = time,
                    Provider = "YouTube",
                    ProviderType = MusicType.YouTube,
                    Query = "https://youtube.com/watch?v=" + data[1],
                };

            }
            catch (Exception ex)
            {
                Logger.Write(ex, Severity.Music, Severity.Critical);
                return null;
            }
        }
    }
}
