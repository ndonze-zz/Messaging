using ServiceStack.Redis;

namespace Messaging
{
    public class LocalClient : RedisClient
    {
        public LocalClient()
            : base("localhost")
        {
        }
    }
}
