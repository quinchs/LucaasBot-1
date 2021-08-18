using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Websocket.Packets
{
    internal class Handshake : IPacket
    {
        [JsonProperty("auth")]
        public string Authentication { get; set; }

        [JsonProperty("events")]
        public List<string> Events { get; set; }

        [JsonProperty("page")]
        public string Page { get; set; }
    }
}
