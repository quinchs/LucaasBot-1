using LucaasBot.Music.Entities;
using System.Threading.Tasks;

namespace LucaasBot.Music
{
    public interface ISongResolverFactory
    {
        Task<IResolveStrategy> GetResolveStrategy(string query, MusicType? musicType);
    }
}
