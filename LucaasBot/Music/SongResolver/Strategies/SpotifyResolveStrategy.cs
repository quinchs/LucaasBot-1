using LucaasBot.Music.Entities;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot.Music
{
    public class SpotifyResolveStrategy : YoutubeResolveStrategy, IResolveStrategy
    {
        private HttpClient Client = new HttpClient();
        private SpotifyApiService service = HandlerService.GetHandlerInstance<SpotifyApiService>();

        public SpotifyResolveStrategy()
            : base() 
        {
            Client.DefaultRequestHeaders.Add("User-Agent", $"Lucaasbot/{Environment.Version} DiscordBot");
        }

        public override async Task<SongInfo> ResolveSong(string query)
        {
            var tracks = await service.ResolveSongNamesAsync(query);

            Logger.Write("Got song name", Severity.Music, Severity.Log);

            if(tracks.Count == 1)
            {
                return await base.ResolveSong(tracks[0].Name);
            }
            else
            {
                var enmn = GetAsyncEnumerator(tracks);

                var info = await service.GetInfo(query);

                var duration = TimeSpan.FromMilliseconds(tracks.Select(x => x.DurationMs).Sum());

                info.TotalTime = duration;

                return info;
            }
        }

        private async IAsyncEnumerable<SongInfo> GetAsyncEnumerator(List<SimpleTrack> tracks)
        {
            foreach(var track in tracks)
            {
                yield return await base.ResolveSong(track.Name);
            }

            yield break;
        }
    }
}
