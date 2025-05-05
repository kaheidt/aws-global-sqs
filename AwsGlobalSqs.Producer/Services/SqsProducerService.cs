using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using AwsGlobalSqs.Common.Models;
using AwsGlobalSqs.Common.Services;
using Microsoft.Extensions.Logging;

namespace AwsGlobalSqs.Producer.Services
{
    public class SqsProducerService : ISqsService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<SqsProducerService> _logger;

        public SqsProducerService(IAmazonSQS sqsClient, ILogger<SqsProducerService> logger)
        {
            _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SendMessageAsync(SqsMessage message, string queueUrl)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(message);
                
                var request = new SendMessageRequest
                {
                    QueueUrl = queueUrl,
                    MessageBody = messageBody,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        {
                            "MessageId", new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = message.Id
                            }
                        },
                        {
                            "Timestamp", new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = message.Timestamp.ToString("o")
                            }
                        },
                        {
                            "OriginRegion", new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = message.OriginRegion
                            }
                        },
                        {
                            "DestinationRegion", new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = message.DestinationRegion
                            }
                        }
                    }
                };

                var response = await _sqsClient.SendMessageAsync(request);
                _logger.LogInformation($"Message sent to SQS. MessageId: {response.MessageId}, Queue URL: {queueUrl.Substring(0,25)}...");
                
                return response.MessageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to SQS: {ex.Message}");
                throw;
            }
        }

        public async Task<SqsMessage[]> ReceiveMessagesAsync(string queueUrl, int maxMessages = 10)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = maxMessages,
                    WaitTimeSeconds = 5,
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(request);
                
                var messages = new List<SqsMessage>();
                foreach (var message in response.Messages)
                {
                    try
                    {
                        var sqsMessage = JsonSerializer.Deserialize<SqsMessage>(message.Body);
                        if (sqsMessage != null)
                        {
                            messages.Add(sqsMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error deserializing message: {ex.Message}");
                    }
                }

                return messages.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error receiving messages from SQS: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteMessageAsync(string queueUrl, string receiptHandle)
        {
            try
            {
                var request = new DeleteMessageRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = receiptHandle
                };

                await _sqsClient.DeleteMessageAsync(request);
                _logger.LogDebug($"Message deleted from SQS. ReceiptHandle: {receiptHandle}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting message from SQS: {ex.Message}");
                throw;
            }
        }
    }
}