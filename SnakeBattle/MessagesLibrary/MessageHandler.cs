﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MessagesLibrary
{
    public static class MessageHandler
    {
        public static string Serialize(Message obj)
        {
            string command = "";
            if (obj.GetType() == typeof(UserNameMessage))
                command = "un";
            else if (obj.GetType() == typeof(NewGameMessage))
                command = "ng";
            else if (obj.GetType() == typeof(PlayMessage))
                command = "pm";
            else if (obj.GetType() == typeof(JoinGameMessage))
                command = "jg";
            else
                command = "er";

            return command + JsonConvert.SerializeObject(obj);
        }

        public static Message Deserialize(string jsonString)
        {
            Message result;
            string commandType = jsonString.Substring(0, 2);
            string message = jsonString.Substring(2);

            if (commandType == "un")
                result = JsonConvert.DeserializeObject<UserNameMessage>(message);
            else if (commandType == "ng")
                result = JsonConvert.DeserializeObject<NewGameMessage>(message);
            else if (commandType == "pm")
                result = JsonConvert.DeserializeObject<PlayMessage>(message);
            else if (commandType == "jg")
                result = JsonConvert.DeserializeObject<JoinGameMessage>(message);
            else if (commandType == "er")
                result = JsonConvert.DeserializeObject<ErrorMessage>(message);
            else
                result = new ErrorMessage ("Error"){ EMessage = "Something went terribly wrong" };

            return result;
        }
    }
}
