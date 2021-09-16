using LucaasBot.Music.Entities;
using LucaasBot.Music.Services;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LucaasBot.Music
{
    public class LocalSongResolveStrategy : IResolveStrategy
    {
        public Task<SongInfo> ResolveSong(string query)
        {
            var guid = query.Split('-').First();

            var path = Path.GetFullPath($"data/musicdata/{query}");
            var duration = FFMPEG.GetAudioDuration(path);

            return Task.FromResult(new SongInfo
            {
                Uri = () => Task.FromResult(Path.GetFullPath($"data/musicdata/{query}")),
                Title = Path.GetFileNameWithoutExtension(query.Replace(guid, "")),
                Provider = "Local File",
                ProviderType = MusicType.Local,
                Query = query.Replace(guid, ""),
                Thumbnail = "https://cdn.discordapp.com/attachments/155726317222887425/261850914783100928/1482522077_music.png",
                TotalTime = duration
            });
        }
    }
}
