using System.Threading.Tasks;
using AwsGlobalSqs.Common.Models;

namespace AwsGlobalSqs.Common.Services
{
    public interface ISqsService
    {
        Task<string> SendMessageAsync(SqsMessage message, string queueUrl);
        Task<SqsMessage[]> ReceiveMessagesAsync(string queueUrl, int maxMessages = 10);
        Task DeleteMessageAsync(string queueUrl, string receiptHandle);
    }
}