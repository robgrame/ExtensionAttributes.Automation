using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExtensionAttributes.Automation.WorkerSvc.Config;

namespace ExtensionAttributes.Automation.WorkerSvc.HealthChecks
{
    public class ConfigurationHealthCheck : IHealthCheck
    {
        private readonly ILogger<ConfigurationHealthCheck> _logger;
        private readonly AppSettings _appSettings;

        public ConfigurationHealthCheck(ILogger<ConfigurationHealthCheck> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Performing configuration health check");

                var issues = new List<string>();
                var warnings = new List<string>();

                // Check basic configuration
                if (string.IsNullOrWhiteSpace(_appSettings.ExportPath))
                {
                    issues.Add("ExportPath is not configured");
                }

                if (_appSettings.ExtensionAttributeMappings == null || !_appSettings.ExtensionAttributeMappings.Any())
                {
                    issues.Add("No extension attribute mappings configured");
                }

                // Check data source configuration
                if (!_appSettings.DataSources.EnableActiveDirectory && !_appSettings.DataSources.EnableIntune)
                {
                    issues.Add("No data sources are enabled");
                }

                // Check for conflicting mappings (same extension attribute used multiple times)
                if (_appSettings.ExtensionAttributeMappings != null)
                {
                    var duplicateAttributes = _appSettings.ExtensionAttributeMappings
                        .GroupBy(m => m.ExtensionAttribute)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateAttributes.Any())
                    {
                        issues.Add($"Duplicate extension attributes found: {string.Join(", ", duplicateAttributes)}");
                    }

                    // Check for mappings with disabled data sources
                    var adMappingsWithDisabledSource = _appSettings.ExtensionAttributeMappings
                        .Where(m => m.DataSource == DataSourceType.ActiveDirectory && !_appSettings.DataSources.EnableActiveDirectory)
                        .Count();

                    var intuneMappingsWithDisabledSource = _appSettings.ExtensionAttributeMappings
                        .Where(m => m.DataSource == DataSourceType.Intune && !_appSettings.DataSources.EnableIntune)
                        .Count();

                    if (adMappingsWithDisabledSource > 0)
                    {
                        warnings.Add($"{adMappingsWithDisabledSource} AD mappings configured but AD is disabled");
                    }

                    if (intuneMappingsWithDisabledSource > 0)
                    {
                        warnings.Add($"{intuneMappingsWithDisabledSource} Intune mappings configured but Intune is disabled");
                    }
                }

                var data = new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.UtcNow,
                    ["adEnabled"] = _appSettings.DataSources.EnableActiveDirectory,
                    ["intuneEnabled"] = _appSettings.DataSources.EnableIntune,
                    ["mappingCount"] = _appSettings.ExtensionAttributeMappings?.Count ?? 0,
                    ["exportPath"] = _appSettings.ExportPath ?? "Not configured"
                };

                if (warnings.Any())
                {
                    data["warnings"] = warnings;
                }

                if (issues.Any())
                {
                    data["issues"] = issues;
                    _logger.LogWarning("Configuration health check failed with issues: {Issues}", string.Join(", ", issues));
                    return Task.FromResult(HealthCheckResult.Unhealthy("Configuration has critical issues", null, data));
                }

                if (warnings.Any())
                {
                    _logger.LogWarning("Configuration health check passed with warnings: {Warnings}", string.Join(", ", warnings));
                    return Task.FromResult(HealthCheckResult.Degraded("Configuration has warnings", null, data));
                }

                _logger.LogDebug("Configuration health check passed");
                return Task.FromResult(HealthCheckResult.Healthy("Configuration is valid", data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration health check failed: {Error}", ex.Message);
                
                var data = new Dictionary<string, object>
                {
                    ["status"] = "Error",
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                };

                return Task.FromResult(HealthCheckResult.Unhealthy("Configuration health check failed", ex, data));
            }
        }
    }
}