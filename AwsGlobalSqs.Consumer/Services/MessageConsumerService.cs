using System;
using System.Threading;
using System.Threading.Tasks;
using AwsGlobalSqs.Common;
using AwsGlobalSqs.Common.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AwsGlobalSqs.Consumer.Services
{
    public class MessageConsumerService : BackgroundService
    {
        private readonly ISqsService _sqsService;
        private readonly ILogger<MessageConsumerService> _logger;
        private readonly string _usEast1QueueUrl;
        private readonly string _usWest2QueueUrl;

        public MessageConsumerService(
            ISqsService sqsService,
            ILogger<MessageConsumerService> logger)
        {
            _sqsService = sqsService ?? throw new ArgumentNullException(nameof(sqsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Direct URLs to regional queues for monitoring
            _usEast1QueueUrl = $"https://sqs.{Constants.UsEast1}.amazonaws.com/{Constants.QueueName}";
            _usWest2QueueUrl = $"https://sqs.{Constants.UsWest2}.amazonaws.com/{Constants.QueueName}";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message Consumer Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check US East 1 queue
                    await CheckQueueForMessages(_usEast1QueueUrl, Constants.UsEast1, stoppingToken);
                    
                    // Check US West 2 queue
                    await CheckQueueForMessages(_usWest2QueueUrl, Constants.UsWest2, stoppingToken);
                    
                    // Wait before checking again
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Message Consumer Service");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Message Consumer Service is stopping.");
        }

        private async Task CheckQueueForMessages(string queueUrl, string region, CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"Checking for messages in {region}...");
                
                var messages = await _sqsService.ReceiveMessagesAsync(queueUrl);
                
                foreach (var message in messages)
                {
                    // Set console color based on region
                    ConsoleColor originalColor = Console.ForegroundColor;
                    if (region.Contains("east", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                    }

                    // Log the message with color
                    Console.WriteLine($"Received message in {region}: {message}");
                    
                    // Reset console color
                    Console.ForegroundColor = originalColor;
                    
                    // Update the destination region since we now know where it was delivered
                    message.DestinationRegion = region;
                    
                    // Process the message (in a real app, you'd do something with it)
                    
                    // Delete the message from the queue
                    if (!string.IsNullOrEmpty(message.ReceiptHandle))
                    {
                        await _sqsService.DeleteMessageAsync(queueUrl, message.ReceiptHandle);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking queue in {region}: {ex.Message}");
            }
        }
    }
}