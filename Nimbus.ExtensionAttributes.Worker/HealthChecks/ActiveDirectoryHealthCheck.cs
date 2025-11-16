using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Nimbus.ExtensionAttributes.AD;

namespace Nimbus.ExtensionAttributes.WorkerSvc.HealthChecks
{
    public class ActiveDirectoryHealthCheck : IHealthCheck
    {
        private readonly ILogger<ActiveDirectoryHealthCheck> _logger;
        private readonly IADHelper _adHelper;

        public ActiveDirectoryHealthCheck(ILogger<ActiveDirectoryHealthCheck> logger, IADHelper adHelper)
        {
            _logger = logger;
            _adHelper = adHelper;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Performing Active Directory health check");

                // Try to get a directory entry to test AD connectivity
                var testEntry = await _adHelper.GetDirectoryEntryAsync("LDAP://RootDSE");
                
                if (testEntry != null)
                {
                    var data = new Dictionary<string, object>
                    {
                        ["status"] = "Connected",
                        ["timestamp"] = DateTime.UtcNow,
                        ["server"] = testEntry.Path
                    };

                    _logger.LogDebug("Active Directory health check passed");
                    return HealthCheckResult.Healthy("Active Directory is accessible", data);
                }
                else
                {
                    _logger.LogWarning("Active Directory health check failed - unable to connect");
                    return HealthCheckResult.Unhealthy("Unable to connect to Active Directory");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Active Directory health check failed: {Error}", ex.Message);
                
                var data = new Dictionary<string, object>
                {
                    ["status"] = "Error",
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                };

                return HealthCheckResult.Unhealthy("Active Directory health check failed", ex, data);
            }
        }
    }
}