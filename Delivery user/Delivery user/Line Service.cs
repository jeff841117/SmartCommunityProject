using Line.Messaging;
using System.Threading.Tasks;

namespace WebApplication6
{
    public class LineService
    {
        private readonly string _channelAccessToken = "YOUR_CHANNEL_ACCESS_TOKEN";

        public async Task PushMessageAsync(string userId, string message)
        {
            var client = new LineMessagingClient(_channelAccessToken);
            await client.PushMessageAsync(userId, message);
        }
    }
}
