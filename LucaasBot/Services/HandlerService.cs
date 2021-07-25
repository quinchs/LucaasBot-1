using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace LucaasBot
{
    public class HandlerService
    {
        private DiscordSocketClient client;
        private static readonly Dictionary<DiscordHandler, object> Handlers = new Dictionary<DiscordHandler, object>();

        /// <summary>
        /// Gets a handler with the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the handler to get.</typeparam>
        /// <returns>The handler with the type of <typeparamref name="T"/>. If no handler is found then <see langword="null"/>.</returns>
        public static T GetHandlerInstance<T>()
            where T : DiscordHandler => Handlers.FirstOrDefault(x => x.Key.GetType() == typeof(T)).Value as T;

        public HandlerService(DiscordSocketClient client)
        {
            this.client = client;

            this.client.Ready += Client_Ready;

            List<Type> typs = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAssignableTo(typeof(DiscordHandler)) && type != typeof(DiscordHandler))
                    {
                        // add to a cache.
                        typs.Add(type);
                    }
                }
            }

            foreach (var handler in typs)
            {
                var inst = Activator.CreateInstance(handler);
                Handlers.Add(inst as DiscordHandler, inst);
            }

            Logger.Write($"Handler service <Green>Initialized</Green>! {Handlers.Count} handlers created!", Severity.Core);
        }

        private Task Client_Ready()
        {
            _ = Task.Run(() =>
            {
                var work = new List<Func<Task>>();

                foreach (var item in Handlers)
                {
                    work.Add(async () =>
                    {
                        try
                        {
                            await item.Key.InitializeAsync(this.client);
                            item.Key.Initialize(this.client);
                        }
                        catch (Exception x)
                        {
                            Console.Error.WriteLine($"Exception occured while initializing {item.Key.GetType().Name}: ", x);
                        }
                    });
                }

                Task.WaitAll(work.Select(x => x()).ToArray());
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Marks the current class as a handler.
    /// </summary>
    public abstract class DiscordHandler
    {
        /// <summary>
        ///     Intitialized this handler asynchronously.
        /// </summary>
        /// <param name="client">The <see cref="DiscordSocketClient"/> to inject.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to inject.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> to inject.</param>
        /// <returns>A task representing the asynchronous operation of initializing this handler.</returns>
        public virtual Task InitializeAsync(DiscordSocketClient client)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Intitialized this handler.
        /// </summary>
        /// <param name="client">The <see cref="DiscordSocketClient"/> to inject.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to inject.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> to inject.</param>
        public virtual void Initialize(DiscordSocketClient client)
        {
        }
    }
}
