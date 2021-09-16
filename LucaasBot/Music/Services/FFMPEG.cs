using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Music.Services
{
    public static class FFMPEG
    {
        public static TimeSpan GetAudioDuration(string path)
        {
            using (Process p = new Process())
            {
                var args = $" -i \"{path}\" -show_entries format=duration -v quiet -of csv=\"p = 0\"";

                p.StartInfo = new ProcessStartInfo()
                {
                    FileName = "ffprobe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                p.Start();

                var s = p.StandardOutput.ReadToEnd();

                if (!double.TryParse(s, out var lng))
                    return TimeSpan.Zero;

                return TimeSpan.FromSeconds(lng);
            }
        }

        public static bool IsAudioFile(string path)
        {
            using (Process p = new Process())
            {
                p.StartInfo = new ProcessStartInfo()
                {
                    FileName = "ffprobe",
                    Arguments = $"-loglevel error -show_entries stream=codec_type -of default=nw=1 {path}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                p.Start();

                var s = p.StandardOutput.ReadToEnd();

                return s.Contains("codec_type=audio");
            }
        }
    }
}
