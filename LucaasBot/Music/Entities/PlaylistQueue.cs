using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Music.Entities
{
    public class PlaylistQueue : IDisposable
    {
        public (int Index, SongInfo info) Current
            => (CurrentIndex, _songs.Current);

        public int Count;
        public int CurrentIndex;

        private IAsyncEnumerator<SongInfo> _songs;

        public async Task<SongInfo> NextAsync()
        {
            await _songs.MoveNextAsync();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
