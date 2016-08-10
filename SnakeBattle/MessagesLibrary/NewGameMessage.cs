using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesLibrary
{
    public class NewGameMessage : Message
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int NumberPlayers { get; set; }
        public bool UsePassword { get; set; }
        public string Password { get; set; }
        public int GameMode { get; set; }
    }
}
