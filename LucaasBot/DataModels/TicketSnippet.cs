using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.DataModels
{
    [BsonIgnoreExtraElements]
    public class TicketSnippet
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
