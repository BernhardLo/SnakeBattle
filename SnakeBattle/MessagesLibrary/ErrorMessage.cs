using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesLibrary
{
    class ErrorMessage : Message
    {
        public ErrorMessage(string userName) : base (userName)
        {

        }
        public string EMessage { get; set; }
    }
}
