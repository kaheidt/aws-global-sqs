using System;
using System.Threading;
using System.Threading.Tasks;
using AwsGlobalSqs.Common;
using AwsGlobalSqs.Common.Models;
using AwsGlobalSqs.Common.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AwsGlobalSqs.Producer.Services
{
    public class MessageProducerService : BackgroundService
    {
        private readonly ISqsService _sqsService;
        private readonly ILogger<MessageProducerService> _logger;
        private readonly string _queueUrl;

        public MessageProducerService(
            ISqsService sqsService,
            ILogger<MessageProducerService> logger)
        {
            _sqsService = sqsService ?? throw new ArgumentNullException(nameof(sqsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Using the global SQS endpoint via Route53
            _queueUrl = $"{Constants.GlobalSqsEndpoint}/{Constants.QueueName}";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message Producer Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create a new message
                    var message = new SqsMessage
                    {
                        Content = $"Test message at {DateTime.UtcNow}",
                        OriginRegion = "unknown", // The producer doesn't know which region it's sending to
                        DestinationRegion = "unknown" // This will be determined by Route53
                    };

                    // Send the message to the global SQS endpoint
                    await _sqsService.SendMessageAsync(message, _queueUrl);
                    _logger.LogInformation($"Sent message: {message}");

                    // Wait for a while before sending the next message
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Message Producer Service");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Message Producer Service is stopping.");
        }
    }
}