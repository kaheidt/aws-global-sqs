using System;
using System.Threading.Tasks;
using AwsGlobalSqs.Producer.Services;
using Microsoft.Extensions.Logging;

namespace AwsGlobalSqs.Producer.Controllers
{
    public class FailoverController
    {
        private readonly Route53FailoverService _failoverService;
        private readonly ILogger<FailoverController> _logger;

        public FailoverController(
            Route53FailoverService failoverService,
            ILogger<FailoverController> logger)
        {
            _failoverService = failoverService ?? throw new ArgumentNullException(nameof(failoverService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task EnableFailoverAsync()
        {
            _logger.LogInformation("Enabling failover to secondary region");
            await _failoverService.ToggleFailoverAsync(true);
        }

        public async Task DisableFailoverAsync()
        {
            _logger.LogInformation("Disabling failover, returning to primary region");
            await _failoverService.ToggleFailoverAsync(false);
        }
    }
}