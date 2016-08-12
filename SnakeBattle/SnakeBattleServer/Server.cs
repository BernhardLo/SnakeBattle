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
    class Server
    {
        List<ClientHandler> _clients = new List<ClientHandler>();
        public List<GameRoom> _games = new List<GameRoom>();
        public void Run()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 5000);
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

        internal string GetNextUser(PlayMessage pm)
        {
            string result = "";

            GameRoom gr = _games.Where(g => g.HostName == pm.HostName).SingleOrDefault();
            Player temp = gr.PlayerList.Where(p => p.PlayerName == pm.UserName).SingleOrDefault();
            int j = gr.PlayerList.IndexOf(temp);
            if (j == gr.PlayerList.Count-1)
                j = 0;
            else
                j += 1;

            result = gr.PlayerList[j].PlayerName;

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
            Console.WriteLine("Broadcasting: " +message);
            foreach (ClientHandler tmpClient in _clients)
            {
                NetworkStream n = tmpClient.tcpclient.GetStream();
                BinaryWriter w = new BinaryWriter(n);
                w.Write(message);
                w.Flush();
            }
        }

        public void DisconnectClient(ClientHandler client)
        {
            Console.WriteLine(client.UserName + " has left the building...");
            //Broadcast(client, "Client X has left the building...");
            _clients.Remove(client);
        }

        internal void SendStartGameMessage(string hostName)
        {
            GameRoom gr = _games.Where(c => c.HostName == hostName).SingleOrDefault();
            string tmpStartingPlayer = gr.PlayerList[1].PlayerName; //todo: slumpa startspelare
            int xPos = 1;
            int yPos = 1;
            ConsoleColor[] tmpColors = {ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.White};

            //todo: slumpa ut startpositioner

            for (int i = 0; i < gr.PlayerList.Count; i++)
            {
                gr.PlayerList[i].Xpos = xPos;
                gr.PlayerList[i].Ypos = yPos;
                yPos+=4; xPos+=4;
                gr.PlayerList[i].Color = tmpColors[i];
            }
            gr.StartingPlayer = tmpStartingPlayer;

            StartGameMessage sgm = new StartGameMessage(hostName)
            {
                GameRoomInfo = gr
            };

            Broadcast(MessageHandler.Serialize(sgm));
        }
    }
}
