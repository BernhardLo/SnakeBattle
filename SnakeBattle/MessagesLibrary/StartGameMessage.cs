using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesLibrary
{
    class StartGameMessage : Message
    {
        public StartGameMessage(string userName) : base (userName)
        {

        }

        public int NumberOfPlayers { get; set; }
        public List<string> PlayerNames { get; set; }
        public List<string> PlayerStartPositions { get; set; }
        public List<int> PlayerColor { get; set; }
        public string StartingPlayer { get; set; }
        public int GameMode { get; set; }

    }
}
