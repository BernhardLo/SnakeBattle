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
            List<string> tmpStartPosList = new List<string>();
            List<int> tmpColorList = new List<int>();
            string tmpStartingPlayer = gr.Gamers[1]; //todo: slumpa startspelare
            int xPos = 1;
            int yPos = 1;

            //todo: slumpa ut startpositioner

            for (int i = 0; i < gr.NumberOfPlayers; i++)
            {
                tmpStartPosList.Add("0" + xPos.ToString() + "0" + yPos.ToString());
                yPos+=2; xPos+=2;
                tmpColorList.Add(i);
            }

            gr.PlayerStartPositions = tmpStartPosList;
            gr.PlayerColors = tmpColorList;
            gr.StartingPlayer = tmpStartingPlayer;

            StartGameMessage sgm = new StartGameMessage(hostName)
            {
                GameRoomInfo = gr
            };

            Thread.Sleep(20);
            Broadcast(MessageHandler.Serialize(sgm));
        }
    }
}
