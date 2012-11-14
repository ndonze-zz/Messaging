using System;
using System.Threading;
using System.Threading.Tasks;
using Messaging.Messages;
using ServiceStack.Text;

namespace Messaging.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var workerId = Guid.NewGuid();
            SubscribeToWorkReceived(workerId);

            var workerAvailable = new WorkerAvailable { MessageId = Guid.NewGuid(), WorkerId = workerId };
            Publish(Channels.WorkerAvailable, workerAvailable.ToString());

            Console.ReadLine();
        }

        public static void SubscribeToWorkReceived(Guid workerId)
        {
            Action<string, string> onWorkReceived = (channel, msg) =>
            {
                var work = JsonSerializer.DeserializeFromString<Work>(msg);
                Console.WriteLine("[{0}] Performing work: {1}",
                    DateTime.Now.ToShortTimeString(),
                    work.Message);

                Thread.Sleep(10000);

                var workComplete = new WorkComplete { MessageId = Guid.NewGuid(), CorrelationId = work.MessageId };
                Publish(Channels.WorkComplete, workComplete.ToString());

                var workerAvailable = new WorkerAvailable { MessageId = Guid.NewGuid(), WorkerId = workerId };
                Publish(Channels.WorkerAvailable, workerAvailable.ToString());
            };

            Task.Factory.StartNew(() => Subscribe(onWorkReceived, workerId.ToString()));
        }

        private static void Publish(string channel, string message)
        {
            using (var client = new LocalClient())
            {
                client.PublishMessage(channel, message);
            }
        }

        private static void Subscribe(Action<string, string> action, params string[] channels)
        {
            using (var client = new LocalClient())
            using (var sub = client.CreateSubscription())
            {
                sub.OnMessage = action;
                sub.SubscribeToChannels(channels);
            }
        }
    }
}
