namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Config
{
    public class AppSettings
    {
        public required string CertificateThumbprint { get; set; }
        public required string ExportPath { get; set; }
        public required string ExportFileNamePrefix { get; set; }
        public required List<ExtensionAttributeMapping> ExtensionAttributeMappings { get; set; }
        
        // New property to enable/disable different data sources
        public DataSourceSettings DataSources { get; set; } = new DataSourceSettings();
    }

    public class DataSourceSettings
    {
        public bool EnableActiveDirectory { get; set; } = true;
        public bool EnableIntune { get; set; } = false;
        public string PreferredDataSource { get; set; } = "ActiveDirectory"; // "ActiveDirectory", "Intune", "Both"
    }

    public class ExtensionAttributeMapping
    {
        public required string ExtensionAttribute { get; set; }
        public required string SourceAttribute { get; set; } // Renamed from ComputerAttribute to be more generic
        public required DataSourceType DataSource { get; set; } // NEW: Identifies the data source
        public string? Regex { get; set; } // Nullable string for optional regex
        public string? DefaultValue { get; set; } // NEW: Default value if attribute is null/empty
        public bool UseHardwareInfo { get; set; } = false; // NEW: For Intune hardware information
        public string? PropertyPath { get; set; } // NEW: For nested properties
        
        public override string ToString()
        {
            return $"{ExtensionAttribute} -> {SourceAttribute} (Source: {DataSource}, Regex: {Regex ?? "none"}, Default: {DefaultValue ?? "none"})";
        }
    }

    public enum DataSourceType
    {
        ActiveDirectory,
        Intune
    }
}
