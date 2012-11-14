using System;
using ServiceStack.Text;

namespace Messaging.Messages
{
    public class WorkerAvailable : MessageBase
    {
        public Guid WorkerId { get; set; }

        public override string ToString()
        {
            return JsonSerializer.SerializeToString(this);
        }
    }
}
