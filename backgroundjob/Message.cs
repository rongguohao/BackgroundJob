using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backgroundjob
{
    public interface IMessageService
    {
        void SendMessage(string msg);
    }

    public class MessageService : IMessageService
    {
        public void SendMessage(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"发送消息{msg}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
