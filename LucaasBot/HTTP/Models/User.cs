using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LucaasBot.HTTP
{
    public class User
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("discriminator")]
        public string Discriminator { get; set; }
        
        [JsonProperty("public_flags")]
        public int public_flags { get; set; }
        
        [JsonProperty("flags")]
        public int Flags { get; set; }
        
        [JsonProperty("locale")]
        public string Locale { get; set; }
        
        [JsonProperty("mfa_enabled")]
        public bool Mfa_enabled { get; set; }
        
        [JsonProperty("premium_type")]
        public int Premium_type { get; set; }
    }
}
