using Nimbus.ExtensionAttributes.EntraAD.Authentication;
using Azure.Core;
using Azure.Identity;
using Nimbus.ExtensionAttributes.EntraAD.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.Identity.Client;
using System.Net.Http;
using Nimbus.ExtensionAttributes.EntraAD.Config;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;



namespace Nimbus.ExtensionAttributes.EntraAD
{
    public class EntraADHelper : IEntraADHelper
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<IEntraADHelper> _logger;
        private readonly EntraADHelperSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly AuthenticationHandler _authenticationHandler;

        public EntraADHelper(ILogger<IEntraADHelper> logger, IHttpClientFactory httpClientFactory,IOptions<EntraADHelperSettings> settings,GraphServiceClient graphServiceClient, AuthenticationHandler authenticationHandler)
        {
            _graphServiceClient = graphServiceClient;
            _httpClient = httpClientFactory.CreateClient("GraphAPI") ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _settings = settings.Value;
            _logger = logger;
            _authenticationHandler = authenticationHandler;
        }


        public async Task<IEnumerable<User>> GetUsers()
        {
            int pageSize = 5;
            return await this.GetUsers(pageSize);
        }

        public async Task<IEnumerable<User>> GetUsers(int pagingNum)
        {

            _logger.LogTrace("GetUsers Called");

            List<User> userList = new List<User>();

            try
            {
                var users = await _graphServiceClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Top = pagingNum;
                        //requestConfiguration.QueryParameters.Select =
                        //    new[] { "displayName", "givenName", "userPrincipalName", "id", "mail", "operatingSystemVersion" };
                    });

                // Loop through the users and log the display name and user principal name paging through the results
                while (users?.Value != null)
                {
                    foreach (var user in users.Value)
                    {
                        userList.Add(user);
                        _logger.LogInformation("User {DisplayName} with {UserPrincipalName}", user.DisplayName, user.UserPrincipalName);
                    }

                    // If OdataNextLink has a value, there is another page
                    if (!string.IsNullOrEmpty(users.OdataNextLink))
                    {
                        // Pass the OdataNextLink to the WithUrl method
                        // to request the next page
                        users = await _graphServiceClient.Users
                            .WithUrl(users.OdataNextLink)
                            .GetAsync();

                    }
                    else
                    {
                        // No more results, exit loop
                        break;
                    }

                }


                return userList;

            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return Enumerable.Empty<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return Enumerable.Empty<User>();
            }

        }

        public async Task<User?> GetUser(string userId)
        {
            try
            {
                var user = await _graphServiceClient.Users[userId].GetAsync();
                return user;
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        public async Task<Device?> GetDeviceAsync(string deviceId)
        {
            try
            {
                var device = await _graphServiceClient.Devices[deviceId].GetAsync();
                return device;
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"An error occurred retrieving Device directory object: {ex.Message}");
                return null;
            }
        }

        public async Task<Device?> GetDeviceByNameAsync(string deviceName)
        {
            try
            {
                _logger.LogTrace("Get the Access Token");
                var token = await _authenticationHandler.GetAccessTokenAsync();
                _logger.LogTrace("Access Token retrieved");

                _logger.LogTrace("Building the request to the Microsoft Graph API (beta)");
                var request = new HttpRequestMessage(HttpMethod.Get, 
                    $"https://graph.microsoft.com/beta/devices?$filter=displayName eq '{deviceName}'&$select={string.Join(",", _settings.AttributesToLoad ?? new[] {"id","deviceId","accountEnabled","approximateLastSignInDateTime","displayName","trustType"})}");
                _logger.LogTrace("Request built");

                _logger.LogTrace("Adding the access token to the request headers");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                request.Headers.Add("ConsistencyLevel", "eventual");
                _logger.LogTrace("Access token added to the request headers");

                _logger.LogTrace("Sending the request");
                var response = await _httpClient.SendAsync(request);
                _logger.LogTrace("Request sent");

                _logger.LogTrace("Checking the response status code");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogTrace("Response status code is success");
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogTrace("Response content retrieved");
                    _logger.LogTrace("Deserializing the response content");
                    var result = System.Text.Json.JsonSerializer.Deserialize<Microsoft.Graph.Models.DeviceCollectionResponse>(content);
                    _logger.LogTrace("Response content deserialized");

                    if (result?.Value == null || result.Value.Count == 0)
                    {
                        _logger.LogWarning("No devices found with name {DeviceName}", deviceName);
                        return null;
                    }

                    // If multiple devices found, prioritize ServerAd devices (devices synced from AD)
                    if (result.Value.Count > 1)
                    {
                        _logger.LogInformation("Found {Count} devices with name {DeviceName}. Filtering by trustType.", result.Value.Count, deviceName);
                        
                        // First, try to find a ServerAd device (synced from AD)
                        var serverAdDevice = result.Value.FirstOrDefault(d => 
                            string.Equals(d.TrustType, "ServerAd", StringComparison.OrdinalIgnoreCase));
                        
                        if (serverAdDevice != null)
                        {
                            _logger.LogInformation("Selected ServerAd device for {DeviceName}: DeviceId={DeviceId}, TrustType={TrustType}", 
                                deviceName, serverAdDevice.DeviceId, serverAdDevice.TrustType);
                            return serverAdDevice;
                        }
                        
                        // If no ServerAd device, log all devices and return the first one
                        _logger.LogWarning("No ServerAd device found for {DeviceName}. Available devices:", deviceName);
                        foreach (var device in result.Value)
                        {
                            _logger.LogWarning("  - DeviceId: {DeviceId}, TrustType: {TrustType}", device.DeviceId, device.TrustType);
                        }
                    }

                    return result.Value.FirstOrDefault();
                }
                else
                {
                    _logger.LogError("Error retrieving device {DeviceName}. Status code: {StatusCode}", deviceName, response.StatusCode);
                    return null;
                }
            }
            catch (AuthenticationFailedException ex)
            {
                _logger.LogError(ex.Message, "Authentication failed while retrieving device {DeviceName}", deviceName);
                throw new Exception("Authentication failed while retrieving device", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device {DeviceName}", deviceName);
                return null;
            }
        }

        public async Task<string?> GetExtensionAttribute(string deviceId, string extensionAttribute)
        {
            _logger.LogTrace("GetExtensionAttribute Called");

            string lowerCasedExtensionAttribute = extensionAttribute.ToLowerInvariant();
            string correctCasingAttribute = char.ToUpperInvariant(lowerCasedExtensionAttribute[0]) + lowerCasedExtensionAttribute.Substring(1);
            correctCasingAttribute = correctCasingAttribute.Substring(0, 9) + char.ToUpperInvariant(extensionAttribute[9]) + extensionAttribute.Substring(10);
            _logger.LogTrace("Extension attribute name {extensionAttribute} converted to {correctCasingAttribute}", extensionAttribute, correctCasingAttribute);

            try
            {
                _logger.LogTrace("Get the Access Token");
                var token = await _authenticationHandler.GetAccessTokenAsync();
                _logger.LogTrace("Access Token retrieved");

                _logger.LogTrace("Building the request to the Microsoft Graph API");
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/beta/devices/{deviceId}");
                _logger.LogTrace("Request built");

                _logger.LogTrace("Adding the access token to the request headers");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                request.Headers.Add("ConsistencyLevel", "eventual");
                _logger.LogTrace("Access token added to the request headers");

                _logger.LogTrace("Sending the request");
                var response = await _httpClient.SendAsync(request);
                _logger.LogTrace("Request sent");

                _logger.LogTrace("Checking the response status code");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogTrace("Response status code is success");
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogTrace("Response content retrieved");
                    _logger.LogTrace("Deserializing the response content");
                    var device = System.Text.Json.JsonSerializer.Deserialize<EntraADDevice>(content);
                    _logger.LogTrace("Response content deserialized");
                    _logger.LogTrace("Returning the {extensionAttribute} value for the device {id}", extensionAttribute, deviceId);

                    if (device?.ExtensionAttributes == null)
                    {
                        _logger.LogError("Extension attributes are null for device {DeviceId}", deviceId);
                        return null;
                    }
                    var extensionAttributeValue = device.ExtensionAttributes.GetType().GetProperty(correctCasingAttribute)?.GetValue(device.ExtensionAttributes, null);
                    if (extensionAttributeValue == null)
                    {
                        _logger.LogWarning("Extension attribute {ExtensionAttribute} is null for device {DeviceId}", extensionAttribute, deviceId);
                        return null;
                    }
                    _logger.LogTrace("Extension attribute {extensionAttribute} for device {deviceId} has value {extensionAttributeValue}", extensionAttribute, deviceId, extensionAttributeValue.ToString());
                    return extensionAttributeValue.ToString();
                }
                else
                {
                    _logger.LogError("Error retrieving extension attribute {ExtensionAttribute} for device {DeviceId}", extensionAttribute, deviceId);
                    return null;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error retrieving extension attribute {ExtensionAttribute} for device {DeviceId}", extensionAttribute, deviceId);
                return null;
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Error retrieving extension attribute {ExtensionAttribute} for device {DeviceId}", extensionAttribute, deviceId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving extension attribute {ExtensionAttribute} for device {DeviceId}", extensionAttribute, deviceId);
                return null;
            }
        }

        public async Task<List<string?>> GetExtensionAttributes(string deviceId, List<string> extensionAttributes)
        {
            _logger.LogTrace("GetExtensionAttributes Called");

            List<string> correctCasingAttributes = new List<string>();

            foreach (string extensionAttribute in extensionAttributes)
            {
                string lowerCasedExtensionAttribute = extensionAttribute.ToLowerInvariant();
                string correctCasingAttribute = char.ToUpperInvariant(lowerCasedExtensionAttribute[0]) + lowerCasedExtensionAttribute.Substring(1);
                correctCasingAttribute = correctCasingAttribute.Substring(0, 9) + char.ToUpperInvariant(extensionAttribute[9]) + extensionAttribute.Substring(10);
                _logger.LogTrace("Extension attribute name {extensionAttribute} converted to {correctCasingAttribute}", extensionAttribute, correctCasingAttribute);
                correctCasingAttributes.Add(correctCasingAttribute);
            }

            try
            {
                _logger.LogTrace("Get the Access Token");
                var token = await _authenticationHandler.GetAccessTokenAsync();
                _logger.LogTrace("Access Token retrieved");

                _logger.LogTrace("Building the request to the Microsoft Graph API");
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/beta/devices/{deviceId}");
                _logger.LogTrace("Request built");

                _logger.LogTrace("Adding the access token to the request headers");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                request.Headers.Add("ConsistencyLevel", "eventual");
                _logger.LogTrace("Access token added to the request headers");

                _logger.LogTrace("Sending the request");
                var response = await _httpClient.SendAsync(request);
                _logger.LogTrace("Request sent");

                _logger.LogTrace("Checking the response status code");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogTrace("Response status code is success");
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogTrace("Response content retrieved");
                    _logger.LogTrace("Deserializing the response content");
                    var device = System.Text.Json.JsonSerializer.Deserialize<EntraADDevice>(content);
                    _logger.LogTrace("Response content deserialized");
                    _logger.LogTrace("Returning the extension attributes for the device {id}", deviceId);

                    if (device?.ExtensionAttributes == null)
                    {
                        _logger.LogError("Extension attributes are null for device {DeviceId}", deviceId);
                        return new List<string?>();
                    }

                    var extensionAttributeValuesList = device.ExtensionAttributes.GetType().GetProperties()
                        .Where(p => correctCasingAttributes.Contains(p.Name))
                        .Select(p => p.GetValue(device.ExtensionAttributes, null)?.ToString())
                        .ToList();

                    return extensionAttributeValuesList;
                }
                else
                {
                    _logger.LogError("Error retrieving extension attributes for device {DeviceId}", deviceId);
                    return new List<string?>();
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error retrieving extension attributes for device {DeviceId}", deviceId);
                return new List<string?>();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Error retrieving extension attributes for device {DeviceId}", deviceId);
                return new List<string?>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving extension attributes for device {DeviceId}", deviceId);
                return new List<string?>();
            }
        }


        public async Task<string?> SetExtensionAttributeValue(string deviceId, string extensionAttributeName, string extensionAttributeValue)
        {
            _logger.LogTrace("SetExtensionAttributeValue Called");

            var lowerCasedExtensionAttribute = extensionAttributeName.ToLowerInvariant();
            var correctCasingAttribute = lowerCasedExtensionAttribute.Substring(0, 9) + char.ToUpperInvariant(extensionAttributeName[9]) + extensionAttributeName.Substring(10);
            _logger.LogTrace("Correct casing for extension attribute {extensionAttribute} is {correctCasingAttribute}", extensionAttributeName, correctCasingAttribute);

            try
            {
                _logger.LogTrace("Get the Access Token");
                var token = await _authenticationHandler.GetAccessTokenAsync();
                _logger.LogTrace("Access Token retrieved");

                _logger.LogTrace("Building the request to the Microsoft Graph API");
                var request = new HttpRequestMessage(HttpMethod.Patch, $"https://graph.microsoft.com/beta/devices/{deviceId}");
                _logger.LogTrace("Request {request} built", request.RequestUri);

                _logger.LogTrace("Adding the access token to the request headers");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                request.Headers.Add("ConsistencyLevel", "eventual");
                _logger.LogTrace("Access token added to the request headers");

                _logger.LogTrace("Adding the request body");
                request.Content = new StringContent($"{{\"extensionAttributes\":{{\"{extensionAttributeName}\":\"{extensionAttributeValue}\"}}}}", System.Text.Encoding.UTF8, "application/json");
                _logger.LogTrace("Request body added");

                _logger.LogTrace("Sending the request");
                var response = await _httpClient.SendAsync(request);
                _logger.LogTrace("Request sent");

                _logger.LogTrace("Checking the response status code");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogTrace("Response status code is success");
                    return extensionAttributeValue;
                }
                else
                {
                    _logger.LogError("Error setting extension attribute {ExtensionAttribute} for device {DeviceId}", extensionAttributeName, deviceId);
                    return null;
                }
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Error setting extension attribute {ExtensionAttribute} for device {DeviceId}", extensionAttributeName, deviceId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting extension attribute {ExtensionAttribute} for device {DeviceId}", extensionAttributeName, deviceId);
                return null;
            }
        }



        public async Task<IEnumerable<Device>> GetDevicesAssignedToUser(string userUPN)
        {
            _logger.LogTrace("GetDevicesAssignedToUser Called");

            List<Device> deviceList = new List<Device>();


            try
            {
                var devices = await _graphServiceClient.Users[userUPN].RegisteredDevices
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Top = 5;
                        //requestConfiguration.QueryParameters.Select =
                        //    new[] { "displayName", "deviceId", "id", "physicalIds", "trustType", "operatingSystemVersion" };
                    });

                while (devices?.Value != null)
                {
                    foreach (var device in devices.Value)
                    {
                        deviceList.Add((Device)device);
                        _logger.LogTrace("Device with {DeviceID}", device.Id);
                    }

                    // If OdataNextLink has a value, there is another page
                    if (!string.IsNullOrEmpty(devices.OdataNextLink))
                    {
                        // Pass the OdataNextLink to the WithUrl method
                        // to request the next page
                        devices = await _graphServiceClient.Users[userUPN].RegisteredDevices
                            .WithUrl(devices.OdataNextLink)
                            .GetAsync();

                    }
                    else
                    {
                        // No more results, exit loop
                        break;
                    }

                }

                return deviceList;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices for user {UserUPN}", userUPN);
                return Enumerable.Empty<Device>();
            }
        }


        private async Task<bool> DeleteDevice(string deviceId)
        {
            try
            {
                await _graphServiceClient.Devices[deviceId].DeleteAsync();
                return true;
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Error deleting device {DeviceId}", deviceId);
                return false;
            }
        }


        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <returns>The list of devices.</returns>
        public async Task<IEnumerable<Device>> GetDevices()
        {
            return await this.GetDevices(0, null, null, null);
        }

        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <param name="pagingNum">The number of items to return in a single request.</param>
        /// <remarks>Use the pagingNum parameter to specify the number of items to return in a single request.</remarks>
        /// <returns>The list of devices.</returns>
        public async Task<IEnumerable<Device>> GetDevices(int pagingNum)
        {
            return await this.GetDevices(pagingNum, null, null, null);
        }

        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <param name="filter">The filter to apply to the request.</param>
        /// <remarks>Use the filter parameter to specify the filter to apply to the request.</remarks>
        /// <returns>The list of devices.</returns>
        public async Task<IEnumerable<Device>> GetDevices(string filter)
        {
            return await this.GetDevices(0, filter, null, null);
        }

        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <param name="pagingNum">The number of items to return in a single request.</param>
        /// <param name="filter">The filter to apply to the request.</param>
        /// <remarks>Use the filter parameter to specify the filter to apply to the request.</remarks>
        /// <returns>The list of devices.</returns>
        public async Task<IEnumerable<Device>> GetDevices(int pagingNum, string filter)
        {
            return await this.GetDevices(pagingNum, filter, null, null);
        }

        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <param name="filter">The filter to apply to the request.</param>
        /// <param name="header">The header to apply to the request.</param>
        /// <remarks>Use the filter parameter to specify the filter to apply to the request.</remarks>
        /// <returns>The list of devices.</returns>
        public async Task<IEnumerable<Device>> GetDevices(string filter, string header)
        {
            return await this.GetDevices(0, filter, header, null);
        }

        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <param name="pagingNum">The number of items to return in a single request.</param>
        /// <param name="filter">The filter to apply to the request.</param>
        /// <param name="header">The header to apply to the request.</param>
        /// <remarks>Use the filter parameter to specify the filter to apply to the request.</remarks>
        /// <returns>The list of devices.</returns>
        public async Task<IEnumerable<Device>> GetDevices(int pagingNum, string filter, string header)
        {
            return await this.GetDevices(pagingNum, filter, header, null);
        }

        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <param name="filter">The filter to apply to the request.</param>
        /// <param name="attributes">The attribute list to return in the response.</param>
        /// <returns>The list of devices.</returns>
        public async Task<IEnumerable<Device>> GetDevices(string filter, string[] attributes)
        {
            return await this.GetDevices(0, filter, null, attributes);
        }

        /// <summary>
        /// Gets the list of all devices
        /// </summary>
        /// <param name="pagingNum">The number of items to return in a single request.</param> 
        /// <param name="filter">The filter to apply to the request.</param>
        /// <param name="header" >The header to apply to the request.</param>
        /// <param name="attributes">The attribute list to return in the response.</param>
        /// <returns>The list of devices.</returns>
        public async Task<IEnumerable<Device>> GetDevices(int? pagingNum, string? filter, string? header, string[]? attributes)
        {
            _logger.LogTrace("GetDevices Called");

            List<Device> deviceList = new List<Device>();

            try
            {
                var devices = await _graphServiceClient.Devices
                    .GetAsync(requestConfiguration =>
                    {
                        if (pagingNum.HasValue && pagingNum > 0)
                            requestConfiguration.QueryParameters.Top = pagingNum;
                        else
                            requestConfiguration.QueryParameters.Top = 5;

                        if (!string.IsNullOrEmpty(filter))
                            requestConfiguration.QueryParameters.Filter = filter;

                        if (!string.IsNullOrEmpty(header))
                            requestConfiguration.Headers.Add("ConsistencyLevel", header);

                        if (attributes != null && attributes.Length > 0)
                            requestConfiguration.QueryParameters.Select = attributes;
                    });



                // Loop through the devices paging through the results
                while (devices?.Value != null)
                {
                    

                    foreach (var device in devices.Value)
                    {
                        deviceList.Add(device);

                        try
                        {
                            var hwId = device.PhysicalIds?.Find(static x => x.StartsWith("[HWID]"))?.Split(':')[2];
                            if (hwId != null)
                            {
                                _logger.LogTrace("Device Name {DisplayName} | DeviceID: {DeviceId} | TrustType: {TrustType} | HWID: {HWID}", device.DisplayName, device.DeviceId, device.TrustType, hwId);
                            }
                            else
                            {
                                _logger.LogTrace("Device Name {DisplayName} | DeviceID: {DeviceId} | TrustType: {TrustType}", device.DisplayName, device.DeviceId, device.TrustType);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error retrieving HWID for device {DeviceId}", device.DeviceId);
                            _logger.LogTrace("Device Name {DisplayName} | DeviceID: {DeviceId} | TrustType: {TrustType}", device.DisplayName, device.DeviceId, device.TrustType);
                        }
                    }


                    // If OdataNextLink has a value, there is another page
                    if (!string.IsNullOrEmpty(devices.OdataNextLink))
                    {
                        // Pass the OdataNextLink to the WithUrl method
                        // to request the next page
                        devices = await _graphServiceClient.Devices
                            .WithUrl(devices.OdataNextLink)
                            .GetAsync();

                    }
                    else
                    {
                        // No more results, exit loop
                        break;
                    }

                }

                return deviceList;


            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Error retrieving devices");
                return Enumerable.Empty<Device>();
            }

        }


        public async Task<string?> GetDeviceHWId(string deviceId)
        {
            // retrieve the array item of the PhysicalIds property starting with "[HWID]" and extract the HWID value
            try
            {
                var device = await _graphServiceClient.Devices[deviceId].GetAsync();

                if (device == null || device.PhysicalIds == null || device.PhysicalIds.Count == 0)
                {
                    return string.Empty; // Return an empty string instead of null
                }

                try
                {
                    var hwId = device.PhysicalIds.Find(static x => x.StartsWith("[HWID]"))?.Split(':')[2];
                    if (hwId != null)
                    {
                        _logger.LogInformation("Device {DeviceID} has the Hardware ID {hwId}", device.Id, hwId);
                        return hwId;
                    }
                    else
                    {
                        _logger.LogInformation("Device {DeviceID} has no Hardware ID", device.Id);
                        return string.Empty; // Return an empty string instead of null
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving HWID for device {DeviceId}", deviceId);
                    return string.Empty; // Return an empty string instead of null
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving HWID for device {DeviceId}", deviceId);
                return string.Empty; // Return an empty string instead of null
            }
        }


        public async Task<IEnumerable<Device>> GetDevicesWithSameHwId(string hwId)
        {
            _logger.LogInformation("GetDevicesWithSameHwId called with {HWID}", hwId);

            var devices = await this.GetDevices($"(physicalIds/any(p:p eq '[HWID]:h:{hwId}'))", "eventual");

            return devices;

        }

        public async Task<IEnumerable<Device>> GetDevicesWithSameHwId(string hwId, string deviceId)
        {

            _logger.LogInformation("GetDevicesWithSameHwId called with {HWID} for DeviceId", hwId);
            var devices = await this.GetDevices($"(physicalIds/any(p:p eq '[HWID]:h:{hwId}') and not (deviceId eq {deviceId}))", "eventual");

            return devices;
        }


        public async Task<bool> DeleteDevicesWithSameHwId(string hwId)
        {
            _logger.LogInformation("DeleteDevicesWithSameHwId called with {HWID}", hwId);

            try
            {
                var devices = await _graphServiceClient.Devices
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"(physicalIds/any(p:p eq '[HWID]:h:{hwId}'))";
                        requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                        requestConfiguration.QueryParameters.Select =
                            new[] { "displayName", "deviceId", "id", "physicalIds", "trustType", "operatingSystemVersion" };
                    });

                if (devices?.Value == null)
                {
                    return false;
                }

                foreach (var device in devices.Value)
                {
                    await _graphServiceClient.Devices[device.Id].DeleteAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting devices with HWID {HWID}", hwId);
                return false;
            }
        }


        public async Task<bool> DeleteDevicesWithSameHwId(string hwId, string deviceId)
        {
            _logger.LogInformation("DeleteDevicesWithSameHwId called with {HWID} for DeviceId", hwId);

            try
            {
                var devices = await _graphServiceClient.Devices
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"(physicalIds/any(p:p eq '[HWID]:h:{hwId}') and not (deviceId eq {deviceId}))";
                        requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                        requestConfiguration.QueryParameters.Select =
                            new[] { "displayName", "deviceId", "id", "physicalIds", "trustType", "operatingSystemVersion" };
                    });

                if (devices?.Value == null)
                {
                    return false;
                }

                foreach (var device in devices.Value)
                {
                    await _graphServiceClient.Devices[device.Id].DeleteAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting devices with HWID {HWID}", hwId);
                return false;
            }
        }


        public async Task<IEnumerable<string>> GetDeviceHWIdByComputerName(string computerName)
        {
            _logger.LogInformation("GetDeviceHWIdByComputerName called with {ComputerName}", computerName);

            List<string> hwIds = new List<string>();

            try
            {
                _logger.LogTrace("Get the Access Token");
                var token = await _authenticationHandler.GetAccessTokenAsync();
                _logger.LogTrace("Access Token retrieved");

                _logger.LogTrace("Building the request to the Microsoft Graph API (beta)");
                var request = new HttpRequestMessage(HttpMethod.Get, 
                    $"https://graph.microsoft.com/beta/devices?$filter=displayName eq '{computerName}'");
                _logger.LogTrace("Request built");

                _logger.LogTrace("Adding the access token to the request headers");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                request.Headers.Add("ConsistencyLevel", "eventual");
                _logger.LogTrace("Access token added to the request headers");

                _logger.LogTrace("Sending the request");
                var response = await _httpClient.SendAsync(request);
                _logger.LogTrace("Request sent");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error retrieving devices for {ComputerName}. Status code: {StatusCode}", computerName, response.StatusCode);
                    return Enumerable.Empty<string>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<Microsoft.Graph.Models.DeviceCollectionResponse>(content);

                if (result?.Value == null || result.Value.Count == 0)
                {
                    _logger.LogInformation("No devices found with name {ComputerName}", computerName);
                    return Enumerable.Empty<string>();
                }

                // If multiple devices found, prioritize ServerAd devices (devices synced from AD)
                var devicesToProcess = result.Value;
                if (result.Value.Count > 1)
                {
                    _logger.LogInformation("Found {Count} devices with name {ComputerName}. Filtering by trustType.", result.Value.Count, computerName);
                    
                    var serverAdDevices = result.Value.Where(d => 
                        string.Equals(d.TrustType, "ServerAd", StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    if (serverAdDevices.Any())
                    {
                        _logger.LogInformation("Found {Count} ServerAd devices for {ComputerName}", serverAdDevices.Count, computerName);
                        devicesToProcess = serverAdDevices;
                    }
                    else
                    {
                        _logger.LogWarning("No ServerAd device found for {ComputerName}. Processing all devices.", computerName);
                    }
                }

                foreach (var device in devicesToProcess)
                {
                    if (device.PhysicalIds == null || device.PhysicalIds.Count == 0)
                    {
                        continue;
                    }

                    // retrieve the array item of the PhysicalIds property starting with "[HWID]" and extract the HWID value
                    try
                    {
                        var hwIdEntry = device.PhysicalIds.Find(x => x.StartsWith("[HWID]"));
                        if (hwIdEntry != null)
                        {
                            string hwId = hwIdEntry.Split(':')[2];
                            _logger.LogInformation("Device {DeviceID} (TrustType: {TrustType}) has the Hardware ID {hwId}", device.Id, device.TrustType, hwId);
                            hwIds.Add(hwId);
                        }
                        else
                        {
                            _logger.LogInformation("Device {DeviceID} has no Hardware ID", device.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving HWID for device {DeviceId}", device.Id);
                    }
                }

                return hwIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving HWID for device {ComputerName}", computerName);
                return Enumerable.Empty<string>();
            }
        }

    }
}
