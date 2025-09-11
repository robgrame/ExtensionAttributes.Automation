using Azure.Automation;
using Azure.Automation.Intune;
using Azure.Automation.Intune.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using System.Text.RegularExpressions;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils
{
    public class IntuneExtensionAttributeHelper
    {
        private readonly ILogger<IntuneExtensionAttributeHelper> _logger;
        private readonly IIntuneHelper _intuneHelper;
        private readonly IEntraADHelper _entraADHelper;
        private readonly IntuneHelperSettings _intuneSettings;

        public IntuneExtensionAttributeHelper(
            ILogger<IntuneExtensionAttributeHelper> logger,
            IIntuneHelper intuneHelper,
            IEntraADHelper entraADHelper,
            IOptions<IntuneHelperSettings> intuneSettings)
        {
            _logger = logger;
            _intuneHelper = intuneHelper;
            _entraADHelper = entraADHelper;
            _intuneSettings = intuneSettings.Value;
        }

        public async Task<int> ProcessIntuneBasedExtensionAttributesAsync()
        {
            var processedCount = 0;
            
            try
            {
                _logger.LogInformation("Starting Intune-based extension attribute processing");

                // Get all Entra AD devices first
                var entraDevices = await _entraADHelper.GetDevices();
                _logger.LogInformation("Found {DeviceCount} Entra AD devices to process", entraDevices.Count());

                var semaphore = new SemaphoreSlim(_intuneSettings.MaxConcurrentRequests, _intuneSettings.MaxConcurrentRequests);
                var tasks = new List<Task>();

                foreach (var entraDevice in entraDevices)
                {
                    tasks.Add(ProcessDeviceAsync(entraDevice, semaphore));
                }

                await Task.WhenAll(tasks);
                processedCount = tasks.Count;

                _logger.LogInformation("Completed processing {ProcessedCount} devices for Intune-based extension attributes", processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Intune-based extension attributes: {Error}", ex.Message);
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
                        return;
                    }
                }

                _logger.LogDebug("Found corresponding Intune device: {IntuneDeviceName} (ID: {IntuneDeviceId})", 
                    intuneDevice.DeviceName, intuneDevice.Id);

                // Process each extension attribute mapping
                foreach (var mapping in _intuneSettings.IntuneExtensionAttributeMappings)
                {
                    await ProcessExtensionAttributeMapping(entraDevice, intuneDevice, mapping);
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

        private async Task ProcessExtensionAttributeMapping(Device entraDevice, ManagedDevice intuneDevice, IntuneExtensionAttributeMapping mapping)
        {
            try
            {
                _logger.LogDebug("Processing mapping: {Mapping}", mapping.ToString());

                // Get the current value from Entra AD
                var currentValue = await _entraADHelper.GetExtensionAttribute(entraDevice.DeviceId!, mapping.ExtensionAttribute);
                
                // Get the value from Intune device
                var intuneValue = await GetIntuneValue(intuneDevice, mapping);
                
                if (string.IsNullOrEmpty(intuneValue))
                {
                    intuneValue = mapping.DefaultValue;
                    _logger.LogDebug("Using default value for {ExtensionAttribute}: {DefaultValue}", 
                        mapping.ExtensionAttribute, mapping.DefaultValue);
                }

                // Apply regex if specified
                if (!string.IsNullOrEmpty(mapping.Regex) && !string.IsNullOrEmpty(intuneValue))
                {
                    try
                    {
                        var regex = new Regex(mapping.Regex);
                        var match = regex.Match(intuneValue);
                        
                        if (match.Success)
                        {
                            // Use the first capture group if available, otherwise the whole match
                            intuneValue = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                            _logger.LogDebug("Regex applied successfully. Original: {Original}, Extracted: {Extracted}", 
                                intuneValue, match.Value);
                        }
                        else
                        {
                            _logger.LogWarning("Regex pattern '{Pattern}' did not match value '{Value}' for device {DeviceName}", 
                                mapping.Regex, intuneValue, entraDevice.DisplayName);
                            intuneValue = mapping.DefaultValue; // Use default if regex doesn't match
                        }
                    }
                    catch (Exception regexEx)
                    {
                        _logger.LogError(regexEx, "Error applying regex pattern '{Pattern}' to value '{Value}': {Error}", 
                            mapping.Regex, intuneValue, regexEx.Message);
                        intuneValue = mapping.DefaultValue;
                    }
                }

                // Check if the value needs to be updated
                if (!string.Equals(currentValue, intuneValue, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Updating {ExtensionAttribute} for device {DeviceName}: '{OldValue}' -> '{NewValue}'",
                        mapping.ExtensionAttribute, entraDevice.DisplayName, currentValue ?? "null", intuneValue ?? "null");

                    // Update the extension attribute
                    var result = await _entraADHelper.SetExtensionAttributeValue(entraDevice.DeviceId!, mapping.ExtensionAttribute, intuneValue ?? string.Empty);
                    
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

        private async Task<string?> GetIntuneValue(ManagedDevice intuneDevice, IntuneExtensionAttributeMapping mapping)
        {
            try
            {
                string? value = null;

                if (mapping.UseHardwareInfo && !string.IsNullOrEmpty(intuneDevice.Id))
                {
                    // Get hardware information
                    var hardwareInfo = await _intuneHelper.GetDeviceHardwareInformation(intuneDevice.Id);
                    if (hardwareInfo.ContainsKey(mapping.IntuneDeviceProperty.ToLowerInvariant()))
                    {
                        value = hardwareInfo[mapping.IntuneDeviceProperty.ToLowerInvariant()];
                    }
                }
                else
                {
                    // Get value from the managed device object
                    value = ExtractDeviceValue(intuneDevice, mapping.IntuneDeviceProperty, mapping.PropertyPath);
                }

                _logger.LogDebug("Extracted Intune value for property {Property}: {Value}", 
                    mapping.IntuneDeviceProperty, value ?? "null");

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Intune value for property {Property}: {Error}", 
                    mapping.IntuneDeviceProperty, ex.Message);
                return null;
            }
        }

        private string? ExtractDeviceValue(ManagedDevice device, string propertyName, string? propertyPath)
        {
            try
            {
                // Handle nested property paths like "hardwareInformation.manufacturer"
                if (!string.IsNullOrEmpty(propertyPath) && propertyPath.Contains('.'))
                {
                    // This would require more complex property navigation
                    _logger.LogDebug("Complex property path not yet implemented: {PropertyPath}", propertyPath);
                    return null;
                }

                // Direct property mapping
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
                _logger.LogError(ex, "Error extracting device value for property {PropertyName}: {Error}", propertyName, ex.Message);
                return null;
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