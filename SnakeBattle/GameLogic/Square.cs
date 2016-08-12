using GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    class Square
    {
        public Player Occupant { get; set; }
        public bool isOccupied { get; set; }
        public ConsoleColor Color { get; set; }
    }
}
