using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LucaasBot.HTTP
{
    public class ModlogDeleteBody
    {
        [JsonProperty("modlog")]
        public string Modlog;
        [JsonProperty("uid")]
        public string Uid;
    }
}
