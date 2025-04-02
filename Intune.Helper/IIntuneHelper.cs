using Microsoft.Graph.Models;

namespace Azure.Automation.Intune
{
    public interface IIntuneHelper
    {


        #region Number of Devices
        Task<ManagedDeviceCollectionResponse> GetIntuneDevices();
        Task<int> GetTotalWindowsDevices();
        Task<int> GetTotalMacOSDevices();
        Task<int> GetTotaliOSDevices();
        Task<int> GetTotalAndroidDevices();
        Task<int> GetTotalWindows365Devices();
        #endregion

        #region Compliance
        Task<int> GetTotalCompliantDevices();
        Task<int> GetTotalNonCompliantDevices();
        Task<int> GetTotalCompliantWindowsDevices();
        Task<int> GetTotalNonCompliantWindowsDevices();

        Task<ManagedDeviceCollectionResponse> GetCompliantDevices();
        Task<ManagedDeviceCollectionResponse> GetNonCompliantDevices();
        Task<ManagedDeviceCollectionResponse> GetCompliantWindowsDevices();
        Task<ManagedDeviceCollectionResponse> GetNonCompliantWindowsDevices();

        Task<ComplianceState> GetDeviceComplianceState(string deviceId);
        Task<ComplianceState> GetDeviceComplianceStatebyName(string deviceName);

        #endregion


        #region Windows 365
        #endregion


        #region Device Details
        Task<ManagedDevice> GetDeviceDetails(string deviceId);
        Task<ManagedDevice> GetDeviceDetailsbyName(string deviceName);
        #endregion


        #region Autopilot
        Task<bool> IsAutopilotRegistered(string serialNumber);
        Task<bool> IsAutopilotRegistered(string serialNumber, string hardwareHash);
        Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash);
        Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag);
        Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag, string userUPN);
        #endregion
    }
}