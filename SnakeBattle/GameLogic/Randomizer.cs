using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLogic
{
    public static class Randomizer
    {
        static Random rnd = new Random();
        public static bool Try(int chance)
        {
            return Rng() <= chance;
        }

        public static int Rng()
        {
            return Rng(0, 101);
        }

        public static int Rng(int max)
        {
            return Rng(0, max);
        }
        public static int Rng(int min, int max)
        {
            return rnd.Next(min, max);
        }
    }
}
