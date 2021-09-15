using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Music.Entities
{
    public class MusicQueue : IDisposable
    {
        private LinkedList<SongInfo> Songs = new LinkedList<SongInfo>();
        private int _currentIndex = 0;

        private object lockObj = new object();

        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                lock (lockObj)
                {
                    if (Songs.Count == 0)
                        _currentIndex = 0;
                    else
                        _currentIndex = value %= Songs.Count;
                }
            }
        }

        public (int Index, SongInfo Song) Current
        {
            get
            {
                var cur = CurrentIndex;
                return (cur, Songs.ElementAtOrDefault(cur));
            }
        }

        private TaskCompletionSource<bool> NextSource { get; } = new TaskCompletionSource<bool>();

        public int Count
        {
            get
            {
                lock(lockObj)
                {
                    return Songs.Count;
                }
            }
        }

        private uint _maxQueueSize;

        public uint MaxQueueSize
        {
            get => _maxQueueSize;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                lock (lockObj)
                {
                    _maxQueueSize = value;
                }
            }
        }

        public void Add(SongInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            lock (lockObj)
            {
                if (MaxQueueSize != 0 && Songs.Count >= MaxQueueSize)
                    throw new ArgumentOutOfRangeException(nameof(info), "Queue is full");

                Songs.AddLast(info);
            }
        }

        public int AddNext(SongInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            lock (lockObj)
            {
                if (MaxQueueSize != 0 && Songs.Count >= MaxQueueSize)
                    throw new ArgumentOutOfRangeException(nameof(info), "Queue is full");


                var currentSong = Current.Song;

                if(currentSong == null)
                {
                    Songs.AddLast(info);
                    return Songs.Count;
                }

                var songList = Songs.ToList();
                songList.Insert(CurrentIndex + 1, info);
                Songs = new LinkedList<SongInfo>(songList);
                return CurrentIndex + 1;
            }
        }

        public void Next(int skipCount = 1)
        {
            lock (lockObj)
                CurrentIndex += skipCount;
        }

        public SongInfo RemoveAt(int index)
        {
            lock (lockObj)
            {
                if (index < 0 || index >= Songs.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var current = Songs.First.Value;

                for(int i = 0; i < Songs.Count; i++)
                {
                    if(i == index)
                    {
                        current = Songs.ElementAt(index);
                        Songs.Remove(current);
                        if(CurrentIndex != 0 && CurrentIndex >= index)
                        {
                            --CurrentIndex;
                        }
                        break;
                    }
                }

                return current;
            }
        }

        public void Clear()
        {
            lock (lockObj)
            {
                Songs.Clear();
                CurrentIndex = 0;
            }
        }

        public (int CurrentIndex, SongInfo[] Songs) ToArray()
        {
            lock (lockObj)
            {
                return (CurrentIndex, Songs.ToArray());
            }
        }

        public List<SongInfo> ToList()
        {
            lock (lockObj)
            {
                return Songs.ToList();
            }
        }

        public void Random()
        {
            lock (lockObj)
            {
                var r = new Random();
                CurrentIndex = r.Next(Songs.Count);
            }
        }

        public SongInfo MoveSong(int n1, int n2)
        {
            lock (lockObj)
            {
                var currentSong = Current.Song;
                var playlist = Songs.ToList();
                if (n1 >= playlist.Count || n2 >= playlist.Count || n1 == n2)
                    return null;

                var s = playlist[n1];

                playlist.RemoveAt(n1);
                playlist.Insert(n2, s);

                Songs = new LinkedList<SongInfo>(playlist);


                if (currentSong != null)
                    CurrentIndex = playlist.IndexOf(currentSong);

                return s;
            }
        }

        public void RemoveSong(SongInfo song)
        {
            lock (lockObj)
            {
                Songs.Remove(song);
            }
        }

        public bool IsLast()
        {
            lock (lockObj)
                return CurrentIndex == Songs.Count - 1;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
