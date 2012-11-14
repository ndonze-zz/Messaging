using System;
using ServiceStack.Text;

namespace Messaging.Messages
{
    public class WorkComplete : MessageBase
    {
        public Guid CorrelationId { get; set; }

        public override string ToString()
        {
            return JsonSerializer.SerializeToString(this);
        }
    }
}
