using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesLibrary
{
    public class JoinGameMessage : Message
    {
        //todo: proppar, svarsmeddelande (answer) + hostname



        public JoinGameMessage(string userName) : base (userName)
        {

        }
    }
}
