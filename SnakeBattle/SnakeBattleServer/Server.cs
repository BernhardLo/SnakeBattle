using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagesLibrary;
using GameLogic;

namespace SnakeBattleServer
{
    class Server : IDisposable
    {
        TcpListener listener;
        List<ClientHandler> _clients = new List<ClientHandler>();
        public List<GameRoom> _games = new List<GameRoom>();
        List<int[]> startPos = new List<int[]>();
        public void Run()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }

            Console.WriteLine($"Server IP: {localIP}");
            Console.WriteLine("Server up and running, waiting for messages...");

            try
            {
                listener.Start();

                while (true)
                {
                    TcpClient c = listener.AcceptTcpClient();
                    ClientHandler newClient = new ClientHandler(c, this);
                    _clients.Add(newClient);

                    Thread clientThread = new Thread(newClient.Run);
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }

        internal PlayMessage GetNextUser(PlayMessage pm)
        {
            PlayMessage result = pm;

            GameRoom gr = _games.Where(g => g.HostName == pm.HostName).SingleOrDefault();
            Player temp = gr.PlayerList.Where(p => p.PlayerName == pm.UserName).SingleOrDefault();

            int j = gr.PlayerList.IndexOf(temp);


            if (!pm.IsAlive)
            {
                if (gr.PlayerList.IndexOf(temp) == gr.PlayerList.Count - 1)
                {
                    j = 0;
                }
                gr.PlayerList.Remove(temp);

            }
            else if (pm.IsAlive)
            {
                if (j == gr.PlayerList.Count - 1)
                    j = 0;
                else
                    j += 1;
            }

            foreach (var item in gr.PlayerList)
                Console.WriteLine(item.PlayerName);

            result.NextUser = gr.PlayerList[j].PlayerName;
            gr.StartingPlayer = result.NextUser;

            if (gr.PlayerList.Count == 1)
            {
                result.GameIsWon = true;
                _games.Remove(gr);
            }


            return result;
        }

        internal void PrivateSend(TcpClient tcpclient, string message)
        {
            NetworkStream n = tcpclient.GetStream();
            BinaryWriter w = new BinaryWriter(n);
            w.Write(message);
            w.Flush();
            Console.WriteLine(message);
        }

        internal bool CheckUserName(string name)
        {

            foreach (var tmpClient in _clients)
            {
                if (tmpClient.UserName == name)
                {
                    return false;
                }
            }
            return true;
        }

        public void Broadcast(string message)
        {
            Console.WriteLine("Broadcasting: " + message);
            try
            {
                foreach (ClientHandler tmpClient in _clients)
                {
                    NetworkStream n = tmpClient.tcpclient.GetStream();
                    BinaryWriter w = new BinaryWriter(n);
                    w.Write(message);
                    w.Flush();
                }
            }
            catch (Exception exep)
            {
                Console.WriteLine("Broadcast error: " + exep.Message);
            }

        }

        public void DisconnectClient(ClientHandler client)
        {
            Console.WriteLine(client.UserName + " has disconnected.");
            FindPlayer(client.UserName);
            _clients.Remove(client);
        }


        /// <summary>
        /// Finds a player from a string representing the PlayerName in all existing gameRooms and removes the player from the playerList.
        /// </summary>
        /// <param name="Name"> A String Reprecnting a player to be found and removed.</param>
        public void FindPlayer(string Name) // Tested with 3 players from 1 computer. needs more testing.
        {
            GameRoom gr;
            Player pl;
            foreach (GameRoom item in _games)
            {
                if (item.PlayerList.Any(p => p.PlayerName == Name))
                {
                    gr = item;
                    pl = item.PlayerList.Where(x => x.PlayerName == Name).SingleOrDefault();
                    gr.PlayerList.Remove(pl);

                }
            }
            
        }

        internal void SendStartGameMessage(string hostName)
        {
            GameRoom gr = _games.Where(c => c.HostName == hostName).SingleOrDefault();
            string tmpStartingPlayer = gr.PlayerList[Randomizer.Rng(0, gr.PlayerList.Count)].PlayerName;
            int xPos = 1;
            int yPos = 1;
            ConsoleColor[] tmpColors = { ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.Yellow ,
                                        ConsoleColor.Magenta, ConsoleColor.Cyan, ConsoleColor.DarkCyan, ConsoleColor.DarkRed};

            startPos.Clear();
            for (int i = 0; i < gr.PlayerList.Count; i++)
            {
                bool validPlacement = false;
                do
                {
                    int x = Randomizer.Rng(2, 19);
                    int y = Randomizer.Rng(2, 19);
                    if (!startPos.Contains(new int[2] { x, y }))
                    {
                        gr.PlayerList[i].Xpos = x;
                        gr.PlayerList[i].Ypos = y;
                        startPos.Add(new int[2] { x-1, y-1 });
                        startPos.Add(new int[2] { x-1, y });
                        startPos.Add(new int[2] { x-1, y+1 });
                        startPos.Add(new int[2] { x, y-1 });
                        startPos.Add(new int[2] { x, y });
                        startPos.Add(new int[2] { x, y+1 });
                        startPos.Add(new int[2] { x+1, y-1 });
                        startPos.Add(new int[2] { x+1, y });
                        startPos.Add(new int[2] { x+1, y+1 });
                        validPlacement = true;
                    }

                } while (!validPlacement);

                //yPos += 4; xPos += 4;
                gr.PlayerList[i].Color = tmpColors[i];
            }
            gr.StartingPlayer = tmpStartingPlayer;

            StartGameMessage sgm = new StartGameMessage(hostName)
            {
                GameRoomInfo = gr
            };

            Broadcast(MessageHandler.Serialize(sgm));
        }

        public void Dispose()
        {
            //foreach (var item in _clients)
            //{
            //    item.Exit();
            //}
            //Console.WriteLine("Die Die");
        }
        public void Exit()
        {
            foreach (ClientHandler tmpClient in _clients)
            {
                try
                {
                    Console.WriteLine(tmpClient.UserName);
                    NetworkStream n = tmpClient.tcpclient.GetStream();
                    BinaryWriter w = new BinaryWriter(n);
                    var msg = new ErrorMessage("Server") { EMessage = " Server shutting down" };
                    w.Write(MessageHandler.Serialize(msg));
                    w.Flush();
                }
                catch (Exception exx)
                {
                    Console.WriteLine("Exit Method failed: " + exx.Message);
                }
                tmpClient.tcpclient.Close();



            }
            Console.WriteLine("Server Shuting down");
            if (listener != null)
                listener.Stop();
        }
    }
}
