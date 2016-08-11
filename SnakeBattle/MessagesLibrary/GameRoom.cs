using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesLibrary
{
    public class GameRoom
    {
        public int NumberOfPlayers { get; set; }
        public int GameMode { get; set; }
        public string HostName { get; set; }

        public List<string> Gamers { get; set; }

        public GameRoom()
        {
            Gamers = new List<string>();
        }
    }
}
