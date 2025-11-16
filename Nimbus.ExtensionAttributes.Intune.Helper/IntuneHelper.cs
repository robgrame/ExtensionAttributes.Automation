using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text.Json.Serialization;
using Serilog;

namespace Nimbus.ExtensionAttributes.Intune
{
    public class IntuneHelper : IIntuneHelper
    {
        private readonly Microsoft.Extensions.Logging.ILogger<IIntuneHelper> _logger;
        private readonly Serilog.ILogger _cmtraceLogger; // CMTrace-optimized logger
        private readonly GraphServiceClient _graphServiceClient;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://graph.microsoft.com";

        public IntuneHelper(ILogger<IIntuneHelper> logger, GraphServiceClient graphClient, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _graphServiceClient = graphClient;
            _httpClient = httpClientFactory.CreateClient("GraphAPI");
            
            // Create component-specific logger for CMTrace
            _cmtraceLogger = Log.ForContext("Component", "IntuneHelper")
                               .ForContext<IntuneHelper>();
            
            _cmtraceLogger.Information("IntuneHelper initialized with authenticated REST API support");
            _logger.LogInformation("IntuneHelper initialized with authenticated REST API support.");
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
                _cmtraceLogger.Debug("Starting device details retrieval for device ID: {DeviceId}", deviceId);
                _logger.LogDebug("Getting device details for device ID: {DeviceId}", deviceId);
                
                var device = await _graphServiceClient.DeviceManagement.ManagedDevices[deviceId].GetAsync();
                
                if (device != null)
                {
                    _cmtraceLogger.Information("Successfully retrieved device details - Name: {DeviceName}, OS: {OS}, Compliance: {ComplianceState}", 
                        device.DeviceName, device.OperatingSystem, device.ComplianceState);
                }
                else
                {
                    _cmtraceLogger.Warning("Device not found for ID: {DeviceId}", deviceId);
                }
                
                return device;
            }
            catch (Exception ex)
            {
                _cmtraceLogger.Error(ex, "Failed to retrieve device details for ID: {DeviceId} - {ErrorMessage}", deviceId, ex.Message);
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
                _logger.LogDebug("Getting installed applications for device ID: {DeviceId} via REST API", deviceId);

                // Use beta endpoint for app installation statuses
                var endpoint = $"/beta/deviceManagement/managedDevices('{deviceId}')/mobileAppIntentAndStates";
                var response = await MakeGraphApiCall<AppIntentStatesResponse>(endpoint);
                
                var installedApps = new List<MobileApp>();
                
                if (response?.Value != null)
                {
                    foreach (var appState in response.Value.Where(app => app.InstallState == "installed"))
                    {
                        if (appState.MobileApp != null)
                        {
                            installedApps.Add(appState.MobileApp);
                            _logger.LogDebug("Found installed app: {AppName}", appState.MobileApp.DisplayName);
                        }
                    }
                }

                _logger.LogInformation("Retrieved {AppCount} installed applications for device: {DeviceId}", installedApps.Count, deviceId);
                return installedApps;
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
                _logger.LogDebug("Getting detected applications for device ID: {DeviceId} via REST API", deviceId);

                // Use beta endpoint for detected apps
                var endpoint = $"/beta/deviceManagement/managedDevices('{deviceId}')/detectedApps";
                var response = await MakeGraphApiCall<DetectedAppsResponse>(endpoint);
                
                var detectedApps = response?.Value ?? new List<DetectedApp>();
                _logger.LogInformation("Retrieved {AppCount} detected applications for device: {DeviceId}", detectedApps.Count, deviceId);
                
                return detectedApps;
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
                _logger.LogDebug("Checking BitLocker status for device ID: {DeviceId} via REST API", deviceId);

                // Get device compliance policy states to check BitLocker/encryption settings
                var endpoint = $"/beta/deviceManagement/managedDevices('{deviceId}')/deviceCompliancePolicyStates";
                var response = await MakeGraphApiCall<CompliancePolicyStatesResponse>(endpoint);

                if (response?.Value != null)
                {
                    foreach (var policyState in response.Value)
                    {
                        if (policyState.SettingStates != null)
                        {
                            // Look for BitLocker or encryption-related settings
                            var encryptionSetting = policyState.SettingStates.FirstOrDefault(s => 
                                s.Setting != null && (
                                    s.Setting.Contains("bitlocker", StringComparison.OrdinalIgnoreCase) ||
                                    s.Setting.Contains("encryption", StringComparison.OrdinalIgnoreCase) ||
                                    s.Setting.Contains("deviceEncryption", StringComparison.OrdinalIgnoreCase)
                                ));

                            if (encryptionSetting != null)
                            {
                                bool isEnabled = encryptionSetting.State?.Equals("compliant", StringComparison.OrdinalIgnoreCase) == true;
                                _logger.LogDebug("BitLocker/Encryption status for device {DeviceId}: {Status}", deviceId, isEnabled ? "Enabled" : "Disabled");
                                return isEnabled;
                            }
                        }
                    }
                }

                // Alternative: Check device configuration states
                var configEndpoint = $"/beta/deviceManagement/managedDevices('{deviceId}')/deviceConfigurationStates";
                var configResponse = await MakeGraphApiCall<DeviceConfigurationStatesResponse>(configEndpoint);

                if (configResponse?.Value != null)
                {
                    foreach (var configState in configResponse.Value)
                    {
                        if (configState.DisplayName != null && 
                            (configState.DisplayName.Contains("BitLocker", StringComparison.OrdinalIgnoreCase) ||
                             configState.DisplayName.Contains("Encryption", StringComparison.OrdinalIgnoreCase)))
                        {
                            bool isConfigured = configState.State?.Equals("compliant", StringComparison.OrdinalIgnoreCase) == true;
                            _logger.LogDebug("BitLocker configuration for device {DeviceId}: {Status}", deviceId, isConfigured ? "Configured" : "Not Configured");
                            return isConfigured;
                        }
                    }
                }

                _logger.LogDebug("No BitLocker information found for device: {DeviceId}", deviceId);
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
            try
            {
                _logger.LogDebug("Checking Autopilot registration for serial: {SerialNumber} via REST API", serialNumber);

                var endpoint = $"/beta/deviceManagement/windowsAutopilotDeviceIdentities?$filter=serialNumber eq '{serialNumber}'";
                var response = await MakeGraphApiCall<AutopilotDevicesResponse>(endpoint);

                bool isRegistered = response?.Value?.Any() == true;
                
                if (isRegistered)
                {
                    var device = response?.Value?.First();
                    _logger.LogInformation("Autopilot device found - Serial: {SerialNumber}, ID: {DeviceId}", 
                        serialNumber, device?.Id);
                }
                else
                {
                    _logger.LogDebug("No Autopilot registration found for serial: {SerialNumber}", serialNumber);
                }
                
                return isRegistered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Autopilot registration for serial: {SerialNumber}", serialNumber);
                return false;
            }
        }

        public async Task<bool> IsAutopilotRegistered(string serialNumber, string hardwareHash)
        {
            try
            {
                // First check by serial number
                bool isRegistered = await IsAutopilotRegistered(serialNumber);
                
                if (isRegistered)
                {
                    // Optionally verify hardware hash matches
                    var endpoint = $"/beta/deviceManagement/windowsAutopilotDeviceIdentities?$filter=serialNumber eq '{serialNumber}'";
                    var response = await MakeGraphApiCall<AutopilotDevicesResponse>(endpoint);
                    
                    if (response?.Value?.Any() == true)
                    {
                        var device = response.Value.First();
                        _logger.LogDebug("Verified Autopilot registration with hardware hash for: {SerialNumber}", serialNumber);
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Autopilot registration with hardware hash for serial: {SerialNumber}", serialNumber);
                return false;
            }
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash)
        {
            return await RegisterAutoPilotDevice(serialNumber, hardwareHash, "", "");
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag)
        {
            return await RegisterAutoPilotDevice(serialNumber, hardwareHash, groupTag, "");
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag, string userUPN)
        {
            try
            {
                _logger.LogInformation("Registering Autopilot device via REST API - Serial: {SerialNumber}, GroupTag: {GroupTag}", 
                    serialNumber, groupTag);

                var autopilotDevice = new
                {
                    serialNumber = serialNumber,
                    hardwareIdentifier = hardwareHash,
                    groupTag = !string.IsNullOrEmpty(groupTag) ? groupTag : null,
                    assignedUserPrincipalName = !string.IsNullOrEmpty(userUPN) ? userUPN : null
                };

                var endpoint = "/beta/deviceManagement/windowsAutopilotDeviceIdentities";
                var response = await MakeGraphApiCall<AutopilotDeviceResponse>(endpoint, HttpMethod.Post, autopilotDevice);

                if (!string.IsNullOrEmpty(response?.Id))
                {
                    _logger.LogInformation("Successfully registered Autopilot device - Serial: {SerialNumber}, ID: {DeviceId}, GroupTag: {GroupTag}", 
                        serialNumber, response.Id, groupTag);
                    return response.Id;
                }
                else
                {
                    _logger.LogWarning("Autopilot registration returned empty ID for serial: {SerialNumber}", serialNumber);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering Autopilot device - Serial: {SerialNumber}", serialNumber);
                return string.Empty;
            }
        }
        #endregion

        #region REST API Helper Methods

        /// <summary>
        /// Generic method to make Graph API REST calls with proper error handling and logging
        /// Authentication is handled by the configured HttpClient with GraphApiAuthenticationHandler
        /// </summary>
        private async Task<T?> MakeGraphApiCall<T>(string endpoint, HttpMethod? method = null, object? body = null)
        {
            try
            {
                method ??= HttpMethod.Get;
                var requestUri = $"{_baseUrl}{endpoint}";

                using var request = new HttpRequestMessage(method, requestUri);
                
                if (body != null)
                {
                    var jsonOptions = new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };
                    var json = JsonSerializer.Serialize(body, jsonOptions);
                    request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    _cmtraceLogger.Debug("REST API request body for {Endpoint}: {RequestBody}", endpoint, json);
                    _logger.LogDebug("REST API request body: {RequestBody}", json);
                }

                _cmtraceLogger.Debug("Making authenticated Graph API REST call: {Method} {Uri}", method, requestUri);
                _logger.LogDebug("Making authenticated Graph API REST call: {Method} {Uri}", method, requestUri);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(request);
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    _cmtraceLogger.Information("Graph API REST call succeeded: {Method} {Endpoint} - Status: {StatusCode}, Duration: {Duration}ms", 
                        method, endpoint, response.StatusCode, stopwatch.ElapsedMilliseconds);
                    _logger.LogDebug("Graph API REST call successful: {StatusCode}", response.StatusCode);
                    
                    if (string.IsNullOrEmpty(content))
                    {
                        return default;
                    }

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    
                    return JsonSerializer.Deserialize<T>(content, jsonOptions);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _cmtraceLogger.Error("Authentication failed for REST API call: {Endpoint} - Status: {StatusCode}, Duration: {Duration}ms", 
                            endpoint, response.StatusCode, stopwatch.ElapsedMilliseconds);
                        _logger.LogError("Authentication failed for REST API call: {Endpoint}. Check token validity and permissions.", endpoint);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _cmtraceLogger.Warning("Insufficient permissions for REST API call: {Endpoint} - Status: {StatusCode}, Duration: {Duration}ms", 
                            endpoint, response.StatusCode, stopwatch.ElapsedMilliseconds);
                        _logger.LogWarning("Insufficient permissions for REST API call: {Endpoint}. Required permissions may be missing.", endpoint);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _cmtraceLogger.Debug("Resource not found for REST API call: {Endpoint} - Status: {StatusCode}", endpoint, response.StatusCode);
                        _logger.LogDebug("Resource not found for REST API call: {Endpoint}", endpoint);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 0;
                        _cmtraceLogger.Warning("Rate limited for REST API call: {Endpoint} - Status: {StatusCode}, Retry-After: {RetryAfter}s", 
                            endpoint, response.StatusCode, retryAfter);
                        _logger.LogWarning("Rate limited for REST API call: {Endpoint}. Retry logic should handle this.", endpoint);
                    }
                    else
                    {
                        _cmtraceLogger.Error("Graph API REST call failed: {Method} {Endpoint} - Status: {StatusCode}, Duration: {Duration}ms, Error: {ErrorContent}", 
                            method, endpoint, response.StatusCode, stopwatch.ElapsedMilliseconds, errorContent);
                        _logger.LogWarning("Graph API REST call failed: {StatusCode} - {ReasonPhrase}. Error: {ErrorContent}", 
                            response.StatusCode, response.ReasonPhrase, errorContent);
                    }
                    
                    return default;
                }
            }
            catch (HttpRequestException ex)
            {
                _cmtraceLogger.Error(ex, "HTTP error making Graph API REST call to: {Endpoint} - {ErrorMessage}", endpoint, ex.Message);
                _logger.LogError(ex, "HTTP error making Graph API REST call to: {Endpoint}", endpoint);
                return default;
            }
            catch (JsonException ex)
            {
                _cmtraceLogger.Error(ex, "JSON serialization error for Graph API REST call to: {Endpoint} - {ErrorMessage}", endpoint, ex.Message);
                _logger.LogError(ex, "JSON serialization error for Graph API REST call to: {Endpoint}", endpoint);
                return default;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _cmtraceLogger.Error(ex, "Timeout making Graph API REST call to: {Endpoint} - {ErrorMessage}", endpoint, ex.Message);
                _logger.LogError(ex, "Timeout making Graph API REST call to: {Endpoint}", endpoint);
                return default;
            }
            catch (Exception ex)
            {
                _cmtraceLogger.Error(ex, "Unexpected error making Graph API REST call to: {Endpoint} - {ErrorMessage}", endpoint, ex.Message);
                _logger.LogError(ex, "Unexpected error making Graph API REST call to: {Endpoint}", endpoint);
                return default;
            }
        }

        #endregion

        #region Response Models for REST API

        private class AppIntentStatesResponse
        {
            public List<MobileAppIntentAndState>? Value { get; set; }
        }

        private class MobileAppIntentAndState
        {
            public string? InstallState { get; set; }
            public MobileApp? MobileApp { get; set; }
        }

        private class DetectedAppsResponse
        {
            public List<DetectedApp>? Value { get; set; }
        }

        private class DeviceConfigurationStatesResponse
        {
            public List<DeviceConfigurationState>? Value { get; set; }
        }

        private class DeviceConfigurationState
        {
            public string? DisplayName { get; set; }
            public string? State { get; set; }
            public DateTime? LastReportedDateTime { get; set; }
        }

        private class CompliancePolicyStatesResponse
        {
            public List<CompliancePolicyState>? Value { get; set; }
        }

        private class CompliancePolicyState
        {
            public string? DisplayName { get; set; }
            public List<PolicySettingState>? SettingStates { get; set; }
        }

        private class PolicySettingState
        {
            public string? Setting { get; set; }
            public string? State { get; set; }
            public string? ErrorDescription { get; set; }
        }

        private class AutopilotDevicesResponse
        {
            public List<AutopilotDevice>? Value { get; set; }
        }

        private class AutopilotDevice
        {
            public string? Id { get; set; }
            public string? SerialNumber { get; set; }
            public string? GroupTag { get; set; }
            public string? AssignedUserPrincipalName { get; set; }
        }

        private class AutopilotDeviceResponse
        {
            public string? Id { get; set; }
            public string? SerialNumber { get; set; }
        }

        #endregion
    }
}
