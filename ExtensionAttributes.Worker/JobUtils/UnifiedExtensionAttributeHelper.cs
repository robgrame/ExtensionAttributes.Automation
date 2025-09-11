using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using Azure.Automation;
using Azure.Automation.Intune;
using AD.Automation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using System.Text.RegularExpressions;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils
{
    public class UnifiedExtensionAttributeHelper
    {
        private readonly ILogger<UnifiedExtensionAttributeHelper> _logger;
        private readonly IIntuneHelper _intuneHelper;
        private readonly IEntraADHelper _entraADHelper;
        private readonly IADHelper _adHelper;
        private readonly AppSettings _appSettings;

        public UnifiedExtensionAttributeHelper(
            ILogger<UnifiedExtensionAttributeHelper> logger,
            IIntuneHelper intuneHelper,
            IEntraADHelper entraADHelper,
            IADHelper adHelper,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _intuneHelper = intuneHelper;
            _entraADHelper = entraADHelper;
            _adHelper = adHelper;
            _appSettings = appSettings.Value;
        }

        public async Task<int> ProcessExtensionAttributesAsync()
        {
            var processedCount = 0;
            
            try
            {
                _logger.LogInformation("Starting unified extension attribute processing");

                // Get all Entra AD devices first
                var entraDevices = await _entraADHelper.GetDevices();
                _logger.LogInformation("Found {DeviceCount} Entra AD devices to process", entraDevices.Count());

                var semaphore = new SemaphoreSlim(10, 10); // Default concurrent requests
                var tasks = new List<Task>();

                foreach (var entraDevice in entraDevices)
                {
                    tasks.Add(ProcessDeviceAsync(entraDevice, semaphore));
                }

                await Task.WhenAll(tasks);
                processedCount = tasks.Count;

                _logger.LogInformation("Completed processing {ProcessedCount} devices for extension attributes", processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing extension attributes: {Error}", ex.Message);
            }

            return processedCount;
        }

        private async Task ProcessDeviceAsync(Device entraDevice, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            
            try
            {
                if (string.IsNullOrEmpty(entraDevice.DeviceId))
                {
                    _logger.LogWarning("Skipping device with null DeviceId: {DisplayName}", entraDevice.DisplayName);
                    return;
                }

                _logger.LogDebug("Processing device: {DisplayName} (DeviceId: {DeviceId})", entraDevice.DisplayName, entraDevice.DeviceId);

                // Process each extension attribute mapping
                foreach (var mapping in _appSettings.ExtensionAttributeMappings)
                {
                    await ProcessExtensionAttributeMapping(entraDevice, mapping);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing device {DisplayName}: {Error}", entraDevice.DisplayName, ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ProcessExtensionAttributeMapping(Device entraDevice, ExtensionAttributeMapping mapping)
        {
            try
            {
                _logger.LogDebug("Processing mapping: {Mapping}", mapping.ToString());

                // Check if the data source is enabled
                if (mapping.DataSource == DataSourceType.ActiveDirectory && !_appSettings.DataSources.EnableActiveDirectory)
                {
                    _logger.LogDebug("Skipping AD mapping for {ExtensionAttribute} - AD disabled", mapping.ExtensionAttribute);
                    return;
                }

                if (mapping.DataSource == DataSourceType.Intune && !_appSettings.DataSources.EnableIntune)
                {
                    _logger.LogDebug("Skipping Intune mapping for {ExtensionAttribute} - Intune disabled", mapping.ExtensionAttribute);
                    return;
                }

                // Get the current value from Entra AD
                var currentValue = await _entraADHelper.GetExtensionAttribute(entraDevice.DeviceId!, mapping.ExtensionAttribute);
                
                // Get the value from the appropriate data source
                var sourceValue = await GetSourceValue(entraDevice, mapping);
                
                if (string.IsNullOrEmpty(sourceValue))
                {
                    sourceValue = mapping.DefaultValue;
                    _logger.LogDebug("Using default value for {ExtensionAttribute}: {DefaultValue}", 
                        mapping.ExtensionAttribute, mapping.DefaultValue);
                }

                // Apply regex if specified
                if (!string.IsNullOrEmpty(mapping.Regex) && !string.IsNullOrEmpty(sourceValue))
                {
                    sourceValue = ApplyRegex(sourceValue, mapping.Regex, mapping.DefaultValue, entraDevice.DisplayName);
                }

                // Check if the value needs to be updated
                if (!string.Equals(currentValue, sourceValue, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Updating {ExtensionAttribute} for device {DeviceName}: '{OldValue}' -> '{NewValue}' (Source: {DataSource})",
                        mapping.ExtensionAttribute, entraDevice.DisplayName, currentValue ?? "null", sourceValue ?? "null", mapping.DataSource);

                    // Update the extension attribute
                    var result = await _entraADHelper.SetExtensionAttributeValue(entraDevice.DeviceId!, mapping.ExtensionAttribute, sourceValue ?? string.Empty);
                    
                    if (!string.IsNullOrEmpty(result))
                    {
                        _logger.LogInformation("Successfully updated {ExtensionAttribute} for device {DeviceName}", 
                            mapping.ExtensionAttribute, entraDevice.DisplayName);
                    }
                    else
                    {
                        _logger.LogError("Failed to update {ExtensionAttribute} for device {DeviceName}", 
                            mapping.ExtensionAttribute, entraDevice.DisplayName);
                    }
                }
                else
                {
                    _logger.LogDebug("No update needed for {ExtensionAttribute} on device {DeviceName}. Current value: {CurrentValue}",
                        mapping.ExtensionAttribute, entraDevice.DisplayName, currentValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing extension attribute mapping {ExtensionAttribute} for device {DeviceName}: {Error}",
                    mapping.ExtensionAttribute, entraDevice.DisplayName, ex.Message);
            }
        }

        private async Task<string?> GetSourceValue(Device entraDevice, ExtensionAttributeMapping mapping)
        {
            try
            {
                return mapping.DataSource switch
                {
                    DataSourceType.ActiveDirectory => await GetActiveDirectoryValue(entraDevice, mapping),
                    DataSourceType.Intune => await GetIntuneValue(entraDevice, mapping),
                    _ => throw new ArgumentException($"Unsupported data source: {mapping.DataSource}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting source value for {DataSource}.{SourceAttribute}: {Error}",
                    mapping.DataSource, mapping.SourceAttribute, ex.Message);
                return mapping.DefaultValue;
            }
        }

        private async Task<string?> GetActiveDirectoryValue(Device entraDevice, ExtensionAttributeMapping mapping)
        {
            try
            {
                if (string.IsNullOrEmpty(entraDevice.DisplayName))
                {
                    _logger.LogWarning("Device display name is null for AD lookup");
                    return mapping.DefaultValue;
                }

                // Find the AD computer object by name
                var adComputer = await _adHelper.GetDirectoryEntryWithAttributeAsync(
                    $"CN={entraDevice.DisplayName}", mapping.SourceAttribute);

                if (adComputer != null)
                {
                    var value = await _adHelper.GetComputerAttributeAsync(adComputer.Path, mapping.SourceAttribute);
                    _logger.LogDebug("Retrieved AD value for {SourceAttribute}: {Value}",
                        mapping.SourceAttribute, value ?? "null");
                    return value;
                }
                else
                {
                    _logger.LogDebug("AD computer not found for device: {DisplayName}", entraDevice.DisplayName);
                    return mapping.DefaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AD value for device {DisplayName}: {Error}",
                    entraDevice.DisplayName, ex.Message);
                return mapping.DefaultValue;
            }
        }

        private async Task<string?> GetIntuneValue(Device entraDevice, ExtensionAttributeMapping mapping)
        {
            try
            {
                if (string.IsNullOrEmpty(entraDevice.DeviceId))
                {
                    _logger.LogWarning("Device ID is null for Intune lookup");
                    return mapping.DefaultValue;
                }

                // Find corresponding Intune device
                var intuneDevice = await _intuneHelper.GetDeviceDetailsByEntraDeviceId(entraDevice.DeviceId);
                
                if (intuneDevice == null)
                {
                    // Try to find by device name as fallback
                    if (!string.IsNullOrEmpty(entraDevice.DisplayName))
                    {
                        intuneDevice = await _intuneHelper.FindDeviceByComputerName(entraDevice.DisplayName);
                    }
                    
                    if (intuneDevice == null)
                    {
                        _logger.LogDebug("No corresponding Intune device found for Entra device: {DisplayName}", entraDevice.DisplayName);
                        return mapping.DefaultValue;
                    }
                }

                string? value = null;

                if (mapping.UseHardwareInfo && !string.IsNullOrEmpty(intuneDevice.Id))
                {
                    // Get hardware information
                    var hardwareInfo = await _intuneHelper.GetDeviceHardwareInformation(intuneDevice.Id);
                    if (hardwareInfo.ContainsKey(mapping.SourceAttribute.ToLowerInvariant()))
                    {
                        value = hardwareInfo[mapping.SourceAttribute.ToLowerInvariant()];
                    }
                }
                else
                {
                    // Get value from the managed device object
                    value = ExtractIntuneDeviceValue(intuneDevice, mapping.SourceAttribute);
                }

                _logger.LogDebug("Retrieved Intune value for {SourceAttribute}: {Value}",
                    mapping.SourceAttribute, value ?? "null");

                return value ?? mapping.DefaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Intune value for device {DisplayName}: {Error}",
                    entraDevice.DisplayName, ex.Message);
                return mapping.DefaultValue;
            }
        }

        private string? ExtractIntuneDeviceValue(ManagedDevice device, string propertyName)
        {
            try
            {
                // Direct property mapping for Intune devices
                return propertyName.ToLowerInvariant() switch
                {
                    "devicename" => device.DeviceName,
                    "operatingsystem" => device.OperatingSystem,
                    "osversion" => device.OsVersion,
                    "serialnumber" => device.SerialNumber,
                    "manufacturer" => device.Manufacturer,
                    "model" => device.Model,
                    "compliancestate" => device.ComplianceState?.ToString(),
                    "lastsyncdate" => device.LastSyncDateTime?.ToString("yyyy-MM-dd"),
                    "lastsynctime" => device.LastSyncDateTime?.ToString("HH:mm:ss"),
                    "lastsyncfull" => device.LastSyncDateTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                    "deviceid" => device.Id,
                    "azureaddeviceid" => device.AzureADDeviceId,
                    "userprincipalname" => device.UserPrincipalName,
                    "enrolleddate" => device.EnrolledDateTime?.ToString("yyyy-MM-dd"),
                    "manageddeviceownertype" => device.ManagedDeviceOwnerType?.ToString(),
                    "managementagent" => device.ManagementAgent?.ToString(),
                    "totalstorage" => FormatStorageSize(device.TotalStorageSpaceInBytes),
                    "totalstoragegb" => FormatStorageSizeGB(device.TotalStorageSpaceInBytes),
                    "freestorage" => FormatStorageSize(device.FreeStorageSpaceInBytes),
                    "freestoragegb" => FormatStorageSizeGB(device.FreeStorageSpaceInBytes),
                    "phonenumber" => device.PhoneNumber,
                    "wifimacaddress" => device.WiFiMacAddress,
                    "imei" => device.Imei,
                    "meid" => device.Meid,
                    "subscribercarrier" => device.SubscriberCarrier,
                    _ => GetPropertyByReflection(device, propertyName)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting Intune device value for property {PropertyName}: {Error}", propertyName, ex.Message);
                return null;
            }
        }

        private string? ApplyRegex(string value, string regexPattern, string? defaultValue, string? deviceName)
        {
            try
            {
                var regex = new Regex(regexPattern);
                var match = regex.Match(value);
                
                if (match.Success)
                {
                    // Use the first capture group if available, otherwise the whole match
                    var extractedValue = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    _logger.LogDebug("Regex applied successfully. Original: {Original}, Extracted: {Extracted}", 
                        value, extractedValue);
                    return extractedValue;
                }
                else
                {
                    _logger.LogWarning("Regex pattern '{Pattern}' did not match value '{Value}' for device {DeviceName}", 
                        regexPattern, value, deviceName);
                    return defaultValue;
                }
            }
            catch (Exception regexEx)
            {
                _logger.LogError(regexEx, "Error applying regex pattern '{Pattern}' to value '{Value}': {Error}", 
                    regexPattern, value, regexEx.Message);
                return defaultValue;
            }
        }

        private string? GetPropertyByReflection(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName, 
                    System.Reflection.BindingFlags.IgnoreCase | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                
                if (property != null)
                {
                    var value = property.GetValue(obj);
                    return value?.ToString();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property {PropertyName} by reflection: {Error}", propertyName, ex.Message);
                return null;
            }
        }

        private string? FormatStorageSize(long? bytes)
        {
            if (!bytes.HasValue || bytes.Value <= 0)
                return null;

            const double gb = 1024 * 1024 * 1024;
            const double mb = 1024 * 1024;
            const double kb = 1024;

            if (bytes >= gb)
                return $"{bytes.Value / gb:F2} GB";
            else if (bytes >= mb)
                return $"{bytes.Value / mb:F2} MB";
            else if (bytes >= kb)
                return $"{bytes.Value / kb:F2} KB";
            else
                return $"{bytes.Value} B";
        }

        private string? FormatStorageSizeGB(long? bytes)
        {
            if (!bytes.HasValue || bytes.Value <= 0)
                return null;

            const double gb = 1024 * 1024 * 1024;
            return $"{bytes.Value / gb:F0}";
        }
    }
}