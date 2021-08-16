using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.DataModels
{
    public class SupportTicket
    {
        public ulong UserID { get; set; }
        public ulong DMChannelID { get; set; }
        public ulong TicketChannel { get; set; }
        public bool Welcomed { get; set; } = false;

        public TicketTranscript Transcript;
    }
}
