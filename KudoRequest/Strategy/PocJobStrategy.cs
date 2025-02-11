using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace KudoRequest.Strategy
{
    public class PocJobStrategy : IWebJobStrategy
    {
        public string SetParameter()
        {
            Console.WriteLine("WebPocを実行します。");
            Console.WriteLine("起動パラメータを指定してください。");

            var userInput = Console.ReadLine();            
            
            if (userInput is null) 
            {
                return string.Empty;
            }
            return HttpUtility.UrlEncode(userInput);
        }
    }

}
