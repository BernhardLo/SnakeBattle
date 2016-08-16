using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SnakeBattle
{
    static class UserInput
    {

        public static string GetIp()
        {
            IPAddress ip;
            string ret = "";
            bool valid = false;
            do
            {
                Console.Write("Skriv in IP-adress till servern: ");
                ret = GetString();
                
                if (!String.IsNullOrWhiteSpace(ret))
                {
                    if (IPAddress.TryParse(ret, out ip))
                    {
                        valid = true;
                    }
                }

            } while (!valid);

            return ret;
        }

        public static string GetUserName()
        {
            string userName = "";
            bool valid = false;
            do
            {
                Console.Write("Ange användarnamn: ");
                userName = GetString();
                if (!String.IsNullOrWhiteSpace(userName))
                {

                    string pattern = @"^[a-zA-Z0-9åäöÅÄÖ]+$";
                    Match result = Regex.Match(userName, pattern);
                    if (userName.Length < 14 && userName.Length > 2 && userName != "<empty>" && result.Success)
                    {
                        valid = true;
                    }
                }
            } while (!valid);
            return userName;
        }

        public static string GetString()
        {
            string ret = "";
            try
            {
                ret = Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            return ret;
        }

        public static int GetInt()
        {
            int ret = 9;
            try
            {
                ret = Convert.ToInt32(Console.ReadLine());
            }
            catch (FormatException fex)
            {
                Console.Write("Input is not integer.");
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            return ret;
        }

        public static int GetIntFiltered(string message, int min, int max)
        {
            int result;
            bool valid = false;
            do
            {
                Console.Write(message);
                result = GetInt();
                if (result >= min && result <= max)
                    valid = true;

            } while (!valid);
            return result;
        }
    }
}
