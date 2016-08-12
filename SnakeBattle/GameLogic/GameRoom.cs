using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public class GameRoom
    {
        public int NumberOfPlayers { get; set; }
        public List<Player> PlayerList { get; set; }
        public string StartingPlayer { get; set; }
        public int GameMode { get; set; }
        public string HostName { get; set; }
        public GameRoom()
        {
            PlayerList = new List<Player>();
        }
    }
}
