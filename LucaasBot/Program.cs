using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using LucaasBot.Services;
using System;
using System.Threading.Tasks;

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
                GatewayIntents = GatewayIntents.All,
                AlwaysAcknowledgeInteractions = false
            });
            _client = client;

            client.Log += LogAsync;
            client.Ready += ReadyAsync;
            var commandServeice = new CommandService();
            commandServeice.Log += LogAsync;

            await client.LoginAsync(TokenType.Bot, ConfigService.Config.Token);
            await client.StartAsync();

            string gameName = "LucaasBot Beta";
            await _client.SetGameAsync(gameName);
            await _client.SetStatusAsync(UserStatus.Online);

            var handler = new CommandHandler(commandServeice, client);


            await Task.Delay(-1);
        }
    

        private Task LogAsync(LogMessage log)
        {
            Logger.Write($"{log.Message} {log.Exception}", log.Severity.ToLogSeverity());

            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Logger.Write($"Connected as -> <Green>{Environment.UserName}</Green> :)", Severity.Log);

            return Task.CompletedTask;
        }
    }
}
