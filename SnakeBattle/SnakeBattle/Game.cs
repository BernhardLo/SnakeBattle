using GameLogic;
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
                            _nwc._commandList.Remove(item); //todo: ändrade från tmp till item
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
                        _nwc._commandList.Remove(item); //todo: ändrade från tmp till item
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

        public void DrawField(int mod)
        {
            Console.Clear();
            if (mod != 0)
                Console.ForegroundColor = ConsoleColor.Red;

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

                Console.SetCursorPosition((_playFieldWidth*2) + 5, 3+i);
                Console.Write(_currentGame.PlayerList[i].PlayerName);
            }
            Console.SetCursorPosition(3, _playFieldHeight + 3);
            Console.Write("Antal steg kvar: ");

            for (int i = 0; i < mod; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.Write("  ");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write("  ");
            }
        }

        public int[] HandleMovement()
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            int newX = _player.Xpos;
            int newY = _player.Ypos;
            int[] result = new int[2] { -1, -1 };

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
                foreach (var item in _nwc._commandList)
                {
                    if (item is StartGameMessage)
                    {
                        StartGameMessage tmp = item as StartGameMessage;
                        Console.WriteLine("time used: " + myclock.ElapsedMilliseconds);//todo: "test"

                        SetGameProperties(tmp);

                        Console.WriteLine("removing StartGameMessage from commandlist"); //todo: "test"
                        _nwc._commandList.Remove(item); //todo : ändrade från tmp till item
                        valid = true;
                        break;
                    }
                }
                if (myclock.ElapsedMilliseconds > 500000)
                {
                    Console.WriteLine("StartGameLobby timeout");
                    valid = true;

                }
                //}

            } while (/*Console.ReadKey(true).Key != ConsoleKey.A ||*/ !valid); // todo: Lyssna efter escape

            Console.WriteLine("Dags att starta nytt spel"); //todo: "test"

            RunGame();

        }

        private void RunGame()
        {
            Console.WriteLine("spelet har börjat"); //todo: "test"
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
                DrawField(3);
                List<int[]> moveList = new List<int[]>();
                moveList.Add(HandleMovement());
                DrawField(2);
                moveList.Add(HandleMovement());
                DrawField(1);
                moveList.Add(HandleMovement());
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
                    Console.WriteLine(MessageHandler.Serialize(apm));
                    // If recived message is not player add movement to squares
                    if (apm.UserName != _player.PlayerName) // JE 
                    {
                        DrawOponents(apm);// JE
                        DrawField(0);
                    }

                    if (apm.NextUser == _player.PlayerName)
                    { //todo: fixa antal steg per tur lite snyggare
                        DrawField(3);
                        PlayMessage pmsg = new PlayMessage(_player.PlayerName);
                        List<int[]> moveList = new List<int[]>();
                        moveList.Add(HandleMovement());
                        DrawField(2);
                        moveList.Add(HandleMovement());
                        DrawField(1);
                        moveList.Add(HandleMovement());
                        DrawField(0);

                        pmsg.MoveList = moveList;
                        pmsg.IsAlive = _player.IsAlive;
                        pmsg.HostName = _currentGame.HostName;
                        _nwc.Send(MessageHandler.Serialize(pmsg));
                        Console.WriteLine(MessageHandler.Serialize(pmsg));
                    }
                }
                

                //DrawField();

            } while (!gameIsWon);

            Console.Clear(); //todo: "test"
            Console.WriteLine(winnerName + " has won the game!!!");
            Console.ReadKey(); //todo: "test"
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

                if (x == -1 && y == -1)
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
                            Console.WriteLine("PlayMessage received: " + myclock.ElapsedMilliseconds);
                            PlayMessage result = new PlayMessage(item.UserName);
                            result = tmp;
                            _nwc._commandList.Remove(item);
                            return result;
                        }
                    }


                    if (myclock.ElapsedMilliseconds > 45000)
                    {
                        Console.WriteLine("GameRoomValidated timeout");
                        return tmp;
                    }
                } while (!valid);
                return tmp;
            }
        }

        private bool WaitForTurn()
        {
            throw new NotImplementedException();
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
            Console.WriteLine("Received: " + MessageHandler.Serialize(tmp));
            _currentGame = tmp.GameRoomInfo;
            _player = _currentGame.PlayerList.Where(p => p.PlayerName == _player.PlayerName).SingleOrDefault();
        }
    }
}
