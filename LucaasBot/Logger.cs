using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static List<Type> AsmTypes;

        private static string GetCallingClass()
        {
            var trace = new StackTrace();

            var frames = trace.GetFrames().Select(x => x.GetMethod().DeclaringType);

            var type = frames.FirstOrDefault(x => x != typeof(Logger) && AsmTypes.Contains(x) && x.IsClass);

            return $"{type.ReflectedType?.Name ?? type.Name}.cs";
        }

        public static void Critical(object data, Severity? sev = null)
               => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Critical } : new Severity[] { Severity.Critical }, GetCallingClass());

        public static void Error(object data, Severity? sev = null)
               => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Error } : new Severity[] { Severity.Error }, GetCallingClass());
        public static void Log(object data, Severity? sev = null)
               => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Log  } : new Severity[] { Severity.Log }, GetCallingClass());
        public static void Warn(object data, Severity? sev = null)
            => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Warning  } : new Severity[] { Severity.Warning }, GetCallingClass());
        public static void Debug(object data, Severity? sev = null)
            => Write(data, sev.HasValue ? new Severity[] { sev.Value, Severity.Debug } : new Severity[] { Severity.Debug }, GetCallingClass());

        public static void Write(object data, Severity sev = Severity.Log, string caller = null)
           => _logEvent?.Invoke(null, (data, new Severity[] { sev }, caller ?? GetCallingClass()));
        public static void Write(object data, params Severity[] sevs)
            => _logEvent?.Invoke(null, (data, sevs, GetCallingClass()));
        public static void Write(object data, Severity[] sevs = null, string caller = null)
            => _logEvent?.Invoke(null, (data, sevs, caller ?? GetCallingClass()));

        private static ConcurrentQueue<KeyValuePair<(object, string), Severity[]>> _queue = new ConcurrentQueue<KeyValuePair<(object, string), Severity[]>>();
        private static event EventHandler<(object data, Severity[] sev, string caller)> _logEvent;
        private static string _prevLine = "";
        private static int _prevCount = 1;

        private static List<StreamWriter> _streams = new List<StreamWriter>();

        private static object lockObj = new object();

        public static void AddStreamSource(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanWrite)
                throw new ArgumentException("Stream is closed or cannot be written to", nameof(stream));

            var writer = new StreamWriter(stream);

            _streams.Add(writer);
        }

        static Logger()
        {
            _logEvent += Logger__logEvent;

            _streams.Add(new StreamWriter(Console.OpenStandardOutput()));

            AsmTypes = new List<Type>(Assembly.GetAssembly(typeof(Logger)).DefinedTypes.Select(x => x.AsType()));

        }

        private static void Logger__logEvent(object sender, (object data, Severity[] sev, string caller) e)
        {
            _queue.Enqueue(new KeyValuePair<(object, string), Severity[]>((e.data, e.caller), e.sev));
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

        private static void WriteToStreams(string str)
        {
            foreach (var stream in _streams)
            {
                stream.Write(str);
                stream.Flush();
            }
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
                    var data = res.Key.Item1;
                    var caller = res.Key.Item2;

                    var enumsWithColors = "";
                    foreach (var item in sev)
                    {
                        if (enumsWithColors == "")
                            enumsWithColors = $"<{(int)SeverityColorParser[item]}>{item}</{(int)SeverityColorParser[item]}>";
                        else
                            enumsWithColors += $" -> <{(int)SeverityColorParser[item]}>{item}</{(int)SeverityColorParser[item]}>";
                    }

                    var items = ProcessColors($"\u001b[38;5;249m{DateTime.UtcNow.ToString("O")} <Green>{caller}</Green> " + $"\u001b[1m[{enumsWithColors}]\u001b[0m - \u001b[37;1m{data}");

                    lock (lockObj)
                    {
                        var rawVal = string.Join("", items.Select(x => x.value)).Remove(0, 44);

                        if (_prevLine == rawVal)
                        {
                            // move left then up
                            WriteToStreams("\u001b[1000D"); // left
                            WriteToStreams("\u001b[1A"); // up
                            _prevCount++;

                        }
                        else if (_prevCount != 1)
                        {
                            _prevCount = 1;
                        }

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            WriteToStreams($"{string.Join("", items.Select(item => $"{ConsoleColorToANSI(item.color)}{item.value}\u001b[0m"))} {(_prevCount != 1 ? $"\u001b[38;5;11mx{_prevCount}" : "")}");
                        }
                        else
                        {
                            foreach (var item in items)
                            {
                                Console.ForegroundColor = item.color;
                                WriteToStreams(item.value);
                            }

                            if (_prevCount != 1)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                WriteToStreams($"x{_prevCount}");
                            }
                        }

                        _prevLine = rawVal;
                    }

                    WriteToStreams("\n");
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
