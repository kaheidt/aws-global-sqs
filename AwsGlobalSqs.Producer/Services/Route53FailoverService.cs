using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Route53;
using Amazon.Route53.Model;
using AwsGlobalSqs.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AwsGlobalSqs.Producer.Services
{
    public class Route53FailoverService : IHostedService
    {
        private readonly IAmazonRoute53 _route53Client;
        private readonly ILogger<Route53FailoverService> _logger;
        private readonly string _hostedZoneId;
        private readonly string _dnsName;

        public Route53FailoverService(
            IAmazonRoute53 route53Client,
            ILogger<Route53FailoverService> logger)
        {
            _route53Client = route53Client ?? throw new ArgumentNullException(nameof(route53Client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hostedZoneId = Constants.Route53HostedZoneId;
            _dnsName = Constants.Route53DnsName;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Route53 Failover Service started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Route53 Failover Service stopped");
            return Task.CompletedTask;
        }

        public async Task ToggleFailoverAsync(bool enableFailover)
        {
            try
            {
                // Get the current record sets
                var recordSets = await GetRecordSetsAsync();
                
                // Find the primary and secondary record sets
                var primaryRecord = recordSets.Find(r => r.SetIdentifier == "Primary");
                var secondaryRecord = recordSets.Find(r => r.SetIdentifier == "Secondary");
                
                if (primaryRecord == null || secondaryRecord == null)
                {
                    _logger.LogError("Could not find primary or secondary record sets");
                    return;
                }
                
                // Update the weights based on the failover flag
                int primaryWeight = enableFailover ? 10 : 90;
                int secondaryWeight = enableFailover ? 90 : 10;
                
                // Create change batch
                var changeBatch = new ChangeBatch
                {
                    Changes = new System.Collections.Generic.List<Change>
                    {
                        new Change
                        {
                            Action = ChangeAction.UPSERT,
                            ResourceRecordSet = new ResourceRecordSet
                            {
                                Name = primaryRecord.Name,
                                Type = primaryRecord.Type,
                                TTL = primaryRecord.TTL,
                                ResourceRecords = primaryRecord.ResourceRecords,
                                SetIdentifier = primaryRecord.SetIdentifier,
                                Weight = primaryWeight,
                                HealthCheckId = primaryRecord.HealthCheckId
                            }
                        },
                        new Change
                        {
                            Action = ChangeAction.UPSERT,
                            ResourceRecordSet = new ResourceRecordSet
                            {
                                Name = secondaryRecord.Name,
                                Type = secondaryRecord.Type,
                                TTL = secondaryRecord.TTL,
                                ResourceRecords = secondaryRecord.ResourceRecords,
                                SetIdentifier = secondaryRecord.SetIdentifier,
                                Weight = secondaryWeight,
                                HealthCheckId = secondaryRecord.HealthCheckId
                            }
                        }
                    }
                };
                
                // Submit the change batch
                var request = new ChangeResourceRecordSetsRequest
                {
                    HostedZoneId = _hostedZoneId,
                    ChangeBatch = changeBatch
                };
                
                var response = await _route53Client.ChangeResourceRecordSetsAsync(request);
                
                _logger.LogInformation($"Failover {(enableFailover ? "enabled" : "disabled")}. Change ID: {response.ChangeInfo.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling failover: {ex.Message}");
                throw;
            }
        }

        private async Task<System.Collections.Generic.List<ResourceRecordSet>> GetRecordSetsAsync()
        {
            try
            {
                var request = new ListResourceRecordSetsRequest
                {
                    HostedZoneId = _hostedZoneId,
                    StartRecordName = _dnsName,
                    StartRecordType = RRType.CNAME
                };
                
                var response = await _route53Client.ListResourceRecordSetsAsync(request);
                
                return response.ResourceRecordSets.FindAll(r => r.Name == _dnsName + "." && r.Type == RRType.CNAME);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting record sets: {ex.Message}");
                throw;
            }
        }
    }
}