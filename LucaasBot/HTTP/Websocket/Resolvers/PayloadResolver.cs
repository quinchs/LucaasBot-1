using Newtonsoft.Json.Linq;
using SimpleSecureWebsockets.API.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSecureWebsockets
{
    public sealed class PayloadResolver
    {
        private JToken token;

        internal PayloadResolver(Dispatch d)
        {
            this.token = d.Payload;
        }

        public TPayload Resolve<TPayload>()
            => token.ToObject<TPayload>();
    }
}
