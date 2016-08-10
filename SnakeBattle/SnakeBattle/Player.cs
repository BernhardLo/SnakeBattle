using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeBattle
{
    class Player
    {
        public string PlayerName { get; set; }
        public int Xpos { get; set; }
        public int Ypos { get; set; }
        public Direction Orientation { get; set; }
        public bool IsAlive { get; set; }

        public Player(string PlayerName, int X, int Y)
        {
            this.PlayerName = PlayerName;
            this.Xpos = X;
            this.Ypos = Y;
            this.IsAlive = true;
        }
    }
}
