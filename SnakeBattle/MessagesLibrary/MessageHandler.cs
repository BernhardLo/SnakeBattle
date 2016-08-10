using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MessagesLibrary
{
    public class MessageHandler
    {
        public string Serialize(Object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        //public Message Deserialize (string jsonString)
        //{
        //    Message result = new Message();


        //    //to do : deserialize

        //    return result;
        //}
    }
}
