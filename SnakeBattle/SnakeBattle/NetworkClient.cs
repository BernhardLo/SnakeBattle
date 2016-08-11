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
        public List<Message> _commandList = new List<Message>();

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
            if (msg is UserNameMessage)
            {
                Console.WriteLine("adding " +msg.UserName); //todo: "test"
                _commandList.Add(msg);
                Console.WriteLine("Nu kom det ett username-message"); //todo: "Test"
            }
            else if (msg is FindGameMessage)
            {
                _commandList.Add(msg);
                Console.WriteLine("Nu kom det en lista av spelrum"); //todo: "test
                
            }
            else if (msg is StartGameMessage)
            {
                _commandList.Add(msg); // todo: Kolla om hostname är aktuellt
            }
            else if (msg is PlayMessage)
            {
                _commandList.Add(msg); // todo: Kolla om hostname är aktuellt
            }
            else if (msg is JoinGameMessage)
            {
                _commandList.Add(msg); // todo: Kolla om hostname är aktuellt
            }
            else if (msg is ErrorMessage)
            {
                _commandList.Add(msg); // todo: Kolla om hostname är aktuellt
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
