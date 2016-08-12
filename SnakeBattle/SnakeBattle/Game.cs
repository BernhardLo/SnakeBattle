﻿using MessagesLibrary;
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
        public Player _player;
        NetworkClient _nwc;
        const int _gamePort = 5000;
        private GameRoom _currentGame;

        public Game()
        {
            _playField = new Square[_playFieldWidth, _playFieldHeight];
            _player = new Player("<empty>", 7, 7);
            CreatePlayField();
            _playField[7, 7].Occupant = _player;
            _nwc = new NetworkClient();
            _currentGame = new GameRoom();
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
                        ListAvailableGames();
                        JoinAGame();
                        break;
                    case 3:
                        Console.WriteLine("Byt användarnamn");
                        Console.WriteLine("TBD");
                        //todo: byt namn
                        break;
                    case 4:
                        Console.WriteLine("Visa tillgängliga spel");
                        ListAvailableGames();
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

        private void JoinAGame()
        {
            if (ChooseGameRoom())
            {
                if (GameRoomValidated())
                {
                    Console.WriteLine("Nu sätter vi oss och väntar"); //todo: "test"
                    WaitingRoom();
                }
                else
                {
                    Console.WriteLine("Det gick inte att joina"); //todo :"test"
                }
            }
            else
            {
                Console.WriteLine("Det gick inte att skicka"); //todo: "test"
            }
        }

        private bool GameRoomValidated()
        {
            {
                bool valid = false;
                Stopwatch myclock = new Stopwatch();
                myclock.Start();
                do
                {
                    Thread.Sleep(50);
                    foreach (var item in _nwc._commandList)
                    {
                        if (item is JoinGameMessage)
                        {
                            JoinGameMessage tmp = item as JoinGameMessage;
                            Console.WriteLine("time used: " + myclock.ElapsedMilliseconds);
                            bool result = tmp.Confirmed;
                            if (result)
                            {
                                _nwc._filterHostName = tmp.HostName;
                                _currentGame.HostName = tmp.HostName;
                            }
                            Console.WriteLine("Trying to join: " + tmp.HostName + " Succeded: " + tmp.Confirmed); //todo: "test"
                            _nwc._commandList.Remove(tmp);
                            return result;
                        }
                    }


                    if (myclock.ElapsedMilliseconds > 10000)
                    {
                        Console.WriteLine("GameRoomValidated timeout");
                        return false;
                    }
                } while (!valid);
                return false;
            }
        }

        private bool ChooseGameRoom()
        {
            Console.Write("Ange spelrummets namn: ");
            string hostName = UserInput.GetString(); //todo: kontrollera att det finns ett spelrum
            var msg = new JoinGameMessage(_player.PlayerName) { HostName = hostName };
            try
            {
                _nwc.Send(MessageHandler.Serialize(msg));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sending JoinGameMessage Failed: " + ex.Message);
                return false;
            }
        }

        private void ListAvailableGames()
        {
            _nwc.Send(MessageHandler.Serialize(new FindGameMessage(_player.PlayerName)));
            var gameList = GetGameRooms();
            PrintGameRooms(gameList);

        }

        private void PrintGameRooms(List<GameRoom> gameList)
        {
            if (gameList.Count == 0)
            {
                Console.WriteLine("Det finns inga tillgängliga spel.");
            }
            else
                foreach (var item in gameList)
                {
                    Console.WriteLine($"{item.HostName} - {item.Gamers.Count} / {item.NumberOfPlayers}");
                }
        }

        private void NewGameRoom()
        {
            int numberOfPlayers = UserInput.GetIntFiltered("Ange antal spelare: ", 2, 4);
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

            try
            {
                _nwc.Send(MessageHandler.Serialize(ngm));
                _currentGame.HostName = _player.PlayerName;
                _nwc._filterHostName = _player.PlayerName;
                Console.WriteLine("NewGameRoom succeded"); //todo: "test"
            }
            catch (Exception ex)
            {
                Console.WriteLine("NewGameRoom failed: " + ex.Message);
            }
            Console.WriteLine("Väntar på fler spelare...");
            WaitingRoom();
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
                        _nwc._filterUserName = userName;
                        // todo: ta bort rätt UserNameMessage ur _commandlist, kanske funkar nu
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
                        bool result = tmp.UserNameConfirm;
                        Console.WriteLine("removing " + tmp.UserName); //todo: "test"
                        _nwc._commandList.Remove(tmp);
                        return result;
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

        private List<GameRoom> GetGameRooms()
        {
            Stopwatch myclock = new Stopwatch();
            myclock.Start();
            do
            {
                Thread.Sleep(50);
                foreach (var item in _nwc._commandList)
                {
                    if (item is FindGameMessage)
                    {
                        FindGameMessage tmp = item as FindGameMessage;
                        Console.WriteLine("time used: " + myclock.ElapsedMilliseconds);
                        List<GameRoom> result = tmp.GamesAvailable;
                        Console.WriteLine("removing FindGameMessage"); //todo: "test"
                        _nwc._commandList.Remove(tmp);
                        return result;
                    }
                }


                if (myclock.ElapsedMilliseconds > 10000)
                {
                    Console.WriteLine("UserNameValidated timeout");
                    return new List<GameRoom>();
                }
            } while (true);
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
        private void WaitingRoom()
        {
            bool valid = false;
            Stopwatch myclock = new Stopwatch();
            myclock.Start();
            do
            {
                //while (!Console.KeyAvailable)
                //{
                Thread.Sleep(50);
                if (_nwc._commandList.Count != 0)
                    foreach (var item in _nwc._commandList)
                    {
                        if (item is StartGameMessage)
                        {
                            StartGameMessage tmp = item as StartGameMessage;
                            Console.WriteLine("time used: " + myclock.ElapsedMilliseconds);//todo: "test"

                            SetGameProperties(tmp);

                            Console.WriteLine("removing StartGameMessage from commandlist"); //todo: "test"
                            _nwc._commandList.Remove(tmp);
                            valid = true;
                        }

                    }
                if (myclock.ElapsedMilliseconds > 500000)
                {
                    Console.WriteLine("StartGameLobby timeout");
                    valid = true;

                }
                //}

            } while (/*Console.ReadKey(true).Key != ConsoleKey.Escape || */!valid); // todo: Lyssna efter escape

        }

        private void SetGameProperties(StartGameMessage tmp)
        {
            Console.WriteLine("Received: " + MessageHandler.Serialize(tmp));
            _currentGame = tmp.GameRoomInfo;
        }
    }
}
