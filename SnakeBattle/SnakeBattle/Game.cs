using MessagesLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
        string serverIP;
        NetworkClient nwc;
        const int gamePort = 5000;

        public Game()
        {
            playField = new Square[playFieldWidth, playFieldHeight];
            player = new Player("Testplayer", 7, 7);
            CreatePlayField();
            playField[7, 7].Occupant = player;
            nwc = new NetworkClient();
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
            //Ansluter till servern och skriver ut IP-adressen om det lyckades
            Console.WriteLine(ConnectToServer());
            //skickar ett usernamemessage till servern, som i sin tur kontrollerar om namnet är ledigt.
            Console.WriteLine(SetUserName());

            PrintMenu();

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

        /// <summary>
        /// reads an ip address input from the user
        /// connects to the server with that address
        /// </summary>
        /// <returns>string representation of ip adress</returns>
        private string ConnectToServer ()
        {
            string serverIP = "";
            bool connected = false;
            do //denna loop körs till klienten anslutit till servern
            {
                serverIP = UserInput.GetIp(); //metod för att kontrollera att inmatningen är en godkänd ip-adress
                if (nwc.Connect(serverIP, gamePort)) //metod som returnerar "true" om anslutningen lyckades
                    connected = true;

            } while (!connected);
            return serverIP;
        }

        //todo : skriv en summary för SetUserName
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string SetUserName()
        {
            string userName = "";
            bool validUserName = false;
            do
            {
                userName = UserInput.GetUserName();
                if (RegisterUserName(userName))
                {
                    player.PlayerName = userName;
                    validUserName = true;
                }

            } while (!validUserName);

            return userName;
        }

        private bool RegisterUserName (string name)
        {
            bool valid = false;
            try
            {
                UserNameMessage unm = new UserNameMessage(name);
                nwc.Send(MessageHandler.Serialize(unm));

            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return valid;
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

        private void PrintMenu()
        {
            //todo: meny
            Console.WriteLine("detta är menyn");
        }
    }
}
