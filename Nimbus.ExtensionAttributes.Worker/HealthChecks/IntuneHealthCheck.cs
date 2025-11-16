using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Nimbus.ExtensionAttributes.Intune;

namespace Nimbus.ExtensionAttributes.WorkerSvc.HealthChecks
{
    public class IntuneHealthCheck : IHealthCheck
    {
        private readonly ILogger<IntuneHealthCheck> _logger;
        private readonly IIntuneHelper _intuneHelper;

        public IntuneHealthCheck(ILogger<IntuneHealthCheck> logger, IIntuneHelper intuneHelper)
        {
            _logger = logger;
            _intuneHelper = intuneHelper;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Performing Intune health check");

                // Try to get Intune devices to test connectivity
                var devices = await _intuneHelper.GetIntuneDevices();
                
                var deviceCount = devices?.Value?.Count ?? 0;
                
                var data = new Dictionary<string, object>
                {
                    ["status"] = "Connected",
                    ["timestamp"] = DateTime.UtcNow,
                    ["deviceCount"] = deviceCount
                };

                _logger.LogDebug("Intune health check passed - retrieved {DeviceCount} devices", deviceCount);
                return HealthCheckResult.Healthy("Intune Graph API is accessible", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Intune health check failed: {Error}", ex.Message);
                
                var data = new Dictionary<string, object>
                {
                    ["status"] = "Error",
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                };

                return HealthCheckResult.Unhealthy("Intune Graph API health check failed", ex, data);
            }
        }
    }
}