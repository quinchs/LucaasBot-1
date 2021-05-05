using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace LucaasBot.Services
{
    public class CommandHandler
    {
        // setup fields to be set later in the constructor
        //private readonly IConfiguration _config;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;

        public CommandHandler(CommandService service, DiscordSocketClient client)
        {
            _commands = service;
            _client = client;

            _commands.CommandExecuted += CommandExecutedAsync;
            _client.MessageReceived += MessageReceivedAsync;

            _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }
        
        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Source != MessageSource.User)
            {
                return;
            }

            var argPos = 0;
            char prefix = Char.Parse("*");

            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(prefix, ref argPos)))
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);
            await _commands.ExecuteAsync(context, argPos, null, MultiMatchHandling.Best);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method

            if (!command.IsSpecified)
            {
                System.Console.WriteLine("Unknown Command Was Used");

                return;
            }

            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("Command Success\n------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                string time = DateTime.Now.ToString("hh:mm:ss tt");
                System.Console.WriteLine($"Command: [{command.Value.Name}]\nUser: [{context.User.Username}]\nTime: {time}");
                Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithAuthor("Command Error", "https://cdn.discordapp.com/emojis/787035973287542854.png?v=1");
                //embed.WithDescription(result.ErrorReason);
                embed.WithDescription("There was an error, check logs.");
                embed.WithColor(Color.Red);
                await context.Channel.SendMessageAsync("", false, embed.Build());

                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("Command Error\n------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                string time = DateTime.Now.ToString("hh:mm:ss tt");
                System.Console.WriteLine($"Command: [{command.Value.Name}]\nUser: [{context.User.Username}]\nTime: {time}\nError: {result.ErrorReason}");
                Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public static async Task ConsoleLogAsync(string action, string user, string details = null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("Action Success\n------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            string time = DateTime.Now.ToString("hh:mm:ss tt");
            System.Console.WriteLine($"Action: [{action}]\nUser: [{user}]\nTime: {time}");
            System.Console.WriteLine($"Details: {details}");
            Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }
    }
}