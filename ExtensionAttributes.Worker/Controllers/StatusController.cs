using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using ExtensionAttributes.Automation.WorkerSvc.JobUtils;
using ExtensionAttributes.Automation.WorkerSvc.Config;
using ExtensionAttributes.Automation.WorkerSvc.Services;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ExtensionAttributes.Automation.WorkerSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly UnifiedExtensionAttributeHelper _unifiedHelper;
        private readonly AppSettings _appSettings;
        private readonly ILogger<StatusController> _logger;
        private readonly IAuditLogger _auditLogger;

        public StatusController(
            HealthCheckService healthCheckService,
            UnifiedExtensionAttributeHelper unifiedHelper,
            IOptions<AppSettings> appSettings,
            ILogger<StatusController> logger,
            IAuditLogger auditLogger)
        {
            _healthCheckService = healthCheckService;
            _unifiedHelper = unifiedHelper;
            _appSettings = appSettings.Value;
            _logger = logger;
            _auditLogger = auditLogger;
        }

        /// <summary>
        /// Get overall system health status
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealthStatus()
        {
            try
            {
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    "API request: GET /api/status/health",
                    AuditSeverity.Low);

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
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    $"API request failed: GET /api/status/health - {ex.Message}",
                    AuditSeverity.High);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get system information and statistics
        /// </summary>
        [HttpGet("info")]
        public async Task<IActionResult> GetSystemInfo()
        {
            try
            {
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    "API request: GET /api/status/info",
                    AuditSeverity.Low);

                var response = new
                {
                    application = new
                    {
                        name = "Extension Attributes Automation Worker",
                        version = "1.3.0",
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
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    $"API request failed: GET /api/status/info - {ex.Message}",
                    AuditSeverity.Medium);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get configuration mappings
        /// </summary>
        [HttpGet("mappings")]
        public async Task<IActionResult> GetMappings()
        {
            try
            {
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    "API request: GET /api/status/mappings",
                    AuditSeverity.Low);

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
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    $"API request failed: GET /api/status/mappings - {ex.Message}",
                    AuditSeverity.Medium);
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
                    await _auditLogger.LogAsync(
                        AuditEventType.ApiRequest,
                        "API request failed: POST /api/status/process-device - Device name is required",
                        AuditSeverity.Medium);
                    return BadRequest(new { error = "Device name is required" });
                }

                await _auditLogger.LogUserActionAsync(
                    $"API request to process device: {deviceName}",
                    deviceName,
                    new Dictionary<string, object>
                    {
                        ["RequestMethod"] = "POST",
                        ["Endpoint"] = "/api/status/process-device",
                        ["UserAgent"] = Request.Headers.UserAgent.ToString(),
                        ["RemoteIP"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                    });

                _logger.LogInformation("API request to process device: {DeviceName}", deviceName);
                
                var result = await _unifiedHelper.ProcessSingleDeviceAsync(deviceName);
                
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    $"API device processing completed: {deviceName} - Success: {result}",
                    result ? AuditSeverity.Low : AuditSeverity.Medium);
                
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
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    $"API request failed: POST /api/status/process-device/{deviceName} - {ex.Message}",
                    AuditSeverity.High);
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
                    await _auditLogger.LogAsync(
                        AuditEventType.ApiRequest,
                        "API request failed: POST /api/status/process-device-by-id - Device ID is required",
                        AuditSeverity.Medium);
                    return BadRequest(new { error = "Device ID is required" });
                }

                await _auditLogger.LogUserActionAsync(
                    $"API request to process device by ID: {deviceId}",
                    additionalData: new Dictionary<string, object>
                    {
                        ["RequestMethod"] = "POST",
                        ["Endpoint"] = "/api/status/process-device-by-id",
                        ["DeviceId"] = deviceId,
                        ["UserAgent"] = Request.Headers.UserAgent.ToString(),
                        ["RemoteIP"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                    });

                _logger.LogInformation("API request to process device by ID: {DeviceId}", deviceId);
                
                var result = await _unifiedHelper.ProcessSingleDeviceByIdAsync(deviceId);
                
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    $"API device processing by ID completed: {deviceId} - Success: {result}",
                    result ? AuditSeverity.Low : AuditSeverity.Medium);
                
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
                await _auditLogger.LogAsync(
                    AuditEventType.ApiRequest,
                    $"API request failed: POST /api/status/process-device-by-id/{deviceId} - {ex.Message}",
                    AuditSeverity.High);
                return StatusCode(500, new { error = ex.Message, deviceId });
            }
        }
    }
}