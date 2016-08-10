using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeBattle
{
    static class UserInput
    {

        public static string GetIp()
        {
            string ret = "";
            Console.Write("Skriv in IP-adress till servern: ");
            bool valid = false;
            do
            {
                ret = GetString();
                if (String.IsNullOrWhiteSpace(ret))
                {

                }

            } while (!valid);

            return ret;
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

        public static int GetInt(string message, int min, int max)
        {
            int ret = 0;
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
    }
}
