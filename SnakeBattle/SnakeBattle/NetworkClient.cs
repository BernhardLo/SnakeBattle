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
        private TcpClient _serverClient;
        public List<Message> _commandList = new List<Message>();
        internal string _filterUserName = "<empty>";
        internal string _filterHostName = "<empty>";

        public bool Connect(string ip, int port)
        {
            bool connectSucceeded = false;

            try
            {
                _serverClient = new TcpClient(ip, port);
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
                    NetworkStream n = _serverClient.GetStream();
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
                _commandList.Add(msg);
            }
            else if (msg is FindGameMessage)
            {
                _commandList.Add(msg);
                
            }
            else if (msg is StartGameMessage)
            {
                Thread.Sleep(500); //todo: fulfix av thread.sleep
                if (msg.UserName == _filterHostName)
                    _commandList.Add(msg);

            }
            else if (msg is PlayMessage)
            {
                PlayMessage tmp = msg as PlayMessage;
                if (tmp.HostName == _filterHostName)
                    _commandList.Add(msg);
            }
            else if (msg is JoinGameMessage)
            {
                if (msg.UserName == _filterUserName)
                    _commandList.Add(msg);
            }
            else if (msg is ErrorMessage)
            {
                _commandList.Add(msg);
            }

        }

        public void Send(string message)
        {
            try
            {
                NetworkStream nws = _serverClient.GetStream();

                BinaryWriter bnw = new BinaryWriter(nws);
                bnw.Write(message);
                bnw.Flush();

                if (message.Equals("quit"))
                    _serverClient.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

    }
}
