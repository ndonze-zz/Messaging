using System;
using Messaging.Messages;
namespace Messaging.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            Func<string> prompt = () =>
            {
                Console.Write("Enter your message: ");
                return Console.ReadLine();
            };

            string message;
            while (!string.IsNullOrEmpty(message = prompt()))
            {
                using (var client = new LocalClient())
                {
                    var work = new Work { MessageId = Guid.NewGuid(), Message = message };
                    client.PublishMessage(Channels.Distribution, work.ToString()); 
                }
            }
        }
    }
}
