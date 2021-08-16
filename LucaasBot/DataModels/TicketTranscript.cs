using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.DataModels
{
    public class TicketTranscript
    {
        public List<TicketMessage> Messages { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        public TicketUser TicketAuthor { get; set; }
        public 
    }

    public class TicketMessage
    {
        public TicketUser Author;
        public DateTime CreatedAt;
        public List<TicketAttachment> Attachments { get; set; }
    }
    public class TicketUser
    {
        public bool IsStaff { get; set; }
        public string Nickname { get; set; }
        public string Username { get; set; }
        public string DisplayColor { get; set; }
        public ulong Id { get; set; }
    }
    public class TicketAttachment
    {
        public string Filename { get; set; }
        public string FileId { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
