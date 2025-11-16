using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Nimbus.ExtensionAttributes.EntraAD;

namespace Nimbus.ExtensionAttributes.WorkerSvc.HealthChecks
{
    public class EntraADHealthCheck : IHealthCheck
    {
        private readonly ILogger<EntraADHealthCheck> _logger;
        private readonly IEntraADHelper _entraADHelper;

        public EntraADHealthCheck(ILogger<EntraADHealthCheck> logger, IEntraADHelper entraADHelper)
        {
            _logger = logger;
            _entraADHelper = entraADHelper;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Performing Entra AD health check");

                // Try to get a small number of devices to test connectivity
                var devices = await _entraADHelper.GetDevices(1); // Get just 1 device
                
                var data = new Dictionary<string, object>
                {
                    ["status"] = "Connected",
                    ["timestamp"] = DateTime.UtcNow,
                    ["deviceCount"] = devices?.Count() ?? 0
                };

                _logger.LogDebug("Entra AD health check passed - retrieved {DeviceCount} devices", devices?.Count() ?? 0);
                return HealthCheckResult.Healthy("Entra AD Graph API is accessible", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entra AD health check failed: {Error}", ex.Message);
                
                var data = new Dictionary<string, object>
                {
                    ["status"] = "Error",
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                };

                return HealthCheckResult.Unhealthy("Entra AD Graph API health check failed", ex, data);
            }
        }
    }
}