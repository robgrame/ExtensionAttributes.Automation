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
        Task<ManagedDevice?> GetDeviceDetails(string deviceId);
        Task<ManagedDevice?> GetDeviceDetailsbyName(string deviceName);
        Task<ManagedDevice?> GetDeviceDetailsByEntraDeviceId(string entraDeviceId);
        #endregion

        #region Hardware Information (using available ManagedDevice properties)
        Task<Dictionary<string, string?>> GetDeviceHardwareInformation(string deviceId);
        Task<Dictionary<string, string?>> GetDeviceHardwareInformationByName(string deviceName);
        Task<Dictionary<string, string?>> GetDeviceHardwareInformationByEntraDeviceId(string entraDeviceId);
        #endregion

        #region Software Information
        Task<IEnumerable<MobileApp>> GetInstalledApplications(string deviceId);
        Task<IEnumerable<DetectedApp>> GetDetectedApplications(string deviceId);
        Task<string?> GetOperatingSystemVersion(string deviceId);
        Task<string?> GetOperatingSystemBuild(string deviceId);
        #endregion

        #region Device Configuration
        Task<bool> IsBitLockerEnabled(string deviceId);
        Task<DateTime?> GetLastSyncDateTime(string deviceId);
        Task<string?> GetDeviceOwnership(string deviceId);
        Task<string?> GetManagementAgent(string deviceId);
        #endregion

        #region Device Lookup by Multiple Identifiers
        Task<ManagedDevice?> FindDeviceByComputerName(string computerName);
        Task<ManagedDevice?> FindDeviceBySerialNumber(string serialNumber);
        Task<IEnumerable<ManagedDevice>> FindDevicesByUser(string userPrincipalName);
        #endregion

        #region Autopilot
        Task<bool> IsAutopilotRegistered(string serialNumber);
        Task<bool> IsAutopilotRegistered(string serialNumber, string hardwareHash);
        Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash);
        Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag);
        Task<string> RegisterAutoPilotDevice(string serialNumber, string hardwareHash, string groupTag, string userUPN);
        #endregion

        #region Extension Attribute Support
        Task<string?> GetIntuneDeviceProperty(string deviceId, string propertyName);
        Task<string?> GetIntuneDevicePropertyByComputerName(string computerName, string propertyName);
        #endregion
    }
}