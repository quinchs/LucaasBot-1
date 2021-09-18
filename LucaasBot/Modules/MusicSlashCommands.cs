﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using LucaasBot.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Modules
{
    public class MusicSlashCommandsFactory : ApplicationCommandFactory
    {
        public static List<string> MusicCommands = new List<string>();

        public readonly string[] RestrictedCommands = new string[]
        {
            "skip",
            "volume",
            "fairplay",
            "queue-next",
            "disconnect",
            "remove-song",
            "stop",
            "pause",
            "resume"
        };

        [GuildSpecificCommand(464733888447643650)]
        public override IEnumerable<ApplicationCommandProperties> BuildCommands()
        {
            var play = new SlashCommandBuilder()
                .WithName("play")
                .WithDescription("Plays a song given a name, youtube url, spotify url, or soundcloud.")
                .AddOption("query", ApplicationCommandOptionType.String, "The link or name of the song.", true)
                .Build();

            var join = new SlashCommandBuilder()
                .WithName("join")
                .WithDescription("Joins your voice channel")
                .Build();

            var queue = new SlashCommandBuilder()
                .WithName("queue")
                .WithDescription("Lists the current queue or adds a song to the queue.")
                .AddOption("query", ApplicationCommandOptionType.String, "The link or name of the song to add to the queue.", false)
                .Build();

            var queuenext = new SlashCommandBuilder()
                .WithName("queue-next")
                .WithDefaultPermission(false)
                .WithDescription("Queues a song to play next")
                .AddOption("query", ApplicationCommandOptionType.String, "The link or name of the song to add to the queue.")
                .Build();

            var search = new SlashCommandBuilder()
                .WithName("search")
                .WithDescription("Searches youtube for songs/videos related to your query.")
                .AddOption("query", ApplicationCommandOptionType.String, "The name or keywords to the video/song.")
                .Build();

            var shuffle = new SlashCommandBuilder()
                .WithName("shuffle")
                .WithDescription("Togles whether or not songs will be picked in a random order.")
                .Build();

            var autoplay = new SlashCommandBuilder()
                .WithName("autoplay")
                .WithDescription("Toggles whether or not related songs will be played after the queue is empty.")
                .Build();

            var listqueue = new SlashCommandBuilder()
                .WithName("listqueue")
                .WithDescription("Lists the current songs within the queue.")
                .AddOption("page", ApplicationCommandOptionType.Integer, "The page of the queue to view", false)
                .Build();

            var next = new SlashCommandBuilder()
                .WithName("skip")
                .WithDescription("Plays the next song in the queue.")
                .WithDefaultPermission(false)
                .AddOption("count", ApplicationCommandOptionType.Integer, "Skip X amount of songs", false)
                .Build();

            var stop = new SlashCommandBuilder()
                .WithName("stop")
                .WithDefaultPermission(false)
                .WithDescription("Clears the queue and stops playing music")
                .Build();

            var dc = new SlashCommandBuilder()
                .WithName("disconnect")
                .WithDefaultPermission(false)
                .WithDescription("Stops and disconnects the bot")
                .Build();

            var pause = new SlashCommandBuilder()
                .WithName("pause")
                .WithDescription("Pauses the current song")
                .WithDefaultPermission(false)
                .Build();

            var resume = new SlashCommandBuilder()
                .WithName("resume")
                .WithDescription("Resumes the current song")
                .WithDefaultPermission(false)
                .Build();

            var volume = new SlashCommandBuilder()
                .WithName("volume")
                .WithDefaultPermission(false)
                .WithDescription("Changes the current volume. Range is 0-100")
                .AddOption("value", ApplicationCommandOptionType.Integer, "The new volume value in the range of 0-100")
                .Build();

            var removesong = new SlashCommandBuilder()
                .WithName("remove-song")
                .WithDefaultPermission(false)
                .WithDescription("Removes a song from the queue")
                .AddOption("index", ApplicationCommandOptionType.Integer, "The index of the song to remove.")
                .Build();

            // depricated because of soundcloud api
            //var soundcloundq = new SlashCommandBuilder()
            //    .WithName("soundcloud-queue")
            //    .WithDescription("Plays a song from soundcloud.")
            //    .Build();

            var fairplay = new SlashCommandBuilder()
                .WithName("fairplay")
                .WithDefaultPermission(false)
                .WithDescription("When multiple people are queueing music it will chose between people fairly.")
                .Build();

            var nowplaying = new SlashCommandBuilder()
                .WithName("now-playing")
                .WithDescription("Shows the currently playing song")
                .Build();

            return new List<ApplicationCommandProperties>() 
            {
                play,
                join, 
                queue,
                next,
                queuenext,
                nowplaying,
                fairplay,
                //soundcloundq,
                removesong,
                volume,
                resume,
                pause,
                dc,
                stop,
                listqueue,
                search,
                shuffle,
                autoplay
            };
        }

        public override async Task OnRegisterAllAsync(IReadOnlyCollection<RestApplicationCommand> commands)
        {
            var restrictedCommandIds = commands.Where(x => RestrictedCommands.Contains(x.Name)).Select(x => x.Id);

            var permissions = new ApplicationCommandPermission[]
            {
                new ApplicationCommandPermission(563030072026595339, ApplicationCommandPermissionTarget.Role, true), // staff role
                new ApplicationCommandPermission(620312075863851039, ApplicationCommandPermissionTarget.Role, true), // dj
                new ApplicationCommandPermission(639547493767446538, ApplicationCommandPermissionTarget.Role, true), // dev
            };

            await Client.Rest.BatchEditGuildCommandPermissions(464733888447643650, new Dictionary<ulong, ApplicationCommandPermission[]>(
                restrictedCommandIds.Select(x => new KeyValuePair<ulong, ApplicationCommandPermission[]>(x, permissions))
            ));

            MusicCommands.AddRange(commands.Select(x => x.Name));
        }
    }
}
