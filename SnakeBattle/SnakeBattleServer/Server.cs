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
        List<int[]> startPos = new List<int[]>() {
            //new int[2] { 2 , 2 },
            //new int[2] { 7 , 2 },
            //new int[2] { 13 , 2 },
            //new int[2] { 17 , 3 },
            //new int[2] { 4 , 5 },
            //new int[2] { 9 , 4 },
            //new int[2] { 12 , 5 },
            //new int[2] { 18 , 5 },
            //new int[2] { 3 , 9 },
            //new int[2] { 8 , 12 },
            //new int[2] { 13 , 10 },
            //new int[2] { 16 , 9 },
            //new int[2] { 4 , 15 },
            //new int[2] { 10 , 14 },
            //new int[2] { 16 , 16 },
            //new int[2] { 6 , 14 },
            //new int[2] { 7 , 17 },
            //new int[2] { 17 , 17 },
            //new int[2] { 5 , 18 }
        };
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
            _clients.Remove(client);
        }

        internal void SendStartGameMessage(string hostName)
        {
            GameRoom gr = _games.Where(c => c.HostName == hostName).SingleOrDefault();
            string tmpStartingPlayer = gr.PlayerList[1].PlayerName; //todo: slumpa startspelare
            int xPos = 1;
            int yPos = 1;
            ConsoleColor[] tmpColors = { ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.Yellow };

            //todo: slumpa ut startpositioner

            for (int i = 0; i < gr.PlayerList.Count; i++)
            {
                bool validPlacement = false;
                do
                {
                    int x = Randomizer.Rng(1, 20);
                    int y = Randomizer.Rng(1, 20);
                    if (!startPos.Contains(new int[2] { x, y }))
                    {
                        gr.PlayerList[i].Xpos = x;
                        gr.PlayerList[i].Ypos = y;
                        startPos.Add(new int[2] { x, y });
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
