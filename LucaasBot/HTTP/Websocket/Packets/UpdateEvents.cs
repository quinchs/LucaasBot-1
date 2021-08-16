using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Websocket.Packets
{
    public class UpdateEvents : IPacket
    {
        [JsonProperty("events")]
        public List<string> Events { get; set; }
    }
}
