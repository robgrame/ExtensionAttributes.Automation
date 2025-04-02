using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
                    _logger.LogInformation("Device {DeviceName} with {DeviceID}", device.DeviceName, device.Id);
                });

                return managedDevices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Intune devices");
                return new ManagedDeviceCollectionResponse();
            }
        }

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

            var comopliantDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'compliant'";
            });

            _logger.LogDebug("Total Compliant devices: {CompliantDevices}", comopliantDevices?.Value?.Count ?? 0);


            return comopliantDevices?.Value?.Count ?? 0;
        }

        public async Task<int> GetTotalNonCompliantDevices()
        {
            _logger.LogTrace("Getting total noncompliant devices");

            var comopliantDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'noncompliant'";
            });

            _logger.LogDebug("Total NonCompliant devices: {NonCompliantDevices}", comopliantDevices?.Value?.Count ?? 0);


            return comopliantDevices?.Value?.Count ?? 0;
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
            _logger.LogDebug("Getting total compliant Windows devices");

            var compliantWindowsDevices = await _graphServiceClient.DeviceManagement.ManagedDevices.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = "complianceState eq 'noncompliant' and operatingSystem eq 'Windows'";
            });

            return compliantWindowsDevices?.Value?.Count ?? 0;
        }

        public async Task<int> GetTotaliOSDevices()
        {
            throw new NotImplementedException();
        }

        public async Task<int> GetTotalMacOSDevices()
        {
            throw new NotImplementedException();
        }


        public async Task<int> GetTotalWindows365Devices()
        {
            throw new NotImplementedException();
        }

        public async Task<int> GetTotalWindowsDevices()
        {
            throw new NotImplementedException();
        }


        public async Task<ManagedDeviceCollectionResponse> GetCompliantDevices()
        {
            throw new NotImplementedException();
        }

        public async Task<ManagedDeviceCollectionResponse> GetNonCompliantDevices()
        {
            throw new NotImplementedException();
        }

        public async Task<ManagedDeviceCollectionResponse> GetCompliantWindowsDevices()
        {
            throw new NotImplementedException();
        }

        public async Task<ManagedDeviceCollectionResponse> GetNonCompliantWindowsDevices()
        {
            throw new NotImplementedException();
        }

        public async Task<ManagedDevice> GetDeviceDetails(string deviceId)
        {
            throw new NotImplementedException();
        }

        public async Task<ManagedDevice> GetDeviceDetailsbyName(string deviceName)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsAutopilotRegistered(string serialNumber)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsAutopilotRegistered(string serialNumber, string hardwareHash)
        {
            throw new NotImplementedException();
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash)
        {
            throw new NotImplementedException();
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag)
        {
            throw new NotImplementedException();
        }

        public async Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag, string userUPN)
        {
            throw new NotImplementedException();
        }

        public async Task<ComplianceState> GetDeviceComplianceState(string deviceId)
        {
            throw new NotImplementedException();
        }

        public async Task<ComplianceState> GetDeviceComplianceStatebyName(string deviceName)
        {
            throw new NotImplementedException();
        }
    }
}
