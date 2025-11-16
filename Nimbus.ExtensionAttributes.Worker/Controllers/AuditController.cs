using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nimbus.ExtensionAttributes.WorkerSvc.Services;

namespace Nimbus.ExtensionAttributes.WorkerSvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditLogger auditLogger, ILogger<AuditController> logger)
        {
            _auditLogger = auditLogger;
            _logger = logger;
        }

        /// <summary>
        /// Get audit logs with optional filtering
        /// </summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] AuditEventType? eventType = null,
            [FromQuery] string? deviceName = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                await _auditLogger.LogUserActionAsync(
                    $"Accessed audit logs API - Page: {page}, EventType: {eventType}, Device: {deviceName}",
                    deviceName,
                    new Dictionary<string, object>
                    {
                        ["Page"] = page,
                        ["PageSize"] = pageSize,
                        ["From"] = from?.ToString() ?? "null",
                        ["To"] = to?.ToString() ?? "null",
                        ["EventType"] = eventType?.ToString() ?? "null"
                    });

                var logs = await _auditLogger.GetAuditLogsAsync(from, to, eventType, deviceName);
                
                // Apply pagination
                var totalCount = logs.Count();
                var pagedLogs = logs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var response = new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    logs = pagedLogs.Select(log => new
                    {
                        log.EventId,
                        log.Timestamp,
                        log.EventType,
                        log.Severity,
                        log.DeviceName,
                        log.ExtensionAttribute,
                        log.Description,
                        log.Success,
                        log.Duration,
                        log.User,
                        log.Source,
                        log.ErrorMessage
                    })
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get audit log summary and statistics
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetAuditSummary(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                await _auditLogger.LogUserActionAsync(
                    $"Accessed audit summary API - From: {from}, To: {to}",
                    additionalData: new Dictionary<string, object>
                    {
                        ["From"] = from?.ToString() ?? "null",
                        ["To"] = to?.ToString() ?? "null"
                    });

                var summary = await _auditLogger.GetAuditSummaryAsync(from, to);
                
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit summary");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get available audit event types
        /// </summary>
        [HttpGet("event-types")]
        public IActionResult GetEventTypes()
        {
            try
            {
                var eventTypes = Enum.GetValues<AuditEventType>()
                    .Select(et => new
                    {
                        value = et.ToString(),
                        displayName = et.ToString().Replace("_", " "),
                        description = GetEventTypeDescription(et)
                    })
                    .ToList();

                return Ok(eventTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event types");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Export audit logs to CSV format
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportAuditLogs(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] AuditEventType? eventType = null,
            [FromQuery] string? deviceName = null)
        {
            try
            {
                await _auditLogger.LogUserActionAsync(
                    $"Exported audit logs - EventType: {eventType}, Device: {deviceName}",
                    deviceName,
                    new Dictionary<string, object>
                    {
                        ["ExportFormat"] = "CSV",
                        ["From"] = from?.ToString() ?? "null",
                        ["To"] = to?.ToString() ?? "null",
                        ["EventType"] = eventType?.ToString() ?? "null"
                    });

                var logs = await _auditLogger.GetAuditLogsAsync(from, to, eventType, deviceName);

                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream);
                
                // Write CSV header
                await writer.WriteLineAsync("EventId,Timestamp,EventType,Severity,DeviceName,ExtensionAttribute,Description,Success,Duration,User,Source,ErrorMessage");
                
                // Write data rows
                foreach (var log in logs)
                {
                    var line = string.Join(",", 
                        EscapeCsvValue(log.EventId),
                        EscapeCsvValue(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC")),
                        EscapeCsvValue(log.EventType.ToString()),
                        EscapeCsvValue(log.Severity.ToString()),
                        EscapeCsvValue(log.DeviceName),
                        EscapeCsvValue(log.ExtensionAttribute),
                        EscapeCsvValue(log.Description),
                        log.Success,
                        log.Duration?.TotalMilliseconds ?? 0,
                        EscapeCsvValue(log.User),
                        EscapeCsvValue(log.Source),
                        EscapeCsvValue(log.ErrorMessage ?? ""));
                    
                    await writer.WriteLineAsync(line);
                }

                await writer.FlushAsync();
                memoryStream.Position = 0;

                var fileName = $"audit-logs-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv";
                return File(memoryStream.ToArray(), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get audit logs for a specific device
        /// </summary>
        [HttpGet("device/{deviceName}")]
        public async Task<IActionResult> GetDeviceAuditLogs(
            string deviceName,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            try
            {
                await _auditLogger.LogUserActionAsync(
                    $"Accessed device audit logs for: {deviceName}",
                    deviceName,
                    new Dictionary<string, object>
                    {
                        ["Page"] = page,
                        ["PageSize"] = pageSize
                    });

                var logs = await _auditLogger.GetAuditLogsAsync(from, to, null, deviceName);
                
                // Apply pagination
                var totalCount = logs.Count();
                var pagedLogs = logs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var response = new
                {
                    deviceName,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    logs = pagedLogs.Select(log => new
                    {
                        log.EventId,
                        log.Timestamp,
                        log.EventType,
                        log.ExtensionAttribute,
                        log.OldValue,
                        log.NewValue,
                        log.Success,
                        log.Duration,
                        log.Description,
                        log.ErrorMessage
                    })
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device audit logs for {DeviceName}", deviceName);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string GetEventTypeDescription(AuditEventType eventType)
        {
            return eventType switch
            {
                AuditEventType.SystemStartup => "System or service startup event",
                AuditEventType.SystemShutdown => "System or service shutdown event",
                AuditEventType.DeviceProcessingStarted => "Device processing operation started",
                AuditEventType.DeviceProcessingCompleted => "Device processing operation completed successfully",
                AuditEventType.DeviceProcessingFailed => "Device processing operation failed",
                AuditEventType.ExtensionAttributeUpdated => "Extension attribute successfully updated",
                AuditEventType.ExtensionAttributeUpdateFailed => "Extension attribute update failed",
                AuditEventType.HealthCheckExecuted => "Health check was executed",
                AuditEventType.ConfigurationChanged => "System configuration was modified",
                AuditEventType.AuthenticationSuccess => "Successful authentication event",
                AuditEventType.AuthenticationFailure => "Failed authentication attempt",
                AuditEventType.NotificationSent => "Notification was sent successfully",
                AuditEventType.NotificationFailed => "Notification sending failed",
                AuditEventType.JobScheduled => "Job was scheduled for execution",
                AuditEventType.JobExecuted => "Scheduled job was executed",
                AuditEventType.JobFailed => "Scheduled job execution failed",
                AuditEventType.UserAction => "User-initiated action",
                AuditEventType.SecurityEvent => "Security-related event",
                AuditEventType.PerformanceMetric => "Performance metric recorded",
                AuditEventType.DataExport => "Data export operation",
                AuditEventType.ApiRequest => "API request received",
                _ => "Unknown event type"
            };
        }

        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            
            // Escape commas, quotes, and newlines
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            
            return value;
        }
    }
}