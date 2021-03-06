﻿using GameLogic;
using MessagesLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        /// <summary>
        /// Create player with playername "<empty>" as default, create a new networkclient and new Gameroom. 
        /// </summary>
        public Game()
        {
            _player = new Player("<empty>");
            _nwc = new NetworkClient();
            _currentGame = new GameRoom();
        }

        /// <summary>
        /// Creates the play-field ; 20 x 20
        /// </summary>
        private void CreatePlayField()
        {
            for (int y = 0; y < _playFieldHeight; y++)
            {
                for (int x = 0; x < _playFieldWidth; x++)
                {
                    var square = new Square { isOccupied = false, Color = ConsoleColor.Black };
                    _playField[x, y] = square;
                }
            }
        }
        /// <summary>
        /// Trying to connect to the server, writes the ip-address if succeeded. Sending username messga to the server who check if the username is avaiable. Printing the menu and handle the userinput. 
        /// </summary>
        public void Play()
        {
            //Ansluter till servern och skriver ut IP-adressen om det lyckades
            bool proceed = false;

            do
            {
                PrintFirstMenu();

                int menuChoice = UserInput.GetInt();

                switch (menuChoice)
                {
                    case 0:
                        Console.WriteLine("Programmet avslutas...");
                        Environment.Exit(0);
                        break;
                    case 1:
                        Console.WriteLine("Anslut till servern");
                        proceed = true;
                        break;
                    case 2:
                        Console.WriteLine("Spela mot datorn");
                        RunSinglePlayer();
                        break;
                    default:
                        Console.WriteLine("Felaktig inmatning");
                        break;
                }
            } while (!proceed);

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
                        Console.WriteLine("Visa tillgängliga spel");
                        ListAvailableGames();
                        break;
                    case 4:
                        Console.WriteLine("Spela mot datorn");
                        RunSinglePlayer();
                        break;
                    default:
                        Console.WriteLine("Felaktig inmatning");
                        break;
                }

            } while (true);
        }

        private void PrintFirstMenu()
        {
            Console.WriteLine("0: Avsluta");
            Console.WriteLine("1: Anslut till servern");
            Console.WriteLine("2: Spela mot datorn");
        }

        /// <summary>
        /// Printing out messages to user depending on game-room. Place the user in the waiting-room if the game-room is not full.
        /// </summary>

        private void JoinAGame()
        {
            if (ChooseGameRoom())
            {
                if (GameRoomValidated())
                {
                    Console.WriteLine("Väntar på fler spelare...");
                    WaitingRoom();
                }
                else
                {
                    Console.WriteLine("Det gick inte att ansluta till spel");
                }
            }
            else
            {
                Console.WriteLine("Det gick inte att ansluta till spel");
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
                            _nwc._commandList.Remove(item);
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
        /// <summary>
        /// Ask user for input of game (hostname), try to serialize message to server
        /// </summary>
        /// <returns></returns>
        private bool ChooseGameRoom()
        {
            Console.Write("Ange spelrummets namn: ");
            string hostName = UserInput.GetString();
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
        /// <summary>
        /// Serialize message to the server to get avaiable games
        /// </summary>
        private void ListAvailableGames()
        {
            _nwc.Send(MessageHandler.Serialize(new FindGameMessage(_player.PlayerName)));
            var gameList = GetGameRooms();
            PrintGameRooms(gameList);

        }
        /// <summary>
        /// Printing out avaiable games for user
        /// </summary>
        /// <param name="gameList"></param>
        private void PrintGameRooms(List<GameRoom> gameList)
        {
            if (gameList.Count == 0)
            {
                Console.WriteLine("Det finns inga tillgängliga spel.");
            }
            else
                foreach (var item in gameList)
                {
                    Console.WriteLine($"{item.HostName} - {item.PlayerList.Count}/{item.NumberOfPlayers}");
                }
        }
        /// <summary>
        /// Ask user for number of players. Trying to serialaize gameinfo to the server. If succeeded; put the user in the waitingroom.
        /// </summary>

        private void NewGameRoom()
        {
            int numberOfPlayers = UserInput.GetIntFiltered("Ange antal spelare (2-8): ", 2, 8);

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
            do //this loop runs until the user has connected to a server
            {
                serverIP = UserInput.GetIp(); //method for checking if the input is a possible IP-adress
                if (_nwc.Connect(serverIP, _gamePort)) //method returning "true" if the connection succeeded
                    connected = true;

            } while (!connected);
            return serverIP;
        }

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
                        _nwc._commandList.Remove(item);
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
                        //Console.WriteLine("time used: " + myclock.ElapsedMilliseconds);
                        List<GameRoom> result = tmp.GamesAvailable;
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

        public void DrawField(int mod)
        {
            Console.Clear();
            if (mod != 0)
                Console.ForegroundColor = _player.Color;

            Console.Write("  ");
            for (int i = 0; i < _playFieldWidth; i++)
            {
                Console.Write("__");
            }

            Console.WriteLine();

            for (int y = 0; y < _playFieldHeight; y++)
            {
                Console.Write(" |");
                for (int x = 0; x < _playFieldWidth; x++)
                {
                    Square square = _playField[x, y];
                    Console.BackgroundColor = square.Color;
                    Console.Write("  ");
                    Console.BackgroundColor = ConsoleColor.Black;
                }
                Console.Write("|");
                Console.WriteLine();
            }

            Console.Write("  ");
            for (int i = 0; i < _playFieldWidth; i++)
                Console.Write("¯¯");

            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < _currentGame.PlayerList.Count; i++)
            {

                Console.SetCursorPosition((_playFieldWidth * 2) + 5, 3 + i);
                Console.BackgroundColor = _currentGame.PlayerList[i].Color;
                Console.Write("  ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write($" {_currentGame.PlayerList[i].PlayerName}");
                if (!_currentGame.PlayerList[i].IsAlive)
                {
                    Console.Write(" (R.I.P.)");
                }
            }
            Console.SetCursorPosition(3, _playFieldHeight + 3);
            Console.Write("Antal steg kvar: ");

            for (int i = 0; i < mod; i++)
            {
                Console.BackgroundColor = _player.Color;
                Console.Write("  ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write("  ");
            }
        }

        public int[] HandleMovement()
        {

            int newX = _player.Xpos;
            int newY = _player.Ypos;
            int[] result = new int[2] { -1, -1 };

            bool validMove = false;

            do
            {

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow:
                        if (_player.Direction != Direction.Left)
                        {
                            _player.Direction = Direction.Right;
                            newX++;
                            validMove = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow:
                        if (_player.Direction != Direction.Right)
                        {
                            _player.Direction = Direction.Left;
                            newX--;
                            validMove = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        if (_player.Direction != Direction.Up)
                        {
                            _player.Direction = Direction.Down;
                            newY++;
                            validMove = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        if (_player.Direction != Direction.Down)
                        {
                            _player.Direction = Direction.Up;
                            newY--;
                            validMove = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    default:
                        break;
                }
            } while (!validMove);

            try
            {
                if (_playField[newX, newY].isOccupied)
                {
                    _player.IsAlive = false;
                }
                else
                {
                    _playField[newX, newY].Color = _player.Color;
                    _playField[newX, newY].isOccupied = true;
                    _player.Xpos = newX;
                    _player.Ypos = newY;
                    result[0] = newX;
                    result[1] = newY;

                }
            }
            catch (IndexOutOfRangeException IORex)
            {
                _player.IsAlive = false;
            }

            return result;
        }

        private void PrintMenu()
        {
            Console.WriteLine("0: Avsluta");
            Console.WriteLine("1: Starta nytt spel");
            Console.WriteLine("2: Anslut till spel");
            Console.WriteLine("3: Visa tillgängliga spel");
            Console.WriteLine("4: Spela ensam");
        }
        private void WaitingRoom()
        {
            bool valid = false;
            Stopwatch myclock = new Stopwatch();
            myclock.Start();
            do
            {
                Thread.Sleep(50);
                foreach (var item in _nwc._commandList)
                {
                    if (item is StartGameMessage)
                    {
                        StartGameMessage tmp = item as StartGameMessage;
                        SetGameProperties(tmp);
                        _nwc._commandList.Remove(item);
                        valid = true;
                        break;
                    }
                }
                if (myclock.ElapsedMilliseconds > 120000)
                {
                    Console.WriteLine("StartGameLobby timeout");
                    valid = true;
                }

            } while (!valid);

            RunGame();
        }

        private void RunGame()
        {
            bool gameIsWon = false;
            string winnerName = "";
            _playField = new Square[_playFieldWidth, _playFieldHeight];
            CreatePlayField();
            InsertPlayers();
            DrawField(0);
            PlayMessage pm = new PlayMessage(_player.PlayerName);

            // First time, if starting player is this player.
            if (_currentGame.StartingPlayer == _player.PlayerName)
            {
                Stopwatch myclock = new Stopwatch();
                myclock.Start();
                List<int[]> moveList = new List<int[]>();

                for (int i = 3; i > 0; i--)
                {
                    DrawField(i);
                    moveList.Add(HandleMovement());
                }
                DrawField(0);

                pm.MoveList = moveList;
                pm.IsAlive = _player.IsAlive;
                pm.HostName = _currentGame.HostName;
                _nwc.Send(MessageHandler.Serialize(pm));
            }

            do
            {
                PlayMessage apm = WaitForPlayMessage();
                if (apm.GameIsWon)
                {
                    winnerName = apm.NextUser;
                    gameIsWon = true;
                }
                else
                {
                    // If recived message is not player add movement to squares

                    if (apm.UserName != _player.PlayerName) // JE 
                    {
                        DrawOponents(apm);// JE
                        DrawField(0);
                    }

                    if (apm.NextUser == _player.PlayerName)
                    {
                        PlayMessage pmsg = new PlayMessage(_player.PlayerName);
                        List<int[]> moveList = new List<int[]>();

                        while (Console.KeyAvailable) //clears input buffer
                        {
                            Console.ReadKey(true);
                        }
                        for (int i = 3; i > 0; i--)
                        {
                            if (_player.IsAlive)
                            {
                                DrawField(i);
                                moveList.Add(HandleMovement());
                            }
                            else
                            {
                                i = 0;
                                DrawField(i);
                            }
                        }
                        DrawField(0);

                        pmsg.MoveList = moveList;
                        pmsg.IsAlive = _player.IsAlive;
                        pmsg.HostName = _currentGame.HostName;
                        _nwc.Send(MessageHandler.Serialize(pmsg));
                    }
                }


                //DrawField();

            } while (!gameIsWon);

            Console.Clear();
            PrintWinner(winnerName);
            Console.ReadKey(true);
            Console.Clear();
        }



        //[DllImport("msvcr71.dll")]
        //static extern int _kbhit();

        //[DllImport("msvcr71.dll")]
        //static extern char _getwch();

        //// return true/false if a keypress is pending
        //public static bool KeyPressed
        //{
        //    get
        //    {
        //        return (_kbhit() != 0);
        //    }
        //}

        //// fetch next char in buffer else null - return immediately
        //public static char GetChar()
        //{
        //    return (KeyPressed) ? _getwch() : NullChar;
        //}

        //// flush all pending keystrokes in buffer - return immediately
        //public static void FlushKeyboardBuffer()
        //{
        //    while (KeyPressed) GetChar();
        //}

        private void RunSinglePlayer()
        {
            bool gameIsWon = false;
            _playField = new Square[_playFieldWidth, _playFieldHeight];
            CreatePlayField();
            SinglePlayer.ClearPosList();
            _currentGame.PlayerList.Clear();
            _player = SinglePlayer.CreateOwnPlayer();
            _currentGame.PlayerList.Add(_player);

            int numberOfPlayers = UserInput.GetIntFiltered("Ange antal spelare (2-8)", 2, 8);

            string[] tmpNames = { "Wall-E", "HAL 9000", "SkyNet", "AlphaGo", "Deep Blue", "GLaDOS", "T-1000",
                                  "R2-D2" , "C-3PO", "Ava", "Bender", "Baymax", "Bastion", "Optimus Prime", "Sir Killalot",
                                  "RX-78-2 Gundam", "Tay", "MechaGodzilla", "Agent Smith", "Tron", "FemBot", "Borg Queen",
                                  "TARS", "Data", "RoboCop", "Roy Batty", "The Iron Giant", "Siri"};
            Random rnd = new Random();
            string[] tmpNamesRnd = tmpNames.OrderBy(x => rnd.Next()).ToArray();

            for (int i = 1; i < numberOfPlayers; i++)
            {
                int[] startPos = SinglePlayer.RandomizeStartPos();
                Player p = new Player(tmpNamesRnd[i]) { Color=SinglePlayer.GetColor(i), Xpos = startPos[0], Ypos = startPos[1] };

                _currentGame.PlayerList.Add(p);
            }

            InsertPlayers();

            do
            {
                while(Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }

                for (int i = 3; i > 0; i--)
                    if (!HandleMovementSP(i))
                    {
                        gameIsWon = true;
                        Console.Clear();
                        break;
                    }


                if (!gameIsWon)
                {
                    for (int i = 1; i < _currentGame.PlayerList.Count; i++)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            int[] aiMoves = HandleAImovement(_currentGame.PlayerList[i]);
                            _currentGame.PlayerList[i].Xpos = aiMoves[0];
                            _currentGame.PlayerList[i].Ypos = aiMoves[1];
                            _playField[aiMoves[0], aiMoves[1]].Color = _currentGame.PlayerList[i].Color;
                            _playField[aiMoves[0], aiMoves[1]].isOccupied = true;
                            DrawField(0);
                            Thread.Sleep(200);
                            if (!_currentGame.PlayerList[i].IsAlive)
                            {
                                _currentGame.PlayerList.Remove(_currentGame.PlayerList[i]); //todo: added
                                break;
                            }
                        }
                        Thread.Sleep(400);
                    }

                    int playersLeft = 0;
                    foreach (var item in _currentGame.PlayerList)
                    {
                        if (item.IsAlive)
                            playersLeft += 1;
                    }

                    if (playersLeft == 1)
                        gameIsWon = true;
                }

            } while (!gameIsWon);

            if (_player.IsAlive)
            {
                Console.Clear();
                PrintWinner(_player.PlayerName);
                Console.ReadKey();
                Console.Clear();
            } else
            {
                Console.Clear();
                Console.WriteLine("You died :(");
                Console.ReadKey();
                Console.Clear();
            }

        }

        private bool CanMoveUp(Player p)
        {
            return p.Ypos > 0 && _playField[p.Xpos, p.Ypos - 1].isOccupied == false;
        }

        private bool CanMoveDown(Player p)
        {
            return p.Ypos < _playFieldHeight - 1 && _playField[p.Xpos, p.Ypos + 1].isOccupied == false;
        }

        private bool CanMoveRight(Player p)
        {
            return p.Xpos < _playFieldWidth - 1 && _playField[p.Xpos + 1, p.Ypos].isOccupied == false;
        }

        private bool CanMoveLeft(Player p)
        {
            return p.Xpos > 0 && _playField[p.Xpos - 1, p.Ypos].isOccupied == false;
        }

        public int[] HandleAImovement(Player player)
        {
            int[] result = new int[2] { player.Xpos, player.Ypos };

            if (player.Direction == Direction.None)
            {
                player.Direction = (Direction)Randomizer.Rng(1, 5);
            }

            if (Randomizer.Try(70)) // 70% to try moving forward first
            {

                #region move up
                if (player.Direction == Direction.Up) //One of 4 possible directions
                {
                    if (CanMoveUp(player))
                    {
                        result[1] -= 1;
                    } else if (Randomizer.Try(50)) //cant move forward, 50% to try moving right first
                    {
                        if (CanMoveRight(player))
                        {
                            player.Direction = Direction.Right;
                            result[0] += 1;
                        } else if (CanMoveLeft(player))
                        {
                            player.Direction = Direction.Left;
                            result[0] -= 1;
                        }
                        else
                        {
                            player.IsAlive = false;
                        }

                    } else                          //50% failed, try moving left first
                    {
                        if (CanMoveLeft(player))
                        {
                            player.Direction = Direction.Left;
                            result[0] -= 1;
                        } else if (CanMoveRight(player))
                        {
                            player.Direction = Direction.Right;
                            result[0] += 1;
                        }
                        else
                        {
                            player.IsAlive = false;
                        }
                    }
                }
                #endregion

                #region move down
                else if (player.Direction == Direction.Down) //One of 4 possible directions
                {
                    if (CanMoveDown(player))
                    {
                        result[1] += 1;
                    }
                    else if (Randomizer.Try(50))
                    {
                        if (CanMoveRight(player))
                        {
                            player.Direction = Direction.Right;
                            result[0] += 1;
                        }
                        else if (CanMoveLeft(player))
                        {
                            player.Direction = Direction.Left;
                            result[0] -= 1;
                        }
                        else
                        {
                            player.IsAlive = false;
                        }

                    } else
                    {
                        if (CanMoveLeft(player))
                        {
                            player.Direction = Direction.Left;
                            result[0] -= 1;
                        }
                        else if (CanMoveRight(player))
                        {
                            player.Direction = Direction.Right;
                            result[0] += 1;
                        }
                        else
                        {
                            player.IsAlive = false;
                        }
                    }
                }
                #endregion

                #region move right
                else if (player.Direction == Direction.Right) //One of 4 possible directions
                {
                    if (CanMoveRight(player))
                    {
                        result[0] += 1;
                    } else if (Randomizer.Try(50))
                    {
                        if (CanMoveUp(player))
                        {
                            player.Direction = Direction.Up;
                            result[1] -= 1;
                        } else if (CanMoveDown(player))
                        {
                            player.Direction = Direction.Down;
                            result[1] += 1;
                        }
                        else
                        {
                            player.IsAlive = false;
                        }
                    } else
                    {
                        if (CanMoveDown(player))
                        {
                            player.Direction = Direction.Down;
                            result[1] += 1;
                        } else if (CanMoveUp(player))
                        {
                            player.Direction = Direction.Up;
                            result[1] -= 1;
                        }
                        else
                        {
                            player.IsAlive = false;
                        }
                    }
                }
                #endregion

                #region move left
                else if (player.Direction == Direction.Left) //One of 4 possible directions
                {
                    if (CanMoveLeft(player))
                    {
                        result[0] -= 1;
                    } else if (Randomizer.Try(50))
                    {
                        if (CanMoveUp(player))
                        {
                            player.Direction = Direction.Up;
                            result[1] -= 1;
                        }
                        else if (CanMoveDown(player))
                        {
                            player.Direction = Direction.Down;
                            result[1] += 1;
                        }
                        else
                        {
                            player.IsAlive = false;
                        }
                    } else
                    {
                        if (CanMoveDown(player))
                        {
                            result[1] += 1;
                        }
                        else if (CanMoveUp(player))
                        {
                            result[1] -= 1;
                        }
                        else
                        {
                            player.IsAlive = false;
                        }
                    }
                }
                #endregion
            }
            //end 70%

            else if (Randomizer.Try(50)) //moving forward failed, 50% to try right first
            {
                #region move up
                if (player.Direction == Direction.Up)
                {
                    if (CanMoveRight(player))
                    {
                        player.Direction = Direction.Right;
                        result[0] += 1;
                    }
                    else if (CanMoveLeft(player))
                    {
                        player.Direction = Direction.Left;
                        result[0] -= 1;
                    }
                    else if (CanMoveUp(player))
                    {
                        player.Direction = Direction.Up;
                        result[1] -= 1;
                    } else
                    {
                        player.IsAlive = false;
                    }
                }
                #endregion

                #region move down
                else if (player.Direction == Direction.Down)
                {
                    if (CanMoveLeft(player))
                    {
                        player.Direction = Direction.Left;
                        result[0] -= 1;
                    }
                    else if (CanMoveRight(player))
                    {
                        player.Direction = Direction.Right;
                        result[0] += 1;
                    }
                    else if (CanMoveDown(player))
                    {
                        player.Direction = Direction.Down;
                        result[1] += 1;
                    }
                    else
                    {
                        player.IsAlive = false;
                    }
                }
                #endregion

                #region move left
                else if (player.Direction == Direction.Left)
                {
                    if (CanMoveUp(player))
                    {
                        player.Direction = Direction.Up;
                        result[1] -= 1;
                    }
                    else if (CanMoveDown(player))
                    {
                        player.Direction = Direction.Down;
                        result[1] += 1;
                    }
                    else if (CanMoveLeft(player))
                    {
                        player.Direction = Direction.Left;
                        result[0] -= 1;
                    } else
                    {
                        player.IsAlive = false;
                    }
                }
                #endregion

                #region move right
                else if (player.Direction == Direction.Right)
                {
                    if (CanMoveDown(player))
                    {
                        player.Direction = Direction.Down;
                        result[1] += 1;
                    }
                    else if (CanMoveUp(player))
                    {
                        player.Direction = Direction.Up;
                        result[1] -= 1;
                    }
                    else if (CanMoveRight(player))
                    {
                        player.Direction = Direction.Right;
                        result[0] += 1;
                    }
                    else
                    {
                        player.IsAlive = false;
                    }
                }
                #endregion
            }

            else //otherwise try left first
            {
                #region move up
                if (player.Direction == Direction.Up)
                {
                    if (CanMoveLeft(player))
                    {
                        player.Direction = Direction.Left;
                        result[0] -= 1;
                    }
                    else if (CanMoveRight(player))
                    {
                        player.Direction = Direction.Right;
                        result[0] += 1;
                    }
                    else if (CanMoveUp(player))
                    {
                        player.Direction = Direction.Up;
                        result[1] -= 1;
                    }
                    else
                    {
                        player.IsAlive = false;
                    }
                }
                #endregion

                #region move down
                else if (player.Direction == Direction.Down)
                {
                    if (CanMoveRight(player))
                    {
                        player.Direction = Direction.Right;
                        result[0] += 1;
                    }
                    else if (CanMoveLeft(player))
                    {
                        player.Direction = Direction.Left;
                        result[0] -= 1;
                    }
                    else if (CanMoveDown(player))
                    {
                        player.Direction = Direction.Down;
                        result[1] += 1;
                    }
                    else
                    {
                        player.IsAlive = false;
                    }
                }

                #endregion

                #region move left
                else if (player.Direction == Direction.Left)
                {
                    if (CanMoveDown(player))
                    {
                        player.Direction = Direction.Down;
                        result[1] += 1;
                    }
                    else if (CanMoveUp(player))
                    {
                        player.Direction = Direction.Up;
                        result[1] -= 1;
                    }
                    else if (CanMoveLeft(player))
                    {
                        player.Direction = Direction.Left;
                        result[0] -= 1;
                    }
                    else
                    {
                        player.IsAlive = false;
                    }
                }
                #endregion

                #region move right
                else if (player.Direction == Direction.Right)
                {
                    if (CanMoveUp(player))
                    {
                        player.Direction = Direction.Up;
                        result[1] -= 1;
                    }
                    else if (CanMoveDown(player))
                    {
                        player.Direction = Direction.Down;
                        result[1] += 1;
                    }
                    else if (CanMoveRight(player))
                    {
                        player.Direction = Direction.Right;
                        result[0] += 1;
                    }
                    else
                    {
                        player.IsAlive = false;
                    }
                }
                #endregion
            }

            return result;
        }

        private bool HandleMovementSP(int movesLeft)
        {
            DrawField(movesLeft);
            int newX = _player.Xpos;
            int newY = _player.Ypos;
            bool validMove = false;

            do
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow:
                        if (_player.Direction != Direction.Left)
                        {
                            _player.Direction = Direction.Right;
                            newX++;
                            validMove = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow:
                        if (_player.Direction != Direction.Right)
                        {
                            _player.Direction = Direction.Left;
                            newX--;
                            validMove = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        if (_player.Direction != Direction.Up)
                        {
                            _player.Direction = Direction.Down;
                            newY++;
                            validMove = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        if (_player.Direction != Direction.Down)
                        {
                            _player.Direction = Direction.Up;
                            newY--;
                            validMove = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    default:
                        break;
                }
            } while (!validMove);

            try
            {
                if (_playField[newX, newY].isOccupied)
                {
                    _player.IsAlive = false;
                }
                else
                {
                    _playField[newX, newY].Color = _player.Color;
                    _playField[newX, newY].isOccupied = true;
                    _player.Xpos = newX;
                    _player.Ypos = newY;
                }
            }
            catch (IndexOutOfRangeException IORex)
            {
                _player.IsAlive = false;
            }

            return _player.IsAlive;
        }

        private void PrintWinner(string winnerName)
        {
            int leftoffset = 3;
            int topoffset = 2;
            for (int i = 0; i < 20; i++)
            {
                for (int y = 0; y < 12; y++)
                {
                    for (int x = 0; x < 6; x++)
                    {
                        int color = Randomizer.Rng(0, 6);
                        if (color == 0)
                            Console.ForegroundColor = ConsoleColor.Red;
                        if (color == 1)
                            Console.ForegroundColor = ConsoleColor.Blue;
                        if (color == 2)
                            Console.ForegroundColor = ConsoleColor.Green;
                        if (color == 3)
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        if (color == 4)
                            Console.ForegroundColor = ConsoleColor.Magenta;
                        if (color == 5)
                            Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.SetCursorPosition((x * 14) + leftoffset, (y * 2) + topoffset);
                        Console.Write(winnerName);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                Thread.Sleep(30);
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Tryck på en knapp för att fortsätta");
        }


        /// <summary>
        /// This function will from a PlayMessage add colour and set IsOccupied property of affected squares.
        /// </summary>
        /// <param name="apm"> The parameter is a play message recived from the server and should not be _player</param>
        private void DrawOponents(PlayMessage apm) // JE
        {
            // Read player and find corresponding colour.
            Player Oponent = _currentGame.PlayerList.Where(x => x.PlayerName == apm.UserName).SingleOrDefault();
            ConsoleColor oponentColour = Oponent.Color;

            // Read movement (get xMovement, get yMovement)
            // add colour and occupied to square.
            for (int i = 0; i < apm.MoveList.Count; i++)
            {

                int[] move = apm.MoveList[i];
                int x = move[0];
                int y = move[1];

                if (x == -1 || y == -1)
                    break;

                Square square = _playField[x, y];
                square.Color = oponentColour;
                square.isOccupied = true;
            }

        }

        private PlayMessage WaitForPlayMessage()
        {
            {
                bool valid = false;
                Stopwatch myclock = new Stopwatch();
                myclock.Start();
                PlayMessage tmp = new PlayMessage("<empty>");

                do
                {
                    Thread.Sleep(50);
                    foreach (var item in _nwc._commandList)
                    {
                        if (item is PlayMessage)
                        {
                            tmp = item as PlayMessage;

                            foreach (var player in _currentGame.PlayerList)
                            {
                                if (player.PlayerName == tmp.UserName)
                                    player.IsAlive = tmp.IsAlive;

                            }

                            PlayMessage result = new PlayMessage(item.UserName);
                            result = tmp;
                            _nwc._commandList.Remove(item);
                            return result;
                        }
                    }


                    if (myclock.ElapsedMilliseconds > 45000)
                    {
                        Console.WriteLine("WaitForPlayMessage timeout");
                        return tmp;
                    }
                } while (!valid);
                return tmp;
            }
        }

        private void InsertPlayers()
        {
            foreach (var item in _currentGame.PlayerList)
            {
                _playField[item.Xpos, item.Ypos].Color = item.Color;
                _playField[item.Xpos, item.Ypos].isOccupied = true;
            }
        }

        private void SetGameProperties(StartGameMessage tmp)
        {
            _currentGame = tmp.GameRoomInfo;
            _player = _currentGame.PlayerList.Where(p => p.PlayerName == _player.PlayerName).SingleOrDefault();
        }
    }
}
