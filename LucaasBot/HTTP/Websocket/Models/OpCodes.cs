using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Websocket
{
    public enum OpCodes
    {
        Handshake = 0,
        HandshakeResult = 1,
        Heartbeat = 2,
        HeartbeatAck = 3,
        Dispatch = 4,
        UpdateEvents = 5,
    }
}
