using LucaasBot.Music.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot.Music
{
    public class SpotifyResolveStrategy : YoutubeResolveStrategy, IResolveStrategy
    {
        public Regex TrackRegex = new Regex(@"<title>(.*?) - song by (.*?) \| Spotify<\/title>");
        private HttpClient Client = new HttpClient();
        private readonly Logger _log;

        public SpotifyResolveStrategy()
            : base() 
        {
            Client.DefaultRequestHeaders.Add("User-Agent", $"Lucaasbot/{Environment.Version} DiscordBot");
        }

        public static bool IsSpotifyLink(string query)
            => query.Contains("https://open.spotify.com/track");

        public override async Task<SongInfo> ResolveSong(string query)
        {
            if (!query.Contains("https://open.spotify.com/track"))
            {
                return null;
            }

            Logger.Write($"Getting track name from {query}", Severity.Music, Severity.Log);
            var response = await Client.GetAsync(query).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            var titleMatch = TrackRegex.Match(await response.Content.ReadAsStringAsync());

            if (!titleMatch.Success)
                return null;

            Logger.Write("Got song name", Severity.Music, Severity.Log);

            var info = await base.ResolveSong($"{titleMatch.Groups[1].Value} {titleMatch.Groups[2].Value}").ConfigureAwait(false);

            info.Provider = "Spotify";
            info.Title = $"{titleMatch.Groups[1].Value} {titleMatch.Groups[2].Value}";
            info.SongUrl = query;
            return info;
        }
    }
}
