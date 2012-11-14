using ServiceStack.Text;

namespace Messaging.Messages
{
    public class Work : MessageBase
    {
        public string Message { get; set; }

        public override string ToString()
        {
            return JsonSerializer.SerializeToString(this);
        }
    }
}
