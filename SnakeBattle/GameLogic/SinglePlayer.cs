﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameLogic
{
    public static class SinglePlayer
    {
        private static List<int[]> _startPos = new List<int[]>();

        private static ConsoleColor[] tmpColors = { ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.Yellow ,
                                        ConsoleColor.Magenta, ConsoleColor.Cyan, ConsoleColor.DarkCyan, ConsoleColor.DarkRed};
        private static string[] tmpNames = { "Wall-E", "H.A.L. 9000", "SkyNet", "AlphaGo", "Deep Blue", "GLaDOS", "T-1000", "R2-D2" };

        public static void ClearPosList ()
        {
            if (_startPos != null)
                _startPos.Clear();
        }

        public static ConsoleColor GetColor(int i)
        {
            return tmpColors[i];
        }

        public static Player CreateOwnPlayer()
        {
            int[] start = RandomizeStartPos();
            return new Player(GetUserName()) { Color = ConsoleColor.Red , Xpos = start[0], Ypos = start[1] };
        }

        public static string GetUserName()
        {
            string userName = "";
            bool valid = false;
            do
            {
                Console.Write("Ange användarnamn: ");
                userName = GetString();
                if (!String.IsNullOrWhiteSpace(userName))
                {

                    string pattern = @"^[a-zA-Z0-9åäöÅÄÖ]+$";
                    Match result = Regex.Match(userName, pattern);
                    if (userName.Length < 14 && userName.Length > 2 && userName != "<empty>" && result.Success)
                    {
                        valid = true;
                    }
                }
            } while (!valid);
            return userName;
        }

        public static string GetString()
        {
            string ret = "";
            try
            {
                ret = Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            return ret;
        }

        public static int[] RandomizeStartPos()
        {
            int[] result = new int[2];
            bool validPlacement = false;
            do
            {
                int x = Randomizer.Rng(2, 19);
                int y = Randomizer.Rng(2, 19);
                if (!_startPos.Contains(new int[2] { x, y }) )
                {
                    result[0] = x;
                    result[1] = y;
                    _startPos.Add(new int[2] { x - 1, y - 1 });
                    _startPos.Add(new int[2] { x - 1, y });
                    _startPos.Add(new int[2] { x - 1, y + 1 });
                    _startPos.Add(new int[2] { x, y - 1 });
                    _startPos.Add(new int[2] { x, y });
                    _startPos.Add(new int[2] { x, y + 1 });
                    _startPos.Add(new int[2] { x + 1, y - 1 });
                    _startPos.Add(new int[2] { x + 1, y });
                    _startPos.Add(new int[2] { x + 1, y + 1 });
                    validPlacement = true;
                }

            } while (!validPlacement);

            return result;

        }
    }
}
