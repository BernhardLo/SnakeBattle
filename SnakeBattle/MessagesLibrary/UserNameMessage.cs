using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagesLibrary
{
    public class UserNameMessage : Message
    {
        public UserNameMessage(string userName) : base (userName)
        {
            this.UserNameConfirm = false;
        }
        public bool UserNameConfirm { get; set; }
    }
}
