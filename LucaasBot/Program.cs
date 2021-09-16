using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using LucaasBot.Services;
using System;
using System.Threading.Tasks;
using System.Linq;
using ConvertApiDotNet;
using System.Net.Http;
using System.Text.RegularExpressions;
using LucaasBot.Music.Services;

namespace LucaasBot
{
    class Program
    {
        private DiscordSocketClient _client;

        static void Main(string[] args)
        {
            ConfigService.LoadConfig();
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            Logger.Write("Starting discord client...", Severity.Core);

            var client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysAcknowledgeInteractions = false,
                LogLevel = LogSeverity.Debug,
                GatewayIntents = (GatewayIntents)(GatewayIntents.All - GatewayIntents.GuildPresences)
            });
            _client = client;

            client.Log += LogAsync;
            client.Ready += ReadyAsync;
            var commandServeice = new CommandService();
            commandServeice.Log += LogAsync;
            UserService.Initialize(client);
            commandServeice.Log += LogAsync;

            await client.LoginAsync(TokenType.Bot, ConfigService.Config.Token);
            await client.StartAsync();

            string gameName = "LucaasBot Beta";
            await _client.SetGameAsync(gameName);
            await _client.SetStatusAsync(UserStatus.Online);

            var handlerService = new HandlerService(_client);
            var commandHandler = new CommandHandler(commandServeice, client);

           

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            if (log.Source.StartsWith("Audio ") && (log.Message?.StartsWith("Sent") ?? false))
                return Task.CompletedTask;

            Severity? sev = null;

            if (log.Source.StartsWith("Audio "))
                sev = Severity.Music;
            if (log.Source.StartsWith("Gateway"))
                sev = Severity.Socket;
            if (log.Source.StartsWith("Rest"))
                sev = Severity.Rest;

            Logger.Write($"{log.Message} {log.Exception}", sev.HasValue ? new Severity[] { sev.Value, log.Severity.ToLogSeverity() } : new Severity[] { log.Severity.ToLogSeverity() });

            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Logger.Write($"Connected as -> <Green>{Environment.UserName}</Green> :)", Severity.Log);

            return Task.CompletedTask;
        }
    }
}
