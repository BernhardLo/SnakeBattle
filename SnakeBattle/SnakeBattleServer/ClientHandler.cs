﻿using MessagesLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeBattleServer
{
    class ClientHandler
    {
        public string UserName { get; set; }
        public TcpClient tcpclient;
        private Server myServer;
        public ClientHandler(TcpClient c, Server server)
        {
            tcpclient = c;
            this.myServer = server;
            this.UserName = "<empty>";
        }

        public void Run()
        {
            try
            {
                string message = "";
                while (!message.Equals("quit"))
                {
                    NetworkStream n = tcpclient.GetStream();
                    message = new BinaryReader(n).ReadString();
                    Console.WriteLine(message);
                    var msg = MessageHandler.Deserialize(message);

                    if (msg.GetType() == typeof(UserNameMessage))
                    {
                        UserNameMessage response = new UserNameMessage(msg.UserName);
                        response.UserNameConfirm = myServer.CheckUserName(msg.UserName);
                        this.UserName = response.UserName;
                        myServer.PrivateSend(tcpclient, MessageHandler.Serialize(response));
                    }

                    //myServer.Broadcast(this, message);
                }

                myServer.DisconnectClient(this);
                tcpclient.Close();
                //todo: ta bort användarenn ur clientlistan
            }
            catch (IOException)
            {
                Console.WriteLine("Remote client disconnected: " + tcpclient.Client.AddressFamily.ToString());
                //todo: Visa IP på användaren som lämnar och ta bort ur clientlistan
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

