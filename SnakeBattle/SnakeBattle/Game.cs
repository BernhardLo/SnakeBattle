using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeBattle
{
    class Game
    {
        Square[,] playField;
        const int playFieldWidth = 20;
        const int playFieldHeight = 20;
        Player player;
        private string serverIP;


        public Game()
        {
            playField = new Square[playFieldWidth, playFieldHeight];
            player = new Player("Testplayer", 7, 7);
            CreatePlayField();
            playField[7, 7].Occupant = player;
        }

        private void CreatePlayField()
        {
            for (int y = 0; y < playFieldHeight; y++)
            {
                for (int x = 0; x < playFieldWidth; x++)
                {
                    var square = new Square { isOccupied = false };
                    playField[x, y] = square;
                }
            }
        }

        public void Play()
        {
            //bool connected = false;
            //do
            //{
            //    Console.Write("Ange IP till servern: ");
            //    serverIP = Console.ReadLine();
            //    //todo: anslut till servern


            //} while (!connected);

            serverIP = UserInput.GetString();

            bool validUsername = false;
            do
            {
                Console.WriteLine("Ange användarnamn: ");
                player.PlayerName = Console.ReadLine();
            } while (!validUsername);

            bool gameProperties = false;


            do
            {
                Console.WriteLine("Ange antal spelare: ");
                int antalSpelare = Convert.ToInt32(Console.ReadLine());


            } while (gameProperties);

            do
            {
                DrawField();
                HandleMovement();

            } while (player.IsAlive);

            Console.WriteLine("Game Over");
        }

        public void DrawField()
        {
            Console.Clear();

            Console.Write("  ");
            for (int i = 0; i < playFieldWidth; i++)
                Console.Write("__");

            Console.WriteLine();

            for (int y = 0; y < playFieldHeight; y++)
            {
                Console.Write(" |");
                for (int x = 0; x < playFieldWidth; x++)
                {
                    Square square = playField[x, y];

                    if (player.Xpos == x && player.Ypos == y)
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.Write("  ");
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        if (playField[x, y].Occupant == null)
                        {
                            Console.Write("  ");
                        }
                        else
                        {
                            Console.BackgroundColor = ConsoleColor.DarkRed;
                            Console.Write("  ");
                            Console.BackgroundColor = ConsoleColor.Black;
                        }
                    }
                }
                Console.Write("|");
                Console.WriteLine();
            }
            Console.Write("  ");
            for (int i = 0; i < playFieldWidth; i++)
                Console.Write("¯¯");
        }

        public void HandleMovement()
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            int newX = player.Xpos;
            int newY = player.Ypos;


            switch (keyInfo.Key)
            {
                case ConsoleKey.RightArrow:
                    newX++;
                    break;
                case ConsoleKey.LeftArrow:
                    newX--;
                    break;
                case ConsoleKey.DownArrow:
                    newY++;
                    break;
                case ConsoleKey.UpArrow:
                    newY--;
                    break;
                default:
                    break;
            }

            try
            {
                if (playField[newX, newY].isOccupied)
                {
                    player.IsAlive = false;
                }
                else
                {
                    playField[newX, newY].Occupant = player;
                    playField[newX, newY].isOccupied = true;
                    player.Xpos = newX;
                    player.Ypos = newY;
                }
            }
            catch (IndexOutOfRangeException IORex)
            {
                player.IsAlive = false;
            }
        }
    }
}
