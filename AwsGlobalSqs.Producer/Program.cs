using System;
using System.Net.Http;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Route53;
using Amazon.Runtime;
using Amazon.SQS;
using AwsGlobalSqs.Common.Services;
using AwsGlobalSqs.Producer.Controllers;
using AwsGlobalSqs.Producer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AwsGlobalSqs.Producer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Configure AWS SQS client with SigV4a signing
                    services.AddAWSService<IAmazonSQS>(new AWSOptions
                    {
                        // Use SigV4a for multi-region support
                        SignatureVersion = "4a",
                        // No specific region is set here since we're using SigV4a for multi-region
                        // The actual endpoint will be determined by Route53
                    });

                    // Configure AWS Route53 client
                    services.AddAWSService<IAmazonRoute53>();

                    // Register services
                    services.AddSingleton<ISqsService, SqsProducerService>();
                    services.AddSingleton<Route53FailoverService>();
                    services.AddSingleton<FailoverController>();
                    
                    // Register hosted services
                    services.AddHostedService<MessageProducerService>();
                    services.AddHostedService<ConsoleMenuService>();

                    // Add HTTP client for health checks
                    services.AddHttpClient();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                });
    }
}