using Newtonsoft.Json;
using LucaasBot.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using LucaasBot.HTTP.Websocket.Entities;
using System.Collections.Immutable;
using LucaasBot.HTTP.Websocket.Packets;

namespace LucaasBot.HTTP.Websocket
{
    public class WebSocketServer
    {
        /// <summary>
        ///     Fired when a new client connects.
        /// </summary>
        public event Func<WebsocketUser, Task> ClientConnected;

        /// <summary>
        ///     Fired when a client disconnects.
        /// </summary>
        public event Func<WebsocketUser, Task> ClientDisconnected;

        /// <summary>
        ///     Fired when a client resumes their connection.
        /// </summary>
        public event Func<WebsocketUser, Task> ClientResumed;

        /// <summary>
        ///     Fired when a client sends an event.
        /// </summary>
        public event Func<WebsocketUser, string, PayloadResolver, Task> EventReceived;

        /// <summary>
        ///     Gets a collection of connected clients.
        /// </summary>
        public IReadOnlyCollection<WebsocketUser> Clients
            => _clients.ToImmutableArray();

        private List<WebsocketUser> _clients;

        private bool disposed = false;

        public Task PushEvent(string eventName, object payload)
        {
            var targetClients = _clients.Where(x => x.Events.Contains(eventName));

            var frame = new SocketFrame(OpCodes.Dispatch, new Dispatch(eventName, payload));

            return Task.WhenAll(targetClients.Select(x => x.SendAsync(frame, default)));
        }

        internal async Task HandleDisconnect(WebsocketUser user, string reason = null)
        {
            await user.DisconnectAsync(reason);

            // they might be switching pages, lets wait 5 seconds before invoking the remove event and removing them from the list
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);

                if (!user.Connected)
                {
                    if (_clients.Remove(user))
                        ClientDisconnected.DispatchEvent(user);
                }
            });
        }

        internal async Task HandleConnectionAsync(HttpListenerWebSocketContext context)
        {
            var socket = context.WebSocket;

            byte[] buff = new byte[1024];

            var resultTask = socket.ReceiveAsync(buff, CancellationToken.None);
            var delay = Task.Delay(5000);

            var t = await Task.WhenAny(resultTask, delay);

            if (delay == t)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "handshake timeout", CancellationToken.None);
                return;
            }

            var result = await resultTask;

            if (result.MessageType != WebSocketMessageType.Text)
            {
                await socket.CloseAsync(WebSocketCloseStatus.ProtocolError, null, CancellationToken.None);
                return;
            }


            var frame = SocketFrame.FromBuffer(buff);

            if (frame.OpCode != OpCodes.Handshake)
            {
                await socket.CloseAsync(WebSocketCloseStatus.ProtocolError, null, CancellationToken.None);
                return;
            }

            var handshake = frame.PayloadAs<Handshake>();

            var webUser = AuthHelper.GetWebUser(handshake.Authentication);

            if (webUser == null)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Unauthorized", CancellationToken.None);
                return;
            }

            await ResumeOrCreateSocket(socket, handshake, webUser.Id);
        }

        private async Task ResumeOrCreateSocket(WebSocket socket, Handshake handshake, ulong userId)
        {
            var user = _clients.FirstOrDefault(x => x.UserId == userId);

            if (user != null)
            {
                user.ResumeAsync(handshake, socket);
                await user.SendAsync(new SocketFrame(OpCodes.HandshakeResult, new HandshakeResult(true)), CancellationToken.None);
                ClientResumed.DispatchEvent(user);

            }
            else
            {
                var websocketUser = new WebsocketUser(socket, handshake, userId, this);
                _clients.Add(websocketUser);
                await user.SendAsync(new SocketFrame(OpCodes.HandshakeResult, new HandshakeResult(false)), CancellationToken.None);
                ClientConnected.DispatchEvent(websocketUser);
            }
        }

        internal void DispatchEvent(WebsocketUser usr, string msg, PayloadResolver res)
            => EventReceived.DispatchEvent(usr, msg, res);

        public void Dispose()
        {
            if (!disposed)
            {
                _clients.ForEach(async x => await x.DisconnectAsync("Server shutdown"));
                this._clients = null;

                this.disposed = true;
            }
        }
    }
}
