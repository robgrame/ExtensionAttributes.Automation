using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils;
using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly UnifiedExtensionAttributeHelper _unifiedHelper;
        private readonly AppSettings _appSettings;
        private readonly ILogger<StatusController> _logger;

        public StatusController(
            HealthCheckService healthCheckService,
            UnifiedExtensionAttributeHelper unifiedHelper,
            IOptions<AppSettings> appSettings,
            ILogger<StatusController> logger)
        {
            _healthCheckService = healthCheckService;
            _unifiedHelper = unifiedHelper;
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Get overall system health status
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealthStatus()
        {
            try
            {
                var healthReport = await _healthCheckService.CheckHealthAsync();
                
                var response = new
                {
                    status = healthReport.Status.ToString(),
                    totalDuration = healthReport.TotalDuration.TotalMilliseconds,
                    timestamp = DateTime.UtcNow,
                    checks = healthReport.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        duration = e.Value.Duration.TotalMilliseconds,
                        description = e.Value.Description,
                        data = e.Value.Data,
                        exception = e.Value.Exception?.Message
                    })
                };

                var statusCode = healthReport.Status switch
                {
                    HealthStatus.Healthy => 200,
                    HealthStatus.Degraded => 200,
                    HealthStatus.Unhealthy => 503,
                    _ => 500
                };

                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health status: {Error}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get system information and statistics
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetSystemInfo()
        {
            try
            {
                var response = new
                {
                    application = new
                    {
                        name = "RGP Extension Attributes Automation Worker",
                        version = "1.2.0",
                        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                        machineName = Environment.MachineName,
                        osVersion = Environment.OSVersion.ToString(),
                        framework = Environment.Version.ToString(),
                        startTime = Process.GetCurrentProcess().StartTime,
                        uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime
                    },
                    configuration = new
                    {
                        adEnabled = _appSettings.DataSources.EnableActiveDirectory,
                        intuneEnabled = _appSettings.DataSources.EnableIntune,
                        preferredDataSource = _appSettings.DataSources.PreferredDataSource,
                        mappingsCount = _appSettings.ExtensionAttributeMappings?.Count ?? 0,
                        exportPath = _appSettings.ExportPath
                    },
                    memory = new
                    {
                        workingSet = GC.GetTotalMemory(false),
                        gen0Collections = GC.CollectionCount(0),
                        gen1Collections = GC.CollectionCount(1),
                        gen2Collections = GC.CollectionCount(2)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system info: {Error}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get configuration mappings
        /// </summary>
        [HttpGet("mappings")]
        public IActionResult GetMappings()
        {
            try
            {
                object mappings;
                int count = 0;

                if (_appSettings.ExtensionAttributeMappings != null && _appSettings.ExtensionAttributeMappings.Any())
                {
                    mappings = _appSettings.ExtensionAttributeMappings
                        .Select(m => new
                        {
                            extensionAttribute = m.ExtensionAttribute,
                            sourceAttribute = m.SourceAttribute,
                            dataSource = m.DataSource.ToString(),
                            regex = m.Regex,
                            defaultValue = m.DefaultValue,
                            useHardwareInfo = m.UseHardwareInfo
                        })
                        .ToList();
                    count = _appSettings.ExtensionAttributeMappings.Count;
                }
                else
                {
                    mappings = new List<object>();
                }

                return Ok(new
                {
                    count,
                    mappings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mappings: {Error}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Process a single device by name
        /// </summary>
        [HttpPost("process-device/{deviceName}")]
        public async Task<IActionResult> ProcessDevice(string deviceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    return BadRequest(new { error = "Device name is required" });
                }

                _logger.LogInformation("API request to process device: {DeviceName}", deviceName);
                
                var result = await _unifiedHelper.ProcessSingleDeviceAsync(deviceName);
                
                return Ok(new
                {
                    deviceName,
                    processed = result,
                    timestamp = DateTime.UtcNow,
                    message = result ? "Device processed successfully" : "Device processing failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing device {DeviceName}: {Error}", deviceName, ex.Message);
                return StatusCode(500, new { error = ex.Message, deviceName });
            }
        }

        /// <summary>
        /// Process a single device by Entra AD Device ID
        /// </summary>
        [HttpPost("process-device-by-id/{deviceId}")]
        public async Task<IActionResult> ProcessDeviceById(string deviceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return BadRequest(new { error = "Device ID is required" });
                }

                _logger.LogInformation("API request to process device by ID: {DeviceId}", deviceId);
                
                var result = await _unifiedHelper.ProcessSingleDeviceByIdAsync(deviceId);
                
                return Ok(new
                {
                    deviceId,
                    processed = result,
                    timestamp = DateTime.UtcNow,
                    message = result ? "Device processed successfully" : "Device processing failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing device by ID {DeviceId}: {Error}", deviceId, ex.Message);
                return StatusCode(500, new { error = ex.Message, deviceId });
            }
        }
    }
}