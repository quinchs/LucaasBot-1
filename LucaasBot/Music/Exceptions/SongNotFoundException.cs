using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Music.Exceptions
{
    public class SongNotFoundException : Exception
    {
        public SongNotFoundException(string message) : base(message)
        {
        }
        public SongNotFoundException() : base("Song is not found.") { }

        public SongNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
