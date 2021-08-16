using LucaasBot.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP
{
    public class ModlogBody
    {
        [JsonProperty("userId")]
        public ulong UserId { get; set; }
        [JsonProperty("type")]
        public ModlogAction Type { get; set; }
        [JsonProperty("moderatorId")]
        public ulong ModeratorId { get; set; }
        [JsonProperty("reason")]
        public string Reason { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
    }
}
