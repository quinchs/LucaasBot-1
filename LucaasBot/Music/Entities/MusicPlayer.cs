using Discord;
using Discord.Audio;
using LucaasBot.Music.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LucaasBot.Music.Entities
{
    public class MusicPlayer
    {
        const int _frameBytes = 3840;
        const float _miliseconds = 20.0f;

        public event Action<MusicPlayer, (int Index, SongInfo Song)> OnStarted;
        public event Action<MusicPlayer, SongInfo> OnCompleted;
        public event Action<MusicPlayer, bool> OnPauseChanged;

        public IVoiceChannel VoiceChannel;
        public IGuild Guild;
        public ITextChannel TextChannel;
        public TimeSpan CurrentTime => TimeSpan.FromSeconds(_bytesSent / (float)_frameBytes / (1000 / _miliseconds));
       
        public bool Exited { get; set; } = false;
        public bool Stopped { get; set; } = false;
        public float Volume { get; set; } = 1.0f;
        public bool Paused => PauseTaskSource != null;
        public string PrettyFullTime => PrettyCurrentTime + " / " + (Queue.Current.Song?.PrettyTotalTime ?? "?");
        public bool RepeatCurrentSong { get; private set; }
        public bool Shuffle { get; private set; }
        public bool Autoplay { get; private set; }
        public bool RepeatPlaylist { get; private set; } = false;
        public bool AutoDelete { get; set; }
        public uint MaxPlaytimeSeconds { get; set; }

        private readonly Task PlayerTask;
        private MusicQueue Queue { get; } = new MusicQueue();
        private TaskCompletionSource<bool> PauseTaskSource { get; set; } = null;
        private CancellationTokenSource SongCancelSource { get; set; }
        private bool _fairPlay;
        private int _bytesSent = 0;
        private IAudioClient _audioClient;
        private readonly object locker = new object();
        private MusicService _musicService;
        private bool manualSkip = false;
        private bool manualIndex = false;
        private bool newVoiceChannel = false;
        private readonly IGoogleApiService _google;

        private bool cancel = false;

        public string PrettyVolume => $"🔉 {(int)(Volume * 100)}%";
        public string PrettyCurrentTime
        {
            get
            {
                var time = CurrentTime.ToString(@"mm\:ss");
                var hrs = (int)CurrentTime.TotalHours;

                if (hrs > 0)
                    return hrs + ":" + time;
                else
                    return time;
            }
        }
        public (int Index, SongInfo Current) Current
        {
            get
            {
                if (Stopped)
                    return (0, null);
                return Queue.Current;
            }
        }
        public uint MaxQueueSize
        {
            get => Queue.MaxQueueSize;
            set { lock (locker) Queue.MaxQueueSize = value; }
        }
        public bool FairPlay
        {
            get => _fairPlay;
            set
            {
                if (value)
                {
                    var (Index, Song) = Queue.Current;
                    if (Song != null)
                        RecentlyPlayedUsers.Add(Song.QueuerName);
                }
                else
                {
                    RecentlyPlayedUsers.Clear();
                }

                _fairPlay = value;
            }
        }
    }
}
