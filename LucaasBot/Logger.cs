using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucaasBot
{
    public enum Severity
    {
        Debug,
        Log,
        Error,
        Warning,
        Socket,
        Rest,
        Critical,
        Core,
        Verbose,
        Music,
        CommandService,
    }
    public class Logger
    {

        public static void Error(object data, Severity? sev = null)
               => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Error } : new Severity[] { Severity.Error });
        public static void Log(object data, Severity? sev = null)
               => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Log  } : new Severity[] { Severity.Log });
        public static void Warn(object data, Severity? sev = null)
            => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Warning  } : new Severity[] { Severity.Warning });
        public static void Debug(object data, Severity? sev = null)
            => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Debug } : new Severity[] { Severity.Debug });

        public static void Write(object data, Severity sev = Severity.Log)
           => _logEvent?.Invoke(null, (data, new Severity[] { sev }));
        public static void Write(object data, params Severity[] sevs)
            => _logEvent?.Invoke(null, (data, sevs));

        private static ConcurrentQueue<KeyValuePair<object, Severity[]>> _queue = new ConcurrentQueue<KeyValuePair<object, Severity[]>>();
        private static event EventHandler<(object data, Severity[] sev)> _logEvent;

        static Logger()
        {
            _logEvent += Logger__logEvent;
        }

        private static void Logger__logEvent(object sender, (object data, Severity[] sev) e)
        {
            _queue.Enqueue(new KeyValuePair<object, Severity[]>(e.data, e.sev));
            if (_queue.Count > 0 && !inProg)
            {
                inProg = true;
                HandleQueueWrite();
            }
        }

        
        

        static bool inProg = false;
        private static Regex ColorRegex = new Regex(@"<(.*)>(.*?)<\/\1>");

        private static List<(ConsoleColor color, string value)> ProcessColors(string input)
        {
            var returnData = new List<(ConsoleColor color, string value)>();

            var mtch = ColorRegex.Matches(input);

            if (mtch.Count == 0)
            {
                returnData.Add((ConsoleColor.White, input));
                return returnData;
            }

            for (int i = 0; i != mtch.Count; i++)
            {
                var match = mtch[i];
                var color = GetColor(match.Groups[1].Value) ?? ConsoleColor.White;

                if (i == 0)
                {
                    if (match.Index != 0)
                    {
                        returnData.Add((ConsoleColor.White, new string(input.Take(match.Index).ToArray())));
                    }
                    returnData.Add((color, match.Groups[2].Value));
                }
                else
                {
                    var previousMatch = mtch[i - 1];
                    var start = previousMatch.Index + previousMatch.Length;
                    var end = match.Index;

                    returnData.Add((ConsoleColor.White, new string(input.Skip(start).Take(end - start).ToArray())));

                    returnData.Add((color, match.Groups[2].Value));
                }

                if (i + 1 == mtch.Count)
                {
                    // check remainder
                    if (match.Index + match.Length < input.Length)
                    {
                        returnData.Add((ConsoleColor.White, new string(input.Skip(match.Index + match.Length).ToArray())));
                    }
                }
            }

            return returnData;
        }
        private static ConsoleColor? GetColor(string tag)
        {
            if (Enum.TryParse(typeof(ConsoleColor), tag, true, out var res))
            {
                return (ConsoleColor)res;
            }
            else if (int.TryParse(tag, out var r))
            {
                return (ConsoleColor)r;
            }
            else return null;
        }

        private static Dictionary<Severity, ConsoleColor> SeverityColorParser = new Dictionary<Severity, ConsoleColor>()
        {
            { Severity.Log, ConsoleColor.Green },
            { Severity.Error, ConsoleColor.Red },
            { Severity.Warning, ConsoleColor.Yellow },
            { Severity.Critical, ConsoleColor.DarkRed },
            { Severity.Debug, ConsoleColor.Gray },
            { Severity.Core, ConsoleColor.Cyan },
            { Severity.Socket, ConsoleColor.Blue },
            { Severity.Rest, ConsoleColor.Magenta },
            { Severity.Verbose, ConsoleColor.DarkCyan },
            { Severity.Music, ConsoleColor.DarkMagenta },
            { Severity.CommandService, ConsoleColor.DarkBlue }
        };

        private static void HandleQueueWrite()
        {
            while (_queue.Count > 0)
            {
                if (_queue.TryDequeue(out var res))
                {
                    var sev = res.Value;
                    var data = res.Key;

                    var enumsWithColors = "";
                    foreach (var item in sev)
                    {
                        if (enumsWithColors == "")
                            enumsWithColors = $"<{(int)SeverityColorParser[item]}>{item}</{(int)SeverityColorParser[item]}>";
                        else
                            enumsWithColors += $" -> <{(int)SeverityColorParser[item]}>{item}</{(int)SeverityColorParser[item]}>";
                    }

                    var items = ProcessColors($"\u001b[38;5;249m{DateTime.UtcNow.ToString("O")} " + $"\u001b[1m[{enumsWithColors}]\u001b[0m - \u001b[37;1m{data}");

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Console.Write($"{string.Join("", items.Select(item => $"{ConsoleColorToANSI(item.color)}{item.value}\u001b[0m"))}");
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            Console.ForegroundColor = item.color;
                            Console.Write(item.value);
                        }
                    }

                    Console.Write("\n");
                }
            }
            inProg = false;
        }

        private static string ConsoleColorToANSI(ConsoleColor color)
        {
            int ansiConverter(ConsoleColor c) 
            {
                switch (c)
                {
                    case ConsoleColor.Black:
                        return 0;
                    case ConsoleColor.DarkRed:
                        return 1;
                    case ConsoleColor.DarkGreen:
                        return 2;
                    case ConsoleColor.DarkYellow:
                        return 3;
                    case ConsoleColor.DarkBlue:
                        return 4;
                    case ConsoleColor.DarkMagenta:
                        return 5;
                    case ConsoleColor.DarkCyan:
                        return 6;
                    case ConsoleColor.Gray:
                        return 7;
                    case ConsoleColor.DarkGray:
                        return 8;
                    case ConsoleColor.Red:
                        return 9;
                    case ConsoleColor.Green:
                        return 10;
                    case ConsoleColor.Yellow:
                        return 11;
                    case ConsoleColor.Blue:
                        return 12;
                    case ConsoleColor.Magenta:
                        return 13;
                    case ConsoleColor.Cyan:
                        return 14;
                    case ConsoleColor.White:
                        return 15;
                    default:
                        return (int)c;
                }
            }

            return $"\u001b[38;5;{ansiConverter(color)}m";
        }
    }
}
