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

        public void Broadcast(ClientHandler client, string message)
        {
            foreach (ClientHandler tmpClient in _clients)
            {
                if (tmpClient != client)
                {
                    NetworkStream n = tmpClient.tcpclient.GetStream();
                    BinaryWriter w = new BinaryWriter(n);
                    w.Write(message);
                    w.Flush();
                }
                else if (_clients.Count() == 1)
                {
                    NetworkStream n = tmpClient.tcpclient.GetStream();
                    BinaryWriter w = new BinaryWriter(n);
                    w.Write("Sorry, you are alone...");
                    w.Flush();
                }
            }
        }

        public void DisconnectClient(ClientHandler client)
        {
            Console.WriteLine(client.UserName + " has left the building...");
            //Broadcast(client, "Client X has left the building...");
            _clients.Remove(client);
        }
    }
}
