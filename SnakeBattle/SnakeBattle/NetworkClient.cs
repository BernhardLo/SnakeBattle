using MessagesLibrary;
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
        private TcpClient serverClient;
        private List<Message> _commandList = new List<Message>();

        public bool Connect(string ip, int port)
        {
            bool connectSucceeded = false;

            try
            {
                serverClient = new TcpClient(ip, port);
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
                    NetworkStream n = serverClient.GetStream();
                    message = new BinaryReader(n).ReadString();

                    CommandListAdd(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void CommandListAdd(string message)
        {
            var msg = MessageHandler.Deserialize(message);
            if (msg.GetType() == typeof(UserNameMessage))
            {
                _commandList.Add(msg);
            }
            else if (true)
            {

            }
        }

        public void Send(string message)
        {
            try
            {
                NetworkStream nws = serverClient.GetStream();

                BinaryWriter bnw = new BinaryWriter(nws);
                bnw.Write(message);
                bnw.Flush();

                if (message.Equals("quit"))
                    serverClient.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

    }
}
