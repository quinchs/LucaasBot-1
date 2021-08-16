using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        Http,
        Websocket
    }
    public class Logger
    {
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

        public static string BuildColoredString(object s, ConsoleColor color)
            => BuildColoredString(s.ToString(), color);
        public static string BuildColoredString(string s, ConsoleColor color)
        {
            return $"<{color}>{s}</{color}>";
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
            else
            {
                return null;
            }
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
            { Severity.Http, ConsoleColor.DarkYellow },
            { Severity.Websocket, ConsoleColor.Cyan },
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

                    var items = ProcessColors($"<DarkGray>{DateTime.UtcNow.ToString("O")}</DarkGray> " + $"\u001b[1m[{enumsWithColors}]\u001b[0m - {data}");

                    string msg = "";

                    foreach (var item in items)
                    {
                        Console.ForegroundColor = item.color;
                        Console.Write(item.value);
                        msg += item.value;
                    }


                    Console.Write("\n");
                }
            }
            inProg = false;
        }
    }
}
