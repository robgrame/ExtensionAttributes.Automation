namespace Azure.Automation.Intune.Config
{
    public class IntuneHelperSettings
    {
        public required string[] AttributesToLoad { get; set; } = new string[] 
        { 
            "id", "deviceName", "operatingSystem", "osVersion", "complianceState", 
            "lastSyncDateTime", "serialNumber", "manufacturer", "model", 
            "totalStorageSpaceInBytes", "freeStorageSpaceInBytes", "managedDeviceOwnerType",
            "enrolledDateTime", "azureADDeviceId", "userPrincipalName"
        };
        public int PageSize { get; set; } = 1000;
        public int ClientTimeout { get; set; } = 60000;
        public bool EnableHardwareInfoRetrieval { get; set; } = true;
        public bool EnableSoftwareInfoRetrieval { get; set; } = false; // Can be resource intensive
        public int MaxConcurrentRequests { get; set; } = 10;
        public required List<IntuneExtensionAttributeMapping> IntuneExtensionAttributeMappings { get; set; } = new();
    }

    public class IntuneExtensionAttributeMapping
    {
        public required string ExtensionAttribute { get; set; }
        public required string IntuneDeviceProperty { get; set; }
        public string? Regex { get; set; } // Optional regex for value extraction
        public string? DefaultValue { get; set; } // Default value if property is null/empty
        public bool UseHardwareInfo { get; set; } = false; // If true, looks in hardware information
        public string? PropertyPath { get; set; } // For nested properties like "hardwareInformation.manufacturer"
        
        public override string ToString()
        {
            return $"{ExtensionAttribute} -> {IntuneDeviceProperty} " +
                   $"(Regex: {Regex ?? "none"}, Default: {DefaultValue ?? "none"}, " +
                   $"HardwareInfo: {UseHardwareInfo}, Path: {PropertyPath ?? "direct"})";
        }
    }

    public enum IntuneDataSource
    {
        ManagedDevice,
        HardwareInformation,
        DetectedApplications,
        InstalledApplications,
        ComplianceState
    }
}