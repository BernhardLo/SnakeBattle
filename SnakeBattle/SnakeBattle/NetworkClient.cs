using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeBattle
{
    class NetworkClient
    {
        private TcpClient client;
        private List<String> commandList = new List<String>();

        public bool Connect(string ip, int port)
        {
            bool connectSucceeded = false;

            try
            {
                client = new TcpClient(ip, port);
                Thread listenerThread = new Thread(Listen);
                listenerThread.Start();
                connectSucceeded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return connectSucceeded;
        }

        public void Listen()
        {
            string message = "";

            try
            {
                while (true)
                {
                    NetworkStream n = client.GetStream();
                    message = new BinaryReader(n).ReadString();

                    commandList.Add(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void Send(string message)
        {
            try
            {
                NetworkStream nws = client.GetStream();

                BinaryWriter bnw = new BinaryWriter(nws);
                bnw.Write(message);
                bnw.Flush();

                if (message.Equals("quit"))
                    client.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

    }
}
