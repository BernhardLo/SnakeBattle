using GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesLibrary
{
   public class FindGameMessage:Message
    {
        public List<GameRoom> GamesAvailable { get; set; }


        public FindGameMessage(string userName):base(userName)
        {
            GamesAvailable = new List<GameRoom>();
        }


    }
}
