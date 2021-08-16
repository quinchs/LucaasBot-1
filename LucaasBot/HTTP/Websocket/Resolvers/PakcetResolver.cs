using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LucaasBot.HTTP.Websocket.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Websocket.Resolvers
{
    internal class PakcetResolver : JsonConverter
    {
        public static PakcetResolver Instance => new PakcetResolver();

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanConvert(Type objectType) => objectType == typeof(SocketFrame);
        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = JObject.Load(reader);
            var frame = new SocketFrame();

            // Remove the data property for manual deserialization
            var result = obj.GetValue("d", StringComparison.OrdinalIgnoreCase);
            result.Parent.Remove();

            // Populate the remaining properties.
            using (var subReader = obj.CreateReader())
            {
                serializer.Populate(subReader, frame);
            }

            // Process the Result property
            if (result != null)
            {
                IPacket packet = default(IPacket);

                switch (frame.OpCode)
                {
                    case OpCodes.Handshake:
                        packet = new Handshake();
                        break;
                    case OpCodes.Dispatch:
                        packet = new Dispatch();
                        break;
                    case OpCodes.UpdateEvents:
                        packet = new UpdateEvents();
                        break;
                }

                serializer.Populate(result.CreateReader(), packet);
                frame.Packet = packet;
            }
            else
                frame.Packet = null;

            return frame;
        }
    }
}
