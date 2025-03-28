using Microsoft.Graph.Models;

namespace Azure.Automation
{
    public interface IEntraADHelper
    {
        Task<Device?> GetDeviceAsync(string deviceId);
        Task<Device?> GetDeviceByNameAsync(string deviceName);
        Task<string?> GetExtensionAttribute(string deviceId, string extensionAttribute);
        Task<List<string?>> GetExtensionAttributes(string deviceId, List<string> extensionAttributes);
        Task<string?> GetDeviceHWId(string deviceId);
        Task<IEnumerable<string>> GetDeviceHWIdByComputerName(string computerName);
        Task<IEnumerable<Device>> GetDevicesWithSameHwId(string hardwareId);
        Task<IEnumerable<Device>> GetDevicesWithSameHwId(string hwId, string deviceId);
        Task<bool> DeleteDevicesWithSameHwId(string hwId, string deviceId);
        Task<IEnumerable<Device>> GetDevices();
        Task<IEnumerable<Device>> GetDevices(int pagingNum);
        Task<IEnumerable<Device>> GetDevices(string filter);
        Task<IEnumerable<Device>> GetDevices(int pagingNum, string filter);
        Task<IEnumerable<Device>> GetDevices(string filter, string header);
        Task<IEnumerable<Device>> GetDevices(int pagingNum, string filter, string header);
        Task<IEnumerable<Device>> GetDevices(string filter, string[] attributes);
        Task<IEnumerable<Device>> GetDevices(int? pagingNum, string? filter, string? header, string[]? attributes);
        Task<IEnumerable<Device>> GetDevicesAssignedToUser(string userId);
        Task<User?> GetUser(string userId);
        Task<IEnumerable<User>> GetUsers();
        Task<IEnumerable<User>> GetUsers(int pagingNum);

        #region Set Device Attributes
        Task<string?> SetExtensionAttributeValue(string deviceId, string extenstionAttribute, string extensionAttributeValue);
        #endregion
    }
}