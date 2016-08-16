using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public class Player
    {
        public string PlayerName { get; set; }
        public int Xpos { get; set; }
        public int Ypos { get; set; }
        public bool IsAlive { get; set; }
        public ConsoleColor Color { get; set; }
        public Direction Direction { get; set; }
        public Player(string PlayerName)
        {
            this.PlayerName = PlayerName;
            this.IsAlive = true;
        }
    }
}
