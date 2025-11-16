using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Nimbus.ExtensionAttributes.WorkerSvc.Hubs;
using System.Text.Json;
using System.Diagnostics;

namespace Nimbus.ExtensionAttributes.WorkerSvc.Services
{
    /// <summary>
    /// Audit event types for tracking operations
    /// </summary>
    public enum AuditEventType
    {
        SystemStartup,
        SystemShutdown,
        DeviceProcessingStarted,
        DeviceProcessingCompleted,
        DeviceProcessingFailed,
        ExtensionAttributeUpdated,
        ExtensionAttributeUpdateFailed,
        HealthCheckExecuted,
        ConfigurationChanged,
        AuthenticationSuccess,
        AuthenticationFailure,
        NotificationSent,
        NotificationFailed,
        JobScheduled,
        JobExecuted,
        JobFailed,
        UserAction,
        SecurityEvent,
        PerformanceMetric,
        DataExport,
        ApiRequest
    }

    /// <summary>
    /// Audit severity levels
    /// </summary>
    public enum AuditSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Audit log entry model
    /// </summary>
    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public AuditEventType EventType { get; set; }
        public AuditSeverity Severity { get; set; }
        public string Source { get; set; } = string.Empty;
        public string User { get; set; } = Environment.UserName;
        public string MachineName { get; set; } = Environment.MachineName;
        public string DeviceName { get; set; } = string.Empty;
        public string ExtensionAttribute { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalData { get; set; } = new();
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public TimeSpan? Duration { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for audit logging service
    /// </summary>
    public interface IAuditLogger
    {
        Task LogAsync(AuditLogEntry entry);
        Task LogAsync(AuditEventType eventType, string description, AuditSeverity severity = AuditSeverity.Medium);
        Task LogDeviceProcessingAsync(string deviceName, string extensionAttribute, string? oldValue, string? newValue, bool success, TimeSpan duration, string? errorMessage = null);
        Task LogSystemEventAsync(AuditEventType eventType, string description, Dictionary<string, object>? additionalData = null);
        Task LogUserActionAsync(string action, string? deviceName = null, Dictionary<string, object>? additionalData = null);
        Task LogPerformanceMetricAsync(string metricName, object value, Dictionary<string, object>? additionalData = null);
        Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, AuditEventType? eventType = null, string? deviceName = null);
        Task<Dictionary<string, object>> GetAuditSummaryAsync(DateTime? from = null, DateTime? to = null);
    }

    /// <summary>
    /// Comprehensive audit logging service for compliance and tracking
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private readonly ILogger<AuditLogger> _logger;
        private readonly IHubContext<AuditHub>? _hubContext;
        private readonly List<AuditLogEntry> _auditEntries;
        private readonly object _lockObject = new();
        private readonly string _auditFilePath;
        private readonly int _maxEntriesInMemory;
        private readonly TimeSpan _flushInterval;
        private Timer? _flushTimer;

        public AuditLogger(ILogger<AuditLogger> logger, IHubContext<AuditHub>? hubContext = null)
        {
            _logger = logger;
            _hubContext = hubContext;
            _auditEntries = new List<AuditLogEntry>();
            _auditFilePath = Path.Combine(Path.GetTempPath(), "ExtensionAttributesAutomation", "Audit", $"audit-{DateTime.UtcNow:yyyy-MM-dd}.json");
            _maxEntriesInMemory = 1000;
            _flushInterval = TimeSpan.FromMinutes(5);

            // Ensure audit directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_auditFilePath) ?? string.Empty);

            // Setup periodic flush timer
            _flushTimer = new Timer(FlushToFile, null, _flushInterval, _flushInterval);

            _logger.LogInformation("AuditLogger initialized. Audit file: {AuditFilePath}, SignalR: {SignalREnabled}", 
                _auditFilePath, hubContext != null);
        }

        public async Task LogAsync(AuditLogEntry entry)
        {
            try
            {
                // Enhance entry with system information
                entry.MachineName = Environment.MachineName;
                entry.User = Environment.UserName;

                // Generate correlation ID if not provided
                if (string.IsNullOrEmpty(entry.CorrelationId))
                {
                    entry.CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
                }

                lock (_lockObject)
                {
                    _auditEntries.Add(entry);

                    // Flush to file if memory limit reached
                    if (_auditEntries.Count >= _maxEntriesInMemory)
                    {
                        FlushToFileInternal();
                    }
                }

                // Log to structured logging as well
                _logger.LogInformation("AUDIT: {EventType} | {DeviceName} | {Description} | Success: {Success} | Severity: {Severity}",
                    entry.EventType, entry.DeviceName, entry.Description, entry.Success, entry.Severity);

                // Broadcast to SignalR clients if available
                if (_hubContext != null)
                {
                    try
                    {
                        var auditEvent = new AuditEvent
                        {
                            EventId = entry.EventId,
                            Timestamp = entry.Timestamp,
                            EventType = entry.EventType.ToString(),
                            Severity = entry.Severity.ToString(),
                            DeviceName = entry.DeviceName,
                            ExtensionAttribute = entry.ExtensionAttribute,
                            Description = entry.Description,
                            Success = entry.Success,
                            User = entry.User,
                            OldValue = entry.OldValue,
                            NewValue = entry.NewValue
                        };

                        await _hubContext.Clients.All.SendAsync("ReceiveAuditEvent", auditEvent);
                    }
                    catch (Exception signalREx)
                    {
                        _logger.LogWarning(signalREx, "Failed to broadcast audit event via SignalR");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit entry: {EventType}", entry.EventType);
            }
        }

        public async Task LogAsync(AuditEventType eventType, string description, AuditSeverity severity = AuditSeverity.Medium)
        {
            var entry = new AuditLogEntry
            {
                EventType = eventType,
                Description = description,
                Severity = severity,
                Source = GetCallerSource()
            };

            await LogAsync(entry);
        }

        public async Task LogDeviceProcessingAsync(string deviceName, string extensionAttribute, string? oldValue, string? newValue, bool success, TimeSpan duration, string? errorMessage = null)
        {
            var entry = new AuditLogEntry
            {
                EventType = success ? AuditEventType.ExtensionAttributeUpdated : AuditEventType.ExtensionAttributeUpdateFailed,
                DeviceName = deviceName,
                ExtensionAttribute = extensionAttribute,
                OldValue = oldValue ?? string.Empty,
                NewValue = newValue ?? string.Empty,
                Success = success,
                Duration = duration,
                ErrorMessage = errorMessage,
                Severity = success ? AuditSeverity.Low : AuditSeverity.Medium,
                Description = success 
                    ? $"Successfully updated {extensionAttribute} for {deviceName}: '{oldValue}' -> '{newValue}'"
                    : $"Failed to update {extensionAttribute} for {deviceName}: {errorMessage}",
                Source = "DeviceProcessing",
                AdditionalData = new Dictionary<string, object>
                {
                    ["ProcessingDuration"] = duration.TotalMilliseconds,
                    ["ValueChanged"] = oldValue != newValue
                }
            };

            await LogAsync(entry);
        }

        public async Task LogSystemEventAsync(AuditEventType eventType, string description, Dictionary<string, object>? additionalData = null)
        {
            var entry = new AuditLogEntry
            {
                EventType = eventType,
                Description = description,
                Severity = GetDefaultSeverity(eventType),
                Source = "System",
                AdditionalData = additionalData ?? new Dictionary<string, object>()
            };

            await LogAsync(entry);
        }

        public async Task LogUserActionAsync(string action, string? deviceName = null, Dictionary<string, object>? additionalData = null)
        {
            var entry = new AuditLogEntry
            {
                EventType = AuditEventType.UserAction,
                DeviceName = deviceName ?? string.Empty,
                Description = action,
                Severity = AuditSeverity.Medium,
                Source = "UserInterface",
                AdditionalData = additionalData ?? new Dictionary<string, object>()
            };

            await LogAsync(entry);
        }

        public async Task LogPerformanceMetricAsync(string metricName, object value, Dictionary<string, object>? additionalData = null)
        {
            var data = additionalData ?? new Dictionary<string, object>();
            data["MetricName"] = metricName;
            data["MetricValue"] = value;

            var entry = new AuditLogEntry
            {
                EventType = AuditEventType.PerformanceMetric,
                Description = $"Performance metric: {metricName} = {value}",
                Severity = AuditSeverity.Low,
                Source = "Performance",
                AdditionalData = data
            };

            await LogAsync(entry);
        }

        public async Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, AuditEventType? eventType = null, string? deviceName = null)
        {
            try
            {
                var allEntries = new List<AuditLogEntry>();

                // Add in-memory entries
                lock (_lockObject)
                {
                    allEntries.AddRange(_auditEntries);
                }

                // Load from file if it exists
                if (File.Exists(_auditFilePath))
                {
                    var fileContent = await File.ReadAllTextAsync(_auditFilePath);
                    var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        try
                        {
                            var entry = JsonSerializer.Deserialize<AuditLogEntry>(line);
                            if (entry != null)
                            {
                                allEntries.Add(entry);
                            }
                        }
                        catch (JsonException)
                        {
                            // Skip invalid JSON lines
                        }
                    }
                }

                // Apply filters
                var filteredEntries = allEntries.AsEnumerable();

                if (from.HasValue)
                    filteredEntries = filteredEntries.Where(e => e.Timestamp >= from.Value);

                if (to.HasValue)
                    filteredEntries = filteredEntries.Where(e => e.Timestamp <= to.Value);

                if (eventType.HasValue)
                    filteredEntries = filteredEntries.Where(e => e.EventType == eventType.Value);

                if (!string.IsNullOrEmpty(deviceName))
                    filteredEntries = filteredEntries.Where(e => e.DeviceName.Contains(deviceName, StringComparison.OrdinalIgnoreCase));

                return filteredEntries.OrderByDescending(e => e.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve audit logs");
                return Enumerable.Empty<AuditLogEntry>();
            }
        }

        public async Task<Dictionary<string, object>> GetAuditSummaryAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var logs = await GetAuditLogsAsync(from, to);
                
                var summary = new Dictionary<string, object>
                {
                    ["TotalEntries"] = logs.Count(),
                    ["TimeRange"] = new { From = from, To = to },
                    ["EventTypeCounts"] = logs.GroupBy(l => l.EventType)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    ["SeverityCounts"] = logs.GroupBy(l => l.Severity)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    ["SuccessRate"] = logs.Any() ? (double)logs.Count(l => l.Success) / logs.Count() * 100 : 0,
                    ["DevicesProcessed"] = logs.Where(l => !string.IsNullOrEmpty(l.DeviceName))
                        .Select(l => l.DeviceName).Distinct().Count(),
                    ["ExtensionAttributesUpdated"] = logs.Count(l => l.EventType == AuditEventType.ExtensionAttributeUpdated),
                    ["FailedOperations"] = logs.Count(l => !l.Success),
                    ["AverageProcessingTime"] = logs.Where(l => l.Duration.HasValue)
                        .Select(l => l.Duration!.Value.TotalMilliseconds).DefaultIfEmpty().Average()
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate audit summary");
                return new Dictionary<string, object> { ["Error"] = ex.Message };
            }
        }

        private string GetCallerSource()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(3); // Skip current method and LogAsync calls
            return frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
        }

        private AuditSeverity GetDefaultSeverity(AuditEventType eventType)
        {
            return eventType switch
            {
                AuditEventType.SystemStartup => AuditSeverity.Medium,
                AuditEventType.SystemShutdown => AuditSeverity.Medium,
                AuditEventType.AuthenticationFailure => AuditSeverity.High,
                AuditEventType.SecurityEvent => AuditSeverity.Critical,
                AuditEventType.DeviceProcessingFailed => AuditSeverity.Medium,
                AuditEventType.ExtensionAttributeUpdateFailed => AuditSeverity.Medium,
                AuditEventType.JobFailed => AuditSeverity.Medium,
                _ => AuditSeverity.Low
            };
        }

        private void FlushToFile(object? state)
        {
            try
            {
                lock (_lockObject)
                {
                    FlushToFileInternal();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush audit entries to file");
            }
        }

        private void FlushToFileInternal()
        {
            if (!_auditEntries.Any()) return;

            try
            {
                var jsonLines = _auditEntries.Select(entry => JsonSerializer.Serialize(entry)).ToList();
                File.AppendAllLines(_auditFilePath, jsonLines);
                
                _logger.LogDebug("Flushed {Count} audit entries to {FilePath}", _auditEntries.Count, _auditFilePath);
                _auditEntries.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit entries to file: {FilePath}", _auditFilePath);
            }
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
            
            // Final flush
            lock (_lockObject)
            {
                FlushToFileInternal();
            }
        }
    }
}