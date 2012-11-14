using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Messaging.Messages;
using ServiceStack.Text;
namespace Messaging.Distributor
{
    class Program
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
        private static readonly ConcurrentQueue<Guid> AvailableWorkers = new ConcurrentQueue<Guid>();
        private static readonly ConcurrentQueue<Work> WorkQueue = new ConcurrentQueue<Work>();
        private static readonly ConcurrentDictionary<Guid, WorkAssignment> AssignedWork = new ConcurrentDictionary<Guid, WorkAssignment>();

        static void Main(string[] args)
        {
            CreateSubscriptions();
            Console.ReadLine();
        }

        private static void CreateSubscriptions()
        {
            SubscribeToDistribution();
            SubscribeToWorkerAvailable();
            SubscribeToWorkComplete();
        }

        private static void SubscribeToDistribution()
        {
            Action<string, string> onWorkReceived = (channel, msg) =>
            {
                var work = JsonSerializer.DeserializeFromString<Work>(msg);

                Guid workerId;
                if (AvailableWorkers.TryDequeue(out workerId))
                {
                    Work expiredWork;

                    if (TryPurgeWork(out expiredWork))
                    {
                        AssignWork(expiredWork, workerId);
                        WorkQueue.Enqueue(work);
                    }
                    else
                    {
                        AssignWork(work, workerId);
                    }
                }
                else
                {
                    WorkQueue.Enqueue(work);
                }
            };

            Task.Factory.StartNew(() => Subscribe(onWorkReceived, Channels.Distribution));
        }

        private static bool TryPurgeWork(out Work work)
        {
            var messageId =
                (from w in AssignedWork
                 where WorkTimeoutExpired(w.Value.AssignedAt)
                 select w.Key).FirstOrDefault();

            if (messageId == null)
            {
                work = null;
                return false;
            }

            WorkAssignment workAssignment;
            if (AssignedWork.TryRemove(messageId, out workAssignment))
            {
                work = workAssignment.Work;
                Console.WriteLine("[{0}] Purging and reassigning work: {1}",
                    DateTime.Now.ToShortTimeString(),
                    work);
                return true;
            }

            work = null;
            return false;
        }

        private static bool WorkTimeoutExpired(DateTime assignedAt)
        {
            return DateTime.Now.Subtract(assignedAt) > Timeout;
        }

        private static void AssignWork(Work work, Guid workerId)
        {
            var workAssignment = new WorkAssignment { Work = work, WorkerId = workerId, AssignedAt = DateTime.Now };
            AssignedWork.TryAdd(work.MessageId, workAssignment);
            
            using (var client = new LocalClient())
            {
                client.PublishMessage(workerId.ToString(), work.ToString());
            }
        }

        private static void SubscribeToWorkerAvailable()
        {
            Action<string, string> onWorkerAvailable = (channel, msg) =>
            {
                var workerAvailable = JsonSerializer.DeserializeFromString<WorkerAvailable>(msg);

                Work work;
                if (TryPurgeWork(out work) || WorkQueue.TryDequeue(out work))
                {
                    AssignWork(work, workerAvailable.WorkerId);
                }
                else
                {
                    AvailableWorkers.Enqueue(workerAvailable.WorkerId);
                }
            };

            Task.Factory.StartNew(() => Subscribe(onWorkerAvailable, Channels.WorkerAvailable));
        }

        private static void SubscribeToWorkComplete()
        {
            Action<string, string> onWorkComplete = (channel, msg) =>
            {
                var workComplete = JsonSerializer.DeserializeFromString<WorkComplete>(msg);
                WorkAssignment workAssignment;
                AssignedWork.TryRemove(workComplete.CorrelationId, out workAssignment);
            };

            Task.Factory.StartNew(() => Subscribe(onWorkComplete, Channels.WorkComplete));
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
