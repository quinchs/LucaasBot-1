using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using LucaasBot.Services;
using System;
using System.Threading.Tasks;
using Discord.Rest;

namespace LucaasBot
{
    public class Program
    {
        public static DiscordSocketClient Client
            => _client;
        public static SocketGuild LucaasGuild
            => _client.GetGuild(464733888447643650);

        private static DiscordSocketClient _client;

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
            var handlerService = new HandlerService(_client);
            var commandHandler = new CommandHandler(commandServeice, client);


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
