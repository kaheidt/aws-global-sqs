using System;

namespace AwsGlobalSqs.Common
{
    public static class Constants
    {
        // AWS Regions
        public static readonly string UsEast1 = Environment.GetEnvironmentVariable("AWS_REGION_PRIMARY") ?? "us-east-1";
        public static readonly string UsWest2 = Environment.GetEnvironmentVariable("AWS_REGION_SECONDARY") ?? "us-west-2";
        
        // SQS Queue Names
        public static readonly string QueueName = Environment.GetEnvironmentVariable("SQS_QUEUE_NAME") ?? "global-sqs-demo-queue";
        
        // Route53 Settings
        public static readonly string Route53HostedZoneId = Environment.GetEnvironmentVariable("ROUTE53_HOSTED_ZONE_ID") ?? "YOUR_HOSTED_ZONE_ID"; 
        public static readonly string Route53DnsName = Environment.GetEnvironmentVariable("ROUTE53_DNS_NAME") ?? "sqs-global.example.com";
        
        // Global SQS Endpoint
        public static readonly string GlobalSqsEndpoint = Environment.GetEnvironmentVariable("GLOBAL_SQS_ENDPOINT") ?? $"https://{Route53DnsName}";
    }
}