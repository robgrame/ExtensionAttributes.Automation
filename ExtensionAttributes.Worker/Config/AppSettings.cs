namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Config
{
    public class AppSettings
    {
        public required string CertificateThumbprint { get; set; }
        public required string ExportPath { get; set; }
        public required string ExportFileNamePrefix { get; set; }
        public required List<ExtensionAttributeMapping> ExtensionAttributeMappings { get; set; }
    }


    public class ExtensionAttributeMapping
    {
        public required string ExtensionAttribute { get; set; }
        public required string ComputerAttribute { get; set; }
        public string? Regex { get; set; } // Nullable string for optional regex
        public override string ToString()
        {
            return $"{ExtensionAttribute} -> {ComputerAttribute} (Regex: {Regex})";
        }
    }



}
