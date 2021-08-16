using Newtonsoft.Json;
using LucaasBot.HTTP.Websocket.Packets;
using LucaasBot.HTTP.Websocket.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Websocket
{
    [JsonConverter(typeof(PakcetResolver))]
    internal class SocketFrame
    {
        [JsonProperty("op")]
        public OpCodes OpCode { get; set; }

        [JsonProperty("d")]
        public IPacket Packet { get; set; }

        public SocketFrame() { }

        public SocketFrame(OpCodes code, IPacket packet)
        {
            this.OpCode = code;
            this.Packet = packet;
        }

        public static SocketFrame FromBuffer(byte[] buff)
            => JsonConvert.DeserializeObject<SocketFrame>(Encoding.UTF8.GetString(buff));

        public TPayload PayloadAs<TPayload>() where TPayload : IPacket
            => (TPayload)this.Packet;

        [JsonIgnore]
        public string Json
            => JsonConvert.SerializeObject(this);
    }
}
