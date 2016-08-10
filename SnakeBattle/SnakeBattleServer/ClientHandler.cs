using MessagesLibrary;
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

                    var msg = MessageHandler.Deserialize(message);

                    if (msg.GetType() == typeof(UserNameMessage))
                    {
                        UserNameMessage response = new UserNameMessage(msg.UserName);
                        response.UserNameConfirm = myServer.CheckUserName(msg.UserName);

                        myServer.PrivateSend(tcpclient ,MessageHandler.Serialize( response));
                        
                    }

                    //myServer.Broadcast(this, message);
                    Console.WriteLine(message);
                }

                myServer.DisconnectClient(this);
                tcpclient.Close();
            }
            catch (IOException)
            {
                Console.WriteLine("Remote client disconnected" + tcpclient.Client.AddressFamily.ToString());
                //TO-DO: Visa IP på användaren som lämnar
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

