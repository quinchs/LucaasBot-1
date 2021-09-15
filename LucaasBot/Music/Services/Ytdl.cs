using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Music.Services
{
    public class Ytdl
    {
        public async Task<string> GetDataAsync(string url)
        {
            // escape the minus on the video argument
            // to prevent youtube-dl to handle it like an argument
            if (url != null && url.StartsWith("-"))
                url = '\\' + url;


            using (Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "youtube-dl",
                    Arguments = $"-4 --geo-bypass -f bestaudio -e --get-url --get-id --get-thumbnail --get-duration --no-check-certificate --default-search \"ytsearch:\" \"{url}\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
            })
            {
                Logger.Debug($"Executing {process.StartInfo.FileName} {process.StartInfo.Arguments}");

                process.Start();
                var str = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                var err = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(err))
                    Logger.Warn(err);
                return str;
            }
        }
    }
}
