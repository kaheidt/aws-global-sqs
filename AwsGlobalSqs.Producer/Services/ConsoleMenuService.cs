using System;
using System.Threading;
using System.Threading.Tasks;
using AwsGlobalSqs.Producer.Controllers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AwsGlobalSqs.Producer.Services
{
    public class ConsoleMenuService : BackgroundService
    {
        private readonly FailoverController _failoverController;
        private readonly ILogger<ConsoleMenuService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public ConsoleMenuService(
            FailoverController failoverController,
            ILogger<ConsoleMenuService> logger,
            IHostApplicationLifetime appLifetime)
        {
            _failoverController = failoverController ?? throw new ArgumentNullException(nameof(failoverController));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Console Menu Service is starting.");

            // Wait a moment for other services to initialize
            await Task.Delay(1000, stoppingToken);

            // Start a separate task for the menu
            _ = Task.Run(async () => await RunMenuAsync(stoppingToken), stoppingToken);
        }

        private async Task RunMenuAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("\n=== AWS Global SQS Demo ===");
                Console.WriteLine("1. Enable failover to secondary region (us-west-2)");
                Console.WriteLine("2. Disable failover (return to us-east-1)");
                Console.WriteLine("3. Exit");
                Console.Write("\nSelect an option: ");

                var key = Console.ReadKey();
                Console.WriteLine();

                try
                {
                    switch (key.KeyChar)
                    {
                        case '1':
                            await _failoverController.EnableFailoverAsync();
                            Console.WriteLine("Failover enabled. Messages will now be routed to us-west-2.");
                            break;
                        case '2':
                            await _failoverController.DisableFailoverAsync();
                            Console.WriteLine("Failover disabled. Messages will now be routed to us-east-1.");
                            break;
                        case '3':
                            _logger.LogInformation("Exiting application...");
                            _appLifetime.StopApplication();
                            return;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing menu option: {ex.Message}");
                    Console.WriteLine($"Error: {ex.Message}");
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}