using MessagesLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeBattle
{
    class Game
    {
        Square[,] _playField;
        const int _playFieldWidth = 20;
        const int _playFieldHeight = 20;
        Player _player;
        NetworkClient _nwc;
        const int _gamePort = 5000;

        public Game()
        {
            _playField = new Square[_playFieldWidth, _playFieldHeight];
            _player = new Player("Testplayer", 7, 7);
            CreatePlayField();
            _playField[7, 7].Occupant = _player;
            _nwc = new NetworkClient();
        }

        private void CreatePlayField()
        {
            for (int y = 0; y < _playFieldHeight; y++)
            {
                for (int x = 0; x < _playFieldWidth; x++)
                {
                    var square = new Square { isOccupied = false };
                    _playField[x, y] = square;
                }
            }
        }

        public void Play()
        {
            //Ansluter till servern och skriver ut IP-adressen om det lyckades
            Console.WriteLine(ConnectToServer());

            //skickar ett usernamemessage till servern, som i sin tur kontrollerar om namnet är ledigt.
            Console.WriteLine(SetUserName());

            do //Körs till användaren matat in ett godkänt menyval
            {
                PrintMenu();
                int menuChoice = UserInput.GetInt();

                switch (menuChoice)
                {
                    case 0:
                        Console.WriteLine("Programmet avslutas...");
                        Environment.Exit(0);
                        break;
                    case 1:
                        Console.WriteLine("Starta nytt spel");
                        NewGameRoom();
                        break;
                    case 2:
                        Console.WriteLine("Anslut till spel");
                        Console.WriteLine("TBD");
                        //todo: anslut till spel
                        break;
                    case 3:
                        Console.WriteLine("Byt användarnamn");
                        Console.WriteLine("TBD");
                        //todo: byt namn
                        break;
                    case 4:
                        Console.WriteLine("Visa tillgängliga spel");
                        Console.WriteLine("TBD");
                        //todo: visa spel
                        break;
                    default:
                        Console.WriteLine("felaktig inmatning");
                        break;
                }

            } while (true);

            do
            {
                DrawField();
                HandleMovement();

            } while (_player.IsAlive);

            Console.WriteLine("Game Over");
        }

        private void NewGameRoom()
        {
            int numberOfPlayers = UserInput.GetIntFiltered("Ange antal spelare", 2, 4);

            //todo: ange spelplanens storlek
            //int sizeX = UserInput.GetIntFiltered("Ange spelplanens storlek (X)", 10, 20);
            //int sizeY = UserInput.GetIntFiltered("Ange spelplanens storlek (Y)", 10, 20);

            //todo: ange spelläge (eventuell bitmask/array)

            NewGameMessage ngm = new NewGameMessage(_player.PlayerName)
            {
                SizeX = _playFieldWidth,
                SizeY = _playFieldHeight,
                NumberPlayers = numberOfPlayers,
                UsePassword = false,
                Password = "",
                GameMode = 0
            };

            _nwc.Send(MessageHandler.Serialize(ngm));
        }

        /// <summary>
        /// reads an ip address input from the user
        /// connects to the server with that address
        /// </summary>
        /// <returns>string representation of ip adress</returns>
        private string ConnectToServer()
        {
            string serverIP = "";
            bool connected = false;
            do //denna loop körs till klienten anslutit till servern
            {
                serverIP = UserInput.GetIp(); //metod för att kontrollera att inmatningen är en godkänd ip-adress
                if (_nwc.Connect(serverIP, _gamePort)) //metod som returnerar "true" om anslutningen lyckades
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
                    if (UserNameValidated(userName))
                    {
                        _player.PlayerName = userName;
                        validUserName = true;
                    }
                }

            } while (!validUserName);

            return userName;
        }

        private bool UserNameValidated(string userName)
        {
            bool valid = false;
            Stopwatch myclock = new Stopwatch();
            myclock.Start();
            do
            {
                Thread.Sleep(50);
                foreach (var item in _nwc._commandList)
                {
                    if (item is UserNameMessage)
                    {
                        UserNameMessage tmp = item as UserNameMessage;
                        Console.WriteLine("time used: " + myclock.ElapsedMilliseconds);
                        return tmp.UserNameConfirm;
                    }
                }

                
                if (myclock.ElapsedMilliseconds > 10000)
                {
                    Console.WriteLine("UserNameValidated timeout");
                    return false;
                }
            } while (!valid);
            return false;

        }

        private bool RegisterUserName(string name)
        {
            try
            {
                UserNameMessage unm = new UserNameMessage(name);
                _nwc.Send(MessageHandler.Serialize(unm));
                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }


        }

        public void DrawField()
        {
            Console.Clear();

            Console.Write("  ");
            for (int i = 0; i < _playFieldWidth; i++)
                Console.Write("__");

            Console.WriteLine();

            for (int y = 0; y < _playFieldHeight; y++)
            {
                Console.Write(" |");
                for (int x = 0; x < _playFieldWidth; x++)
                {
                    Square square = _playField[x, y];

                    if (_player.Xpos == x && _player.Ypos == y)
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.Write("  ");
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        if (_playField[x, y].Occupant == null)
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
            for (int i = 0; i < _playFieldWidth; i++)
                Console.Write("¯¯");
        }

        public void HandleMovement()
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            int newX = _player.Xpos;
            int newY = _player.Ypos;

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
                if (_playField[newX, newY].isOccupied)
                {
                    _player.IsAlive = false;
                }
                else
                {
                    _playField[newX, newY].Occupant = _player;
                    _playField[newX, newY].isOccupied = true;
                    _player.Xpos = newX;
                    _player.Ypos = newY;
                }
            }
            catch (IndexOutOfRangeException IORex)
            {
                _player.IsAlive = false;
            }
        }

        private void PrintMenu()
        {
            Console.WriteLine("0: Avsluta");
            Console.WriteLine("1: Starta nytt spel");
            Console.WriteLine("2: Anslut till spel");
            Console.WriteLine("3: Byt användarnamn");
            Console.WriteLine("4: Visa tillgängliga spel");
        }
    }
}
