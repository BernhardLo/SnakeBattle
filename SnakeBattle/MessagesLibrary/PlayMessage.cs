using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesLibrary
{
    class PlayMessage : Message
    {
        public PlayMessage(string userName) : base(userName)
        {

        }
        //Anger vilken spelomgång som pågår, också hjälp för severn att identifiera rätt svar
        public int TurnCount { get; set; }
        public int[] XMovements { get; set; }
        public int[] YMovements { get; set; }
        public bool IsAlive { get; set; }
        public string NextUser { get; set; }
        public int PowerUp { get; set; }
    }
}
