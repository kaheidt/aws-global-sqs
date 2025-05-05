using System;

namespace AwsGlobalSqs.Common.Models
{
    public class SqsMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string OriginRegion { get; set; }
        public string DestinationRegion { get; set; }
        public string ReceiptHandle { get; set; }
        
        public override string ToString()
        {
            return $"Message ID: {Id}, Content: {Content}, Timestamp: {Timestamp}, Origin: {OriginRegion}, Destination: {DestinationRegion}";
        }
    }
}