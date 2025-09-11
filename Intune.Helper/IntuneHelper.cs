using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Azure.Automation.Intune
{
    public class IntuneHelper : IIntuneHelper
    {
        private readonly ILogger<IIntuneHelper> _logger;
        private readonly GraphServiceClient _graphServiceClient;

        public IntuneHelper(ILogger<IIntuneHelper> logger, GraphServiceClient graphClient)
        {
            _logger = logger;
            _graphServiceClient = graphClient;
        }

        #region Basic Device Information
        public async Task<ManagedDeviceCollectionResponse> GetIntuneDevices()
        {
            try
            {
                var managedDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync();
                if (managedDevices?.Value == null)
                {
                    return new ManagedDeviceCollectionResponse();
                }

                managedDevices.Value?.ForEach(device =>
                {
                    _logger.LogDebug("Device {DeviceName} with {DeviceID}", device.DeviceName, device.Id);
                });

                return managedDevices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Intune devices");
                return new ManagedDeviceCollectionResponse();
            }
        }

        public async Task<ManagedDevice?> GetDeviceDetails(string deviceId)
        {
            try
            {
                _logger.LogDebug("Getting device details for device ID: {DeviceId}", deviceId);
                var device = await _graphServiceClient.DeviceManagement.ManagedDevices[deviceId].GetAsync();
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device details for device ID: {DeviceId}", deviceId);
                return null;
            }
        }

        public async Task<ManagedDevice?> GetDeviceDetailsbyName(string deviceName)
        {
            try
            {
                _logger.LogDebug("Getting device details for device name: {DeviceName}", deviceName);
                var devices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"deviceName eq '{deviceName}'";
                });

                return devices?.Value?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device details for device name: {DeviceName}", deviceName);
                return null;
            }
        }

        public async Task<ManagedDevice?> GetDeviceDetailsByEntraDeviceId(string entraDeviceId)
        {
            try
            {
                _logger.LogDebug("Getting device details for Entra device ID: {EntraDeviceId}", entraDeviceId);
                var devices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"azureADDeviceId eq '{entraDeviceId}'";
                });

                return devices?.Value?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device details for Entra device ID: {EntraDeviceId}", entraDeviceId);
                return null;
            }
        }
        #endregion

        #region Hardware Information (using ManagedDevice properties)
        public async Task<Dictionary<string, string?>> GetDeviceHardwareInformation(string deviceId)
        {
            try
            {
                _logger.LogDebug("Getting hardware information for device ID: {DeviceId}", deviceId);
                var device = await GetDeviceDetails(deviceId);
                
                if (device == null)
                {
                    return new Dictionary<string, string?>();
                }

                // Extract hardware-related information from ManagedDevice properties
                return new Dictionary<string, string?>
                {
                    ["manufacturer"] = device.Manufacturer,
                    ["model"] = device.Model,
                    ["serialNumber"] = device.SerialNumber,
                    ["operatingSystem"] = device.OperatingSystem,
                    ["osVersion"] = device.OsVersion,
                    ["totalStorageSpaceInBytes"] = device.TotalStorageSpaceInBytes?.ToString(),
                    ["freeStorageSpaceInBytes"] = device.FreeStorageSpaceInBytes?.ToString(),
                    ["phoneNumber"] = device.PhoneNumber,
                    ["deviceName"] = device.DeviceName,
                    ["wiFiMacAddress"] = device.WiFiMacAddress,
                    ["imei"] = device.Imei,
                    ["meid"] = device.Meid,
                    ["subscriberCarrier"] = device.SubscriberCarrier
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hardware information for device ID: {DeviceId}", deviceId);
                return new Dictionary<string, string?>();
            }
        }

        public async Task<Dictionary<string, string?>> GetDeviceHardwareInformationByName(string deviceName)
        {
            var device = await GetDeviceDetailsbyName(deviceName);
            if (device?.Id != null)
            {
                return await GetDeviceHardwareInformation(device.Id);
            }
            return new Dictionary<string, string?>();
        }

        public async Task<Dictionary<string, string?>> GetDeviceHardwareInformationByEntraDeviceId(string entraDeviceId)
        {
            var device = await GetDeviceDetailsByEntraDeviceId(entraDeviceId);
            if (device?.Id != null)
            {
                return await GetDeviceHardwareInformation(device.Id);
            }
            return new Dictionary<string, string?>();
        }
        #endregion

        #region Software Information
        public async Task<IEnumerable<MobileApp>> GetInstalledApplications(string deviceId)
        {
            try
            {
                _logger.LogDebug("Getting installed applications for device ID: {DeviceId}", deviceId);
                // Note: This API might not be directly available in the current Microsoft Graph SDK
                // This is a placeholder implementation
                return new List<MobileApp>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting installed applications for device ID: {DeviceId}", deviceId);
                return new List<MobileApp>();
            }
        }

        public async Task<IEnumerable<DetectedApp>> GetDetectedApplications(string deviceId)
        {
            try
            {
                _logger.LogDebug("Getting detected applications for device ID: {DeviceId}", deviceId);
                // Note: DetectedApps API might not be directly available
                // This is a placeholder - you might need to use a different approach
                return new List<DetectedApp>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detected applications for device ID: {DeviceId}", deviceId);
                return new List<DetectedApp>();
            }
        }

        public async Task<string?> GetOperatingSystemVersion(string deviceId)
        {
            var device = await GetDeviceDetails(deviceId);
            return device?.OsVersion;
        }

        public async Task<string?> GetOperatingSystemBuild(string deviceId)
        {
            // Build number is typically part of the OS version string
            var osVersion = await GetOperatingSystemVersion(deviceId);
            if (!string.IsNullOrEmpty(osVersion))
            {
                // Try to extract build number from version string if possible
                var buildMatch = System.Text.RegularExpressions.Regex.Match(osVersion, @"(\d+\.\d+\.\d+)\.(\d+)");
                if (buildMatch.Success && buildMatch.Groups.Count > 2)
                {
                    return buildMatch.Groups[2].Value;
                }
            }
            return null;
        }
        #endregion

        #region Device Configuration
        public async Task<bool> IsBitLockerEnabled(string deviceId)
        {
            try
            {
                // This would require checking device compliance policies or configuration profiles
                // For now, return false as placeholder
                _logger.LogDebug("Checking BitLocker status for device ID: {DeviceId}", deviceId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking BitLocker status for device ID: {DeviceId}", deviceId);
                return false;
            }
        }

        public async Task<DateTime?> GetLastSyncDateTime(string deviceId)
        {
            var device = await GetDeviceDetails(deviceId);
            return device?.LastSyncDateTime?.DateTime;
        }

        public async Task<string?> GetDeviceOwnership(string deviceId)
        {
            var device = await GetDeviceDetails(deviceId);
            return device?.ManagedDeviceOwnerType?.ToString();
        }

        public async Task<string?> GetManagementAgent(string deviceId)
        {
            var device = await GetDeviceDetails(deviceId);
            return device?.ManagementAgent?.ToString();
        }
        #endregion

        #region Device Lookup Methods
        public async Task<ManagedDevice?> FindDeviceByComputerName(string computerName)
        {
            return await GetDeviceDetailsbyName(computerName);
        }

        public async Task<ManagedDevice?> FindDeviceBySerialNumber(string serialNumber)
        {
            try
            {
                _logger.LogDebug("Finding device by serial number: {SerialNumber}", serialNumber);
                var devices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"serialNumber eq '{serialNumber}'";
                });

                return devices?.Value?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding device by serial number: {SerialNumber}", serialNumber);
                return null;
            }
        }

        public async Task<IEnumerable<ManagedDevice>> FindDevicesByUser(string userPrincipalName)
        {
            try
            {
                _logger.LogDebug("Finding devices by user: {UserPrincipalName}", userPrincipalName);
                var devices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"userPrincipalName eq '{userPrincipalName}'";
                });

                return devices?.Value ?? new List<ManagedDevice>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding devices by user: {UserPrincipalName}", userPrincipalName);
                return new List<ManagedDevice>();
            }
        }
        #endregion

        #region Extension Attribute Support
        public async Task<string?> GetIntuneDeviceProperty(string deviceId, string propertyName)
        {
            try
            {
                _logger.LogDebug("Getting property {PropertyName} for device ID: {DeviceId}", propertyName, deviceId);
                
                var device = await GetDeviceDetails(deviceId);
                if (device == null)
                {
                    _logger.LogWarning("Device not found: {DeviceId}", deviceId);
                    return null;
                }

                return ExtractPropertyValue(device, propertyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property {PropertyName} for device ID: {DeviceId}", propertyName, deviceId);
                return null;
            }
        }

        public async Task<string?> GetIntuneDevicePropertyByComputerName(string computerName, string propertyName)
        {
            var device = await FindDeviceByComputerName(computerName);
            if (device?.Id != null)
            {
                return await GetIntuneDeviceProperty(device.Id, propertyName);
            }
            return null;
        }

        private string? ExtractPropertyValue(ManagedDevice device, string propertyName)
        {
            try
            {
                // Use reflection to get property values dynamically
                var deviceType = device.GetType();
                var property = deviceType.GetProperty(propertyName);
                
                if (property != null)
                {
                    var value = property.GetValue(device);
                    return value?.ToString();
                }

                // Handle special cases for nested properties
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
                    "totalstorage" => device.TotalStorageSpaceInBytes?.ToString(),
                    "freestorage" => device.FreeStorageSpaceInBytes?.ToString(),
                    "totalstoragegb" => FormatStorageSizeGB(device.TotalStorageSpaceInBytes),
                    "freestoragegb" => FormatStorageSizeGB(device.FreeStorageSpaceInBytes),
                    "phonenumber" => device.PhoneNumber,
                    "wifimacaddress" => device.WiFiMacAddress,
                    "imei" => device.Imei,
                    "meid" => device.Meid,
                    "subscribercarrier" => device.SubscriberCarrier,
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting property {PropertyName} from device", propertyName);
                return null;
            }
        }

        private string? FormatStorageSizeGB(long? bytes)
        {
            if (!bytes.HasValue || bytes.Value <= 0)
                return null;

            const double gb = 1024 * 1024 * 1024;
            return $"{bytes.Value / gb:F0}";
        }
        #endregion

        #region Count Methods
        public async Task<int> GetTotalAndroidDevices()
        {
            _logger.LogTrace("Getting total Android devices");

            var androidDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "operatingSystem eq 'Android'";
            });

            var totalAndroidDevices = androidDevices?.Value?.Count ?? 0;
            _logger.LogDebug("Total Android devices: {TotalAndroidDevices}", totalAndroidDevices);

            return totalAndroidDevices;
        }

        public async Task<int> GetTotalCompliantDevices()
        {
            _logger.LogTrace("Getting total compliant devices");

            var compliantDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'compliant'";
            });

            _logger.LogDebug("Total Compliant devices: {CompliantDevices}", compliantDevices?.Value?.Count ?? 0);

            return compliantDevices?.Value?.Count ?? 0;
        }

        public async Task<int> GetTotalNonCompliantDevices()
        {
            _logger.LogTrace("Getting total noncompliant devices");

            var nonCompliantDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'noncompliant'";
            });

            _logger.LogDebug("Total NonCompliant devices: {NonCompliantDevices}", nonCompliantDevices?.Value?.Count ?? 0);

            return nonCompliantDevices?.Value?.Count ?? 0;
        }

        public async Task<int> GetTotalCompliantWindowsDevices()
        {
            _logger.LogDebug("Getting total compliant Windows devices");

            var compliantWindowsDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'compliant' and operatingSystem eq 'Windows'";
            });

            return compliantWindowsDevices?.Value?.Count ?? 0;
        }

        public async Task<int> GetTotalNonCompliantWindowsDevices()
        {
            _logger.LogDebug("Getting total non-compliant Windows devices");

            var nonCompliantWindowsDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'noncompliant' and operatingSystem eq 'Windows'";
            });

            return nonCompliantWindowsDevices?.Value?.Count ?? 0;
        }

        public async Task<int> GetTotaliOSDevices()
        {
            var iOSDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "operatingSystem eq 'iOS'";
            });

            return iOSDevices?.Value?.Count ?? 0;
        }

        public async Task<int> GetTotalMacOSDevices()
        {
            var macOSDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "operatingSystem eq 'macOS'";
            });

            return macOSDevices?.Value?.Count ?? 0;
        }

        public async Task<int> GetTotalWindows365Devices()
        {
            // Windows 365 devices might be identified by specific properties
            // This is a placeholder implementation
            return 0;
        }

        public async Task<int> GetTotalWindowsDevices()
        {
            var windowsDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "operatingSystem eq 'Windows'";
            });

            return windowsDevices?.Value?.Count ?? 0;
        }
        #endregion

        #region Collection Methods
        public async Task<ManagedDeviceCollectionResponse> GetCompliantDevices()
        {
            var devices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'compliant'";
            });

            return devices ?? new ManagedDeviceCollectionResponse();
        }

        public async Task<ManagedDeviceCollectionResponse> GetNonCompliantDevices()
        {
            var devices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'noncompliant'";
            });

            return devices ?? new ManagedDeviceCollectionResponse();
        }

        public async Task<ManagedDeviceCollectionResponse> GetCompliantWindowsDevices()
        {
            var devices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'compliant' and operatingSystem eq 'Windows'";
            });

            return devices ?? new ManagedDeviceCollectionResponse();
        }

        public async Task<ManagedDeviceCollectionResponse> GetNonCompliantWindowsDevices()
        {
            var devices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'noncompliant' and operatingSystem eq 'Windows'";
            });

            return devices ?? new ManagedDeviceCollectionResponse();
        }

        public async Task<ComplianceState> GetDeviceComplianceState(string deviceId)
        {
            var device = await GetDeviceDetails(deviceId);
            return device?.ComplianceState ?? ComplianceState.Unknown;
        }

        public async Task<ComplianceState> GetDeviceComplianceStatebyName(string deviceName)
        {
            var device = await GetDeviceDetailsbyName(deviceName);
            return device?.ComplianceState ?? ComplianceState.Unknown;
        }
        #endregion

        #region Autopilot
        public async Task<bool> IsAutopilotRegistered(string serialNumber)
        {
            // Placeholder implementation
            return false;
        }

        public async Task<bool> IsAutopilotRegistered(string serialNumber, string hardwareHash)
        {
            // Placeholder implementation
            return false;
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash)
        {
            // Placeholder implementation
            return string.Empty;
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag)
        {
            // Placeholder implementation
            return string.Empty;
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag, string userUPN)
        {
            // Placeholder implementation
            return string.Empty;
        }
        #endregion
    }
}
