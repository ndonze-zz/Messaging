using System;
using Messaging.Messages;

namespace Messaging.Distributor
{
    public class WorkAssignment
    {
        public Work Work { get; set; }
        public Guid WorkerId { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
