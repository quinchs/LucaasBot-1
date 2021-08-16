using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSecureWebsockets.API.Packets
{
    internal class HandshakeResult : IPacket
    {
        [JsonProperty("resume")]
        public bool Resume { get; set; }

        public HandshakeResult() { }
        public HandshakeResult(bool resume)
        {
            this.Resume = resume;
        }
    }
}
