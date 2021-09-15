using LucaasBot.Music.Entities;
using System.Threading.Tasks;

namespace LucaasBot.Music
{
    public interface IResolveStrategy
    {
        Task<SongInfo> ResolveSong(string query);
    }
}
