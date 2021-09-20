using Discord.WebSocket;
using LucaasBot.Music.Entities;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot.Music
{
    public class SpotifyApiService : DiscordHandler 
    {
        public static readonly Regex SpotifyLinkRegex = new Regex(@"https:\/\/open\.spotify\.com\/(.*?)\/(.*?)(?>\?|$)");
        public static bool IsSpotifyLink(string q)
            => SpotifyLinkRegex.IsMatch(q);
        private DiscordSocketClient discordClient;
        private SpotifyClient spotifyClient;

        public override async Task InitializeAsync(DiscordSocketClient client)
        {
            try
            {
                this.discordClient = client;

                var config = SpotifyClientConfig.CreateDefault();

                var request = new ClientCredentialsRequest(ConfigService.Config.SpotifyClientId, ConfigService.Config.SpotifyClientSecret);
                var response = await new OAuthClient(config).RequestToken(request);


                spotifyClient = new SpotifyClient(config.WithToken(response.AccessToken));

            }
            catch (Exception)
            {
                Logger.Critical("Failed to generate credentials with spotify, please make sure that the config file contains the correct credentials.", Severity.Music);

                throw;
            }
        }

        public async Task<PlaylistInfo> GetInfo(string link)
        {
            if (string.IsNullOrEmpty(link))
                throw new ArgumentException("Not a spotify link", nameof(link));

            if (!link.StartsWith("https://open.spotify.com/"))
                throw new ArgumentException("Not a spotify link", nameof(link));

            var match = SpotifyLinkRegex.Match(link);

            if (!match.Success)
                throw new ArgumentException("Link didnt match spotify regex", nameof(link));

            switch (match.Groups[1].Value)
            {
                case "albumn":
                    {
                        var albumn = await spotifyClient.Albums.Get(match.Groups[2].Value);

                        return new PlaylistInfo()
                        {
                            Provider = "Spotify",
                            ProviderType = MusicType.Spotify,
                            Query = link,
                            Title = albumn.Name,
                            Uri = async () => albumn.Href,
                            Thumbnail = albumn.Images.OrderByDescending(x => x.Height).First().Url,
                            Label = albumn.Label,
                            Type = albumn.Type
                        };
                    }
                case "playlist":
                    {
                        var pl = await spotifyClient.Playlists.Get(match.Groups[2].Value);

                        return new PlaylistInfo()
                        {
                            Provider = "Spotify",
                            ProviderType = MusicType.Spotify,
                            Query = link,
                            Title = pl.Name,
                            Uri = async () => pl.Href,
                            Thumbnail = pl.Images.OrderByDescending(x => x.Height).First().Url,
                            Type = pl.Type,
                            Label = pl.Owner?.DisplayName
                        };
                    }

                default: return null;
            }
        }

        public async Task<List<SimpleTrack>> ResolveSongNamesAsync(string link)
        {
            if(string.IsNullOrEmpty(link))
                throw new ArgumentException("Not a spotify link", nameof(link));

            if (!link.StartsWith("https://open.spotify.com/"))
                throw new ArgumentException("Not a spotify link", nameof(link));

            var match = SpotifyLinkRegex.Match(link);

            if (!match.Success)
                throw new ArgumentException("Link didnt match spotify regex", nameof(link));

            List<SimpleTrack> tracksResult = new List<SimpleTrack>();

            switch (match.Groups[1].Value)
            {
                case "albumn":
                    {
                        var req = new AlbumTracksRequest() { Limit = 50, Offset = 0 };
                        string next = "";

                        while(next != null)
                        {
                            var tracks = await spotifyClient.Albums.GetTracks(match.Groups[2].Value, req);
                            req.Offset += 50;
                            tracksResult.AddRange(tracks.Items);
                            next = tracks.Next;
                        }
                    }
                    break;
                case "playlist":
                    {
                        var req = new PlaylistGetItemsRequest(PlaylistGetItemsRequest.AdditionalTypes.Track);
                        string next = "";

                        while(next != null)
                        {
                            var tracks = await spotifyClient.Playlists.GetItems(match.Groups[2].Value, req);
                            req.Offset += 100;
                            tracksResult.AddRange(tracks.Items.Select(x => (x.Track as SimpleTrack) ?? null));

                            tracksResult.RemoveAll(x => x == null);

                            next = tracks.Next;
                        }
                    }
                    break;
                case "track":
                    {
                        var track = await spotifyClient.Tracks.Get(match.Groups[2].Value);

                        tracksResult.Add(new SimpleTrack() 
                        {
                            Artists = track.Artists,
                            AvailableMarkets = track.AvailableMarkets,
                            DiscNumber = track.DiscNumber,
                            DurationMs = track.DurationMs,
                            Explicit = track.Explicit,
                            ExternalUrls = track.ExternalUrls,
                            Href = track.Href,
                            Id = track.Id,
                            IsPlayable = track.IsPlayable,
                            LinkedFrom = track.LinkedFrom,
                            Name = track.Name,
                            PreviewUrl = track.PreviewUrl,
                            TrackNumber = track.TrackNumber,
                            Type = track.Type,
                            Uri = track.Uri
                        });
                    }
                    break;

            }

            return tracksResult;
        }
    }
}
