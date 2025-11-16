using Nimbus.ExtensionAttributes.WorkerSvc.Config;
using Nimbus.ExtensionAttributes.WorkerSvc.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nimbus.ExtensionAttributes.EntraAD;
using Nimbus.ExtensionAttributes.AD;
using Nimbus.ExtensionAttributes.Intune;
using Microsoft.Graph.Models;
using System.Diagnostics;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Nimbus.ExtensionAttributes.WorkerSvc.JobUtils
{
    /// <summary>
    /// Unified helper that processes extension attributes from both Active Directory and Intune
    /// based on the configuration, with comprehensive audit logging
    /// </summary>
    public class UnifiedExtensionAttributeHelper
    {
        private readonly ILogger<UnifiedExtensionAttributeHelper> _logger;
        private readonly AppSettings _appSettings;
        private readonly IEntraADHelper _entraADHelper;
        private readonly IADHelper _adHelper;
        private readonly IIntuneHelper _intuneHelper;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogger _auditLogger;

        public UnifiedExtensionAttributeHelper(
            ILogger<UnifiedExtensionAttributeHelper> logger,
            IOptions<AppSettings> appSettings,
            IEntraADHelper entraADHelper,
            IADHelper adHelper,
            IIntuneHelper intuneHelper,
            INotificationService notificationService,
            IAuditLogger auditLogger)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _entraADHelper = entraADHelper;
            _adHelper = adHelper;
            _intuneHelper = intuneHelper;
            _notificationService = notificationService;
            _auditLogger = auditLogger;
        }

        /// <summary>
        /// Process extension attributes for all devices using unified configuration
        /// </summary>
        public async Task<int> ProcessExtensionAttributesAsync()
        {
            var overallStopwatch = Stopwatch.StartNew();
            
            try
            {
                await _auditLogger.LogSystemEventAsync(
                    AuditEventType.DeviceProcessingStarted,
                    "Starting unified extension attribute processing for all devices",
                    new Dictionary<string, object>
                    {
                        ["MappingCount"] = _appSettings.ExtensionAttributeMappings.Count,
                        ["ADEnabled"] = _appSettings.DataSources.EnableActiveDirectory,
                        ["IntuneEnabled"] = _appSettings.DataSources.EnableIntune,
                        ["PreferredDataSource"] = _appSettings.DataSources.PreferredDataSource
                    });

                _logger.LogInformation("?? Starting unified extension attribute processing...");
                _logger.LogInformation("Configuration - AD: {EnableAD}, Intune: {EnableIntune}, Preferred: {Preferred}",
                    _appSettings.DataSources.EnableActiveDirectory,
                    _appSettings.DataSources.EnableIntune,
                    _appSettings.DataSources.PreferredDataSource);

                // Validate configuration
                if (!ValidateConfiguration())
                {
                    await _auditLogger.LogSystemEventAsync(
                        AuditEventType.ConfigurationChanged,
                        "Configuration validation failed",
                        new Dictionary<string, object> { ["ValidationResult"] = "Failed" });
                    
                    return 0;
                }

                // Get all Entra AD devices
                _logger.LogInformation("?? Retrieving Entra AD devices...");
                var entraDevices = (await _entraADHelper.GetDevices()).ToList();
                _logger.LogInformation("Found {DeviceCount} devices in Entra AD", entraDevices.Count);

                if (!entraDevices.Any())
                {
                    _logger.LogWarning("No devices found in Entra AD");
                    await _auditLogger.LogSystemEventAsync(
                        AuditEventType.DeviceProcessingCompleted,
                        "Processing completed - No devices found in Entra AD");
                    return 0;
                }

                // Process devices concurrently
                var processedCount = 0;
                var failedCount = 0;
                var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);

                var tasks = entraDevices.Select(async device =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var deviceProcessed = await ProcessSingleDeviceInternalAsync(device);
                        if (deviceProcessed)
                            Interlocked.Increment(ref processedCount);
                        else
                            Interlocked.Increment(ref failedCount);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                overallStopwatch.Stop();
                
                // Log completion and performance metrics
                await _auditLogger.LogPerformanceMetricAsync(
                    "UnifiedProcessingCompleted",
                    processedCount,
                    new Dictionary<string, object>
                    {
                        ["TotalDevices"] = entraDevices.Count,
                        ["ProcessedCount"] = processedCount,
                        ["FailedCount"] = failedCount,
                        ["ProcessingTimeMs"] = overallStopwatch.ElapsedMilliseconds,
                        ["DevicesPerMinute"] = entraDevices.Count / Math.Max(overallStopwatch.Elapsed.TotalMinutes, 1)
                    });

                await _auditLogger.LogSystemEventAsync(
                    AuditEventType.DeviceProcessingCompleted,
                    $"Unified processing completed - Processed: {processedCount}, Failed: {failedCount}",
                    new Dictionary<string, object>
                    {
                        ["TotalDevices"] = entraDevices.Count,
                        ["ProcessedCount"] = processedCount,
                        ["FailedCount"] = failedCount,
                        ["DurationMs"] = overallStopwatch.ElapsedMilliseconds
                    });

                _logger.LogInformation("? Unified processing completed in {ElapsedTime}. Processed: {ProcessedCount}, Failed: {FailedCount}",
                    overallStopwatch.Elapsed, processedCount, failedCount);

                // Export results if configured
                await ExportResultsAsync(entraDevices, processedCount, failedCount);

                // Send notification if there were failures (comment out until notification method is implemented)
                // if (failedCount > 10) // Use hardcoded threshold for now
                // {
                //     await _notificationService.SendDeviceProcessingFailureNotificationAsync(failedCount, entraDevices.Count);
                // }

                return processedCount;
            }
            catch (Exception ex)
            {
                overallStopwatch.Stop();
                
                await _auditLogger.LogSystemEventAsync(
                    AuditEventType.DeviceProcessingFailed,
                    $"Unified processing failed with exception: {ex.Message}",
                    new Dictionary<string, object>
                    {
                        ["ExceptionType"] = ex.GetType().Name,
                        ["StackTrace"] = ex.StackTrace ?? string.Empty,
                        ["DurationMs"] = overallStopwatch.ElapsedMilliseconds
                    });

                _logger.LogError(ex, "? Unified processing failed: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Process extension attributes for a single device by name
        /// </summary>
        public async Task<bool> ProcessSingleDeviceAsync(string deviceName)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await _auditLogger.LogUserActionAsync(
                    $"Single device processing requested for: {deviceName}",
                    deviceName,
                    new Dictionary<string, object> { ["RequestType"] = "ByName" });

                _logger.LogInformation("?? Processing single device: {DeviceName}", deviceName);

                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    _logger.LogError("Device name cannot be null or empty");
                    return false;
                }

                // Find the device in Entra AD
                _logger.LogInformation("?? Looking up device in Entra AD: {DeviceName}", deviceName);
                var entraDevices = (await _entraADHelper.GetDevices()).ToList();
                var targetDevice = entraDevices.FirstOrDefault(d =>
                    string.Equals(d.DisplayName, deviceName, StringComparison.OrdinalIgnoreCase));

                if (targetDevice == null)
                {
                    _logger.LogWarning("? Device not found in Entra AD: {DeviceName}", deviceName);
                    await _auditLogger.LogAsync(
                        AuditEventType.DeviceProcessingFailed,
                        $"Device not found in Entra AD: {deviceName}",
                        AuditSeverity.Medium);
                    return false;
                }

                _logger.LogInformation("? Found device in Entra AD: {DisplayName} (ID: {DeviceId})", 
                    targetDevice.DisplayName, targetDevice.Id);

                var result = await ProcessSingleDeviceInternalAsync(targetDevice);
                
                stopwatch.Stop();
                await _auditLogger.LogPerformanceMetricAsync(
                    "SingleDeviceProcessingTime",
                    stopwatch.ElapsedMilliseconds,
                    new Dictionary<string, object>
                    {
                        ["DeviceName"] = deviceName,
                        ["Success"] = result
                    });

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                await _auditLogger.LogAsync(
                    AuditEventType.DeviceProcessingFailed,
                    $"Exception processing device {deviceName}: {ex.Message}",
                    AuditSeverity.High);

                _logger.LogError(ex, "? Failed to process device {DeviceName}: {Error}", deviceName, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Process extension attributes for a single device by Entra AD Device ID
        /// </summary>
        public async Task<bool> ProcessSingleDeviceByIdAsync(string deviceId)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await _auditLogger.LogUserActionAsync(
                    $"Single device processing requested for ID: {deviceId}",
                    additionalData: new Dictionary<string, object> 
                    { 
                        ["RequestType"] = "ByDeviceId",
                        ["DeviceId"] = deviceId
                    });

                _logger.LogInformation("?? Processing single device by ID: {DeviceId}", deviceId);

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    _logger.LogError("Device ID cannot be null or empty");
                    return false;
                }

                // Get the specific device from Entra AD by ID
                _logger.LogInformation("?? Looking up device in Entra AD by ID: {DeviceId}", deviceId);
                var targetDevice = await _entraADHelper.GetDeviceAsync(deviceId);

                if (targetDevice == null)
                {
                    _logger.LogWarning("? Device not found in Entra AD by ID: {DeviceId}", deviceId);
                    await _auditLogger.LogAsync(
                        AuditEventType.DeviceProcessingFailed,
                        $"Device not found in Entra AD by ID: {deviceId}",
                        AuditSeverity.Medium);
                    return false;
                }

                _logger.LogInformation("? Found device in Entra AD: {DisplayName} (ID: {DeviceId})", 
                    targetDevice.DisplayName, targetDevice.Id);

                var result = await ProcessSingleDeviceInternalAsync(targetDevice);
                
                stopwatch.Stop();
                await _auditLogger.LogPerformanceMetricAsync(
                    "SingleDeviceProcessingTimeById",
                    stopwatch.ElapsedMilliseconds,
                    new Dictionary<string, object>
                    {
                        ["DeviceId"] = deviceId,
                        ["DeviceName"] = targetDevice.DisplayName ?? "Unknown",
                        ["Success"] = result
                    });

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                await _auditLogger.LogAsync(
                    AuditEventType.DeviceProcessingFailed,
                    $"Exception processing device ID {deviceId}: {ex.Message}",
                    AuditSeverity.High);

                _logger.LogError(ex, "? Failed to process device by ID {DeviceId}: {Error}", deviceId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Internal method to process a single device
        /// </summary>
        private async Task<bool> ProcessSingleDeviceInternalAsync(Device entraDevice)
        {
            var deviceStopwatch = Stopwatch.StartNew();
            var deviceName = entraDevice.DisplayName ?? "Unknown";
            var processedMappings = 0;
            var failedMappings = 0;

            try
            {
                _logger.LogDebug("Processing device: {DeviceName} (ID: {DeviceId})", deviceName, entraDevice.Id);

                // Get enabled mappings based on configuration
                var enabledMappings = GetEnabledMappings();
                if (!enabledMappings.Any())
                {
                    _logger.LogWarning("No enabled extension attribute mappings found for device: {DeviceName}", deviceName);
                    return true; // Not an error, just no work to do
                }

                _logger.LogDebug("Processing {MappingCount} extension attribute mappings for device: {DeviceName}",
                    enabledMappings.Count, deviceName);

                // Process each enabled mapping
                foreach (var mapping in enabledMappings)
                {
                    var mappingStopwatch = Stopwatch.StartNew();
                    
                    try
                    {
                        var mappingResult = await ProcessSingleMappingAsync(entraDevice, mapping);
                        mappingStopwatch.Stop();

                        if (mappingResult)
                        {
                            processedMappings++;
                            await _auditLogger.LogPerformanceMetricAsync(
                                $"MappingProcessingTime_{mapping.ExtensionAttribute}",
                                mappingStopwatch.ElapsedMilliseconds,
                                new Dictionary<string, object>
                                {
                                    ["DeviceName"] = deviceName,
                                    ["ExtensionAttribute"] = mapping.ExtensionAttribute,
                                    ["DataSource"] = mapping.DataSource.ToString()
                                });
                        }
                        else
                        {
                            failedMappings++;
                        }
                    }
                    catch (Exception ex)
                    {
                        mappingStopwatch.Stop();
                        failedMappings++;
                        
                        await _auditLogger.LogDeviceProcessingAsync(
                            deviceName,
                            mapping.ExtensionAttribute,
                            null,
                            null,
                            false,
                            mappingStopwatch.Elapsed,
                            $"Mapping processing exception: {ex.Message}");

                        _logger.LogError(ex, "Failed to process mapping {ExtensionAttribute} for device {DeviceName}: {Error}",
                            mapping.ExtensionAttribute, deviceName, ex.Message);
                    }
                }

                deviceStopwatch.Stop();
                
                await _auditLogger.LogPerformanceMetricAsync(
                    "DeviceProcessingCompleted",
                    deviceStopwatch.ElapsedMilliseconds,
                    new Dictionary<string, object>
                    {
                        ["DeviceName"] = deviceName,
                        ["ProcessedMappings"] = processedMappings,
                        ["FailedMappings"] = failedMappings,
                        ["TotalMappings"] = enabledMappings.Count
                    });

                var deviceSuccess = failedMappings == 0;
                _logger.LogDebug("{Status} processing device {DeviceName}: Processed {ProcessedCount}/{TotalCount} mappings in {ElapsedTime}",
                    deviceSuccess ? "?" : "??", deviceName, processedMappings, enabledMappings.Count, deviceStopwatch.Elapsed);

                return deviceSuccess;
            }
            catch (Exception ex)
            {
                deviceStopwatch.Stop();
                
                await _auditLogger.LogDeviceProcessingAsync(
                    deviceName,
                    "All",
                    null,
                    null,
                    false,
                    deviceStopwatch.Elapsed,
                    $"Device processing exception: {ex.Message}");

                _logger.LogError(ex, "? Failed to process device {DeviceName}: {Error}", deviceName, ex.Message);
                return false;
            }
        }

        private bool ValidateConfiguration()
        {
            if (_appSettings.ExtensionAttributeMappings == null || !_appSettings.ExtensionAttributeMappings.Any())
            {
                _logger.LogError("? No extension attribute mappings configured");
                return false;
            }

            // Check for duplicate extension attributes
            var duplicateAttributes = _appSettings.ExtensionAttributeMappings
                .GroupBy(m => m.ExtensionAttribute)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateAttributes.Any())
            {
                _logger.LogError("? Duplicate extension attribute mappings found: {DuplicateAttributes}",
                    string.Join(", ", duplicateAttributes));
                return false;
            }

            // Validate that at least one data source is enabled
            if (!_appSettings.DataSources.EnableActiveDirectory && !_appSettings.DataSources.EnableIntune)
            {
                _logger.LogError("? At least one data source (Active Directory or Intune) must be enabled");
                return false;
            }

            _logger.LogInformation("? Configuration validation passed");
            return true;
        }

        private List<ExtensionAttributeMapping> GetEnabledMappings()
        {
            return _appSettings.ExtensionAttributeMappings.Where(mapping =>
                (mapping.DataSource == DataSourceType.ActiveDirectory && _appSettings.DataSources.EnableActiveDirectory) ||
                (mapping.DataSource == DataSourceType.Intune && _appSettings.DataSources.EnableIntune)
            ).ToList();
        }

        private async Task<bool> ProcessSingleMappingAsync(Device entraDevice, ExtensionAttributeMapping mapping)
        {
            var deviceName = entraDevice.DisplayName ?? "Unknown";
            
            try
            {
                string? sourceValue = null;

                // Get source value based on data source type
                if (mapping.DataSource == DataSourceType.ActiveDirectory && _appSettings.DataSources.EnableActiveDirectory)
                {
                    sourceValue = await GetActiveDirectoryValueAsync(deviceName, mapping);
                }
                else if (mapping.DataSource == DataSourceType.Intune && _appSettings.DataSources.EnableIntune)
                {
                    sourceValue = await GetIntuneValueAsync(deviceName, mapping);
                }

                if (sourceValue == null)
                {
                    _logger.LogDebug("No source value found for {ExtensionAttribute} from {DataSource} for device {DeviceName}",
                        mapping.ExtensionAttribute, mapping.DataSource, deviceName);
                    sourceValue = mapping.DefaultValue;
                }

                // Apply regex if specified
                if (!string.IsNullOrEmpty(mapping.Regex) && !string.IsNullOrEmpty(sourceValue))
                {
                    sourceValue = ApplyRegex(sourceValue, mapping.Regex) ?? mapping.DefaultValue;
                }

                // Ensure we have a value (use default if still null)
                sourceValue ??= mapping.DefaultValue;

                if (string.IsNullOrEmpty(sourceValue))
                {
                    _logger.LogWarning("?? No value available for {ExtensionAttribute} for device {DeviceName} (no default value configured)",
                        mapping.ExtensionAttribute, deviceName);
                    
                    await _auditLogger.LogDeviceProcessingAsync(
                        deviceName,
                        mapping.ExtensionAttribute,
                        null,
                        null,
                        false,
                        TimeSpan.Zero,
                        "No value available and no default value configured");
                        
                    return false;
                }

                // Get current extension attribute value
                var currentValue = GetExtensionAttributeValue(entraDevice, mapping.ExtensionAttribute);

                // Only update if value has changed
                if (currentValue == sourceValue)
                {
                    _logger.LogDebug("? {ExtensionAttribute} for device {DeviceName} already has correct value: {Value}",
                        mapping.ExtensionAttribute, deviceName, sourceValue);
                    
                    await _auditLogger.LogDeviceProcessingAsync(
                        deviceName,
                        mapping.ExtensionAttribute,
                        currentValue,
                        sourceValue,
                        true,
                        TimeSpan.Zero,
                        null);
                        
                    return true;
                }

                // Update the extension attribute
                _logger.LogInformation("?? Updating {ExtensionAttribute} for device {DeviceName}: '{OldValue}' -> '{NewValue}'",
                    mapping.ExtensionAttribute, deviceName, currentValue ?? "null", sourceValue);

                var updateStopwatch = Stopwatch.StartNew();
                var updateResult = await _entraADHelper.SetExtensionAttributeValue(
                    entraDevice.Id!, mapping.ExtensionAttribute, sourceValue);
                updateStopwatch.Stop();

                if (!string.IsNullOrEmpty(updateResult))
                {
                    await _auditLogger.LogDeviceProcessingAsync(
                        deviceName,
                        mapping.ExtensionAttribute,
                        currentValue,
                        sourceValue,
                        true,
                        updateStopwatch.Elapsed);

                    _logger.LogInformation("? Successfully updated {ExtensionAttribute} for device {DeviceName}",
                        mapping.ExtensionAttribute, deviceName);
                    return true;
                }
                else
                {
                    await _auditLogger.LogDeviceProcessingAsync(
                        deviceName,
                        mapping.ExtensionAttribute,
                        currentValue,
                        sourceValue,
                        false,
                        updateStopwatch.Elapsed,
                        "Failed to update extension attribute via Graph API");

                    _logger.LogError("? Failed to update {ExtensionAttribute} for device {DeviceName}",
                        mapping.ExtensionAttribute, deviceName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _auditLogger.LogDeviceProcessingAsync(
                    deviceName,
                    mapping.ExtensionAttribute,
                    null,
                    null,
                    false,
                    TimeSpan.Zero,
                    $"Exception during mapping processing: {ex.Message}");

                _logger.LogError(ex, "? Exception processing mapping {ExtensionAttribute} for device {DeviceName}: {Error}",
                    mapping.ExtensionAttribute, deviceName, ex.Message);
                return false;
            }
        }

        private async Task<string?> GetActiveDirectoryValueAsync(string deviceName, ExtensionAttributeMapping mapping)
        {
            try
            {
                // AD Helper doesn't have a GetComputersAsync method, we need to implement a different approach
                // For now, we'll try to find the device by name using directory search
                await foreach (var adEntry in _adHelper.GetDirectoryEntriesAsyncEnumerable(""))
                {
                    if (string.Equals(adEntry.Name, $"CN={deviceName}", StringComparison.OrdinalIgnoreCase))
                    {
                        return GetPropertyValue(adEntry.Properties, mapping.SourceAttribute);
                    }
                }

                _logger.LogDebug("Device {DeviceName} not found in Active Directory", deviceName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Active Directory value for {DeviceName}: {Error}", deviceName, ex.Message);
                return null;
            }
        }

        private async Task<string?> GetIntuneValueAsync(string deviceName, ExtensionAttributeMapping mapping)
        {
            try
            {
                // Try to get device details by name
                var intuneDevice = await _intuneHelper.GetDeviceDetailsbyName(deviceName);

                if (intuneDevice == null)
                {
                    _logger.LogDebug("Device {DeviceName} not found in Intune", deviceName);
                    return null;
                }

                // If hardware info is requested, get hardware information
                if (mapping.UseHardwareInfo)
                {
                    var hardwareInfo = await _intuneHelper.GetDeviceHardwareInformationByName(deviceName);
                    if (hardwareInfo.TryGetValue(mapping.SourceAttribute, out var hardwareValue))
                    {
                        return hardwareValue;
                    }
                }

                // Otherwise get property from managed device
                return GetPropertyValue(intuneDevice, mapping.SourceAttribute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Intune value for {DeviceName}: {Error}", deviceName, ex.Message);
                return null;
            }
        }

        private string? GetPropertyValue(object source, string propertyName)
        {
            try
            {
                if (source == null) return null;

                var property = source.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (property == null)
                {
                    _logger.LogDebug("Property {PropertyName} not found on {SourceType}", propertyName, source.GetType().Name);
                    return null;
                }

                var value = property.GetValue(source);
                return value?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get property value for {PropertyName}: {Error}", propertyName, ex.Message);
                return null;
            }
        }

        private string? ApplyRegex(string input, string pattern)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(input, pattern);
                if (match.Success)
                {
                    // If there are named groups, try to get the first one
                    if (match.Groups.Count > 1)
                    {
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            if (match.Groups[i].Success)
                            {
                                return match.Groups[i].Value;
                            }
                        }
                    }
                    // Otherwise return the entire match
                    return match.Value;
                }

                _logger.LogDebug("Regex pattern '{Pattern}' did not match input '{Input}'", pattern, input);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply regex pattern '{Pattern}' to input '{Input}': {Error}", pattern, input, ex.Message);
                return null;
            }
        }

        private string? GetExtensionAttributeValue(Device device, string extensionAttribute)
        {
            try
            {
                if (device.AdditionalData == null) return null;

                return device.AdditionalData.TryGetValue(extensionAttribute, out var value) ? value?.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get extension attribute {ExtensionAttribute} value: {Error}", extensionAttribute, ex.Message);
                return null;
            }
        }

        private async Task ExportResultsAsync(List<Device> processedDevices, int processedCount, int failedCount)
        {
            try
            {
                if (string.IsNullOrEmpty(_appSettings.ExportPath))
                {
                    _logger.LogDebug("Export path not configured, skipping export");
                    return;
                }

                var exportStopwatch = Stopwatch.StartNew();
                
                Directory.CreateDirectory(_appSettings.ExportPath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var fileName = $"{_appSettings.ExportFileNamePrefix}-{timestamp}.csv";
                var filePath = Path.Combine(_appSettings.ExportPath, fileName);

                _logger.LogInformation("?? Exporting results to: {FilePath}", filePath);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                using var writer = new StringWriter();
                using var csv = new CsvWriter(writer, config);

                // Write header
                csv.WriteField("DeviceName");
                csv.WriteField("DeviceId");
                csv.WriteField("ProcessingStatus");
                csv.WriteField("Timestamp");
                foreach (var mapping in _appSettings.ExtensionAttributeMappings)
                {
                    csv.WriteField($"{mapping.ExtensionAttribute}_Value");
                    csv.WriteField($"{mapping.ExtensionAttribute}_Source");
                }
                csv.NextRecord();

                // Write data
                foreach (var device in processedDevices)
                {
                    csv.WriteField(device.DisplayName ?? "Unknown");
                    csv.WriteField(device.Id ?? "Unknown");
                    csv.WriteField("Processed"); // We could track individual device status if needed
                    csv.WriteField(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));

                    foreach (var mapping in _appSettings.ExtensionAttributeMappings)
                    {
                        var value = GetExtensionAttributeValue(device, mapping.ExtensionAttribute);
                        csv.WriteField(value ?? "");
                        csv.WriteField(mapping.DataSource.ToString());
                    }
                    csv.NextRecord();
                }

                await File.WriteAllTextAsync(filePath, writer.ToString());
                exportStopwatch.Stop();

                await _auditLogger.LogSystemEventAsync(
                    AuditEventType.DataExport,
                    $"Results exported to {fileName}",
                    new Dictionary<string, object>
                    {
                        ["FilePath"] = filePath,
                        ["DeviceCount"] = processedDevices.Count,
                        ["ProcessedCount"] = processedCount,
                        ["FailedCount"] = failedCount,
                        ["ExportDurationMs"] = exportStopwatch.ElapsedMilliseconds
                    });

                _logger.LogInformation("? Results exported successfully to: {FilePath} (took {ElapsedTime})", 
                    filePath, exportStopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                await _auditLogger.LogSystemEventAsync(
                    AuditEventType.DataExport,
                    $"Export failed: {ex.Message}",
                    new Dictionary<string, object> { ["Error"] = ex.Message });

                _logger.LogError(ex, "? Failed to export results: {Error}", ex.Message);
            }
        }
    }
}