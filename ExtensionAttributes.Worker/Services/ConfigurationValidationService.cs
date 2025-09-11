using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using AD.Helper.Config;
using Azure.Automation.Config;
using Azure.Automation.Intune.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Services
{
    public class ConfigurationValidationService
    {
        private readonly ILogger<ConfigurationValidationService> _logger;

        public ConfigurationValidationService(ILogger<ConfigurationValidationService> logger)
        {
            _logger = logger;
        }

        public bool ValidateConfiguration(IConfiguration configuration)
        {
            var isValid = true;

            // Validate AppSettings
            var settings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
            if (settings == null)
            {
                _logger.LogError("Failed to load AppSettings from configuration.");
                return false;
            }

            // Validate required settings
            if (string.IsNullOrWhiteSpace(settings.ExportPath))
            {
                _logger.LogError("ExportPath is required in AppSettings.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(settings.ExportFileNamePrefix))
            {
                _logger.LogError("ExportFileNamePrefix is required in AppSettings.");
                isValid = false;
            }

            if (settings.ExtensionAttributeMappings == null || !settings.ExtensionAttributeMappings.Any())
            {
                _logger.LogError("ExtensionAttributeMappings is required in AppSettings.");
                isValid = false;
            }

            // Validate extension attribute mappings
            foreach (var mapping in settings.ExtensionAttributeMappings ?? new List<ExtensionAttributeMapping>())
            {
                if (string.IsNullOrWhiteSpace(mapping.ExtensionAttribute))
                {
                    _logger.LogError("ExtensionAttribute is required in all mappings.");
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(mapping.SourceAttribute))
                {
                    _logger.LogError("SourceAttribute is required in all mappings.");
                    isValid = false;
                }

                // Validate data source specific requirements
                if (mapping.DataSource == DataSourceType.ActiveDirectory && !settings.DataSources.EnableActiveDirectory)
                {
                    _logger.LogWarning("Mapping for {ExtensionAttribute} uses AD but AD is disabled", mapping.ExtensionAttribute);
                }

                if (mapping.DataSource == DataSourceType.Intune && !settings.DataSources.EnableIntune)
                {
                    _logger.LogWarning("Mapping for {ExtensionAttribute} uses Intune but Intune is disabled", mapping.ExtensionAttribute);
                }
            }

            // Validate EntraADHelperSettings
            var entraADHelperSettings = configuration.GetSection(nameof(EntraADHelperSettings)).Get<EntraADHelperSettings>();
            if (entraADHelperSettings == null)
            {
                _logger.LogError("Failed to load EntraADHelperSettings from configuration.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(entraADHelperSettings.ClientId))
            {
                _logger.LogError("ClientId is required in EntraADHelperSettings.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(entraADHelperSettings.TenantId))
            {
                _logger.LogError("TenantId is required in EntraADHelperSettings.");
                isValid = false;
            }

            if (entraADHelperSettings.UseClientSecret)
            {
                if (string.IsNullOrWhiteSpace(entraADHelperSettings.ClientSecret))
                {
                    _logger.LogError("ClientSecret is required when UseClientSecret is true.");
                    isValid = false;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entraADHelperSettings.CertificateThumbprint))
                {
                    _logger.LogError("CertificateThumbprint is required when UseClientSecret is false.");
                    isValid = false;
                }
            }

            // Validate ADHelperSettings (only if AD is enabled)
            if (settings.DataSources.EnableActiveDirectory)
            {
                var adHelperSettings = configuration.GetSection(nameof(ADHelperSettings)).Get<ADHelperSettings>();
                if (adHelperSettings == null)
                {
                    _logger.LogError("Failed to load ADHelperSettings from configuration when ActiveDirectory is enabled.");
                    isValid = false;
                }
                else if (string.IsNullOrWhiteSpace(adHelperSettings.RootOrganizationaUnitDN))
                {
                    _logger.LogError("RootOrganizationaUnitDN is required in ADHelperSettings when ActiveDirectory is enabled.");
                    isValid = false;
                }
            }

            // Validate IntuneHelperSettings (only if Intune is enabled)
            if (settings.DataSources.EnableIntune)
            {
                var intuneHelperSettings = configuration.GetSection(nameof(IntuneHelperSettings)).Get<IntuneHelperSettings>();
                if (intuneHelperSettings == null)
                {
                    _logger.LogError("Failed to load IntuneHelperSettings from configuration when Intune is enabled.");
                    isValid = false;
                }
            }

            // Ensure at least one data source is enabled
            if (!settings.DataSources.EnableActiveDirectory && !settings.DataSources.EnableIntune)
            {
                _logger.LogError("At least one data source (ActiveDirectory or Intune) must be enabled.");
                isValid = false;
            }

            return isValid;
        }

        public void LogConfigurationValues(IConfiguration configuration)
        {
            var settings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
            var adHelperSettings = configuration.GetSection(nameof(ADHelperSettings)).Get<ADHelperSettings>();
            var entraADHelperSettings = configuration.GetSection(nameof(EntraADHelperSettings)).Get<EntraADHelperSettings>();
            var intuneHelperSettings = configuration.GetSection(nameof(IntuneHelperSettings)).Get<IntuneHelperSettings>();

            // Print out the Worker Service Assembly name and version
            string assemblyVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly()?.Location ?? string.Empty).FileVersion?.ToString() ?? "Unknown version";

            _logger.LogInformation("Worker Service Assembly: {AssemblyName} with version: {version}", 
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, assemblyVersion);

            // Log arguments passed to the service
            var args = Environment.GetCommandLineArgs();
            _logger.LogInformation("Worker Service arguments: {args}", string.Join(" ", args.Skip(1)));

            // Print out the configuration values
            _logger.LogInformation("ClientId: {ClientId}", entraADHelperSettings?.ClientId ?? "ClientID not provided");
            _logger.LogInformation("TenantId: {TenantId}", entraADHelperSettings?.TenantId ?? "TenantId not provided");
            _logger.LogInformation("UseClientSecret: {UseClient}", entraADHelperSettings?.UseClientSecret);
            _logger.LogInformation("ClientSecret: {ClientSecret}", entraADHelperSettings?.ClientSecret == null ? "ClientSecret not provided" : "**********");
            _logger.LogInformation("CertificateThumbprint: {CertificateThumbprint}", entraADHelperSettings?.CertificateThumbprint ?? "CertificateThumbprint not provided");

            // Log data source configuration
            _logger.LogInformation("Data Sources - ActiveDirectory: {EnableAD}, Intune: {EnableIntune}, Preferred: {Preferred}",
                settings?.DataSources.EnableActiveDirectory, settings?.DataSources.EnableIntune, settings?.DataSources.PreferredDataSource);

            // Log AD configuration if enabled
            if (settings?.DataSources.EnableActiveDirectory == true)
            {
                _logger.LogInformation("RootOrganizationaUnitDN: {RootOrganizationaUnitDN}", 
                    adHelperSettings?.RootOrganizationaUnitDN ?? "RootOrganizationaUnitDN not provided");
                
                // print all excluded OUs one per line
                if (adHelperSettings?.ExcludedOUs != null)
                {
                    foreach (var ou in adHelperSettings.ExcludedOUs)
                    {
                        _logger.LogInformation("ExcludedOU: {ExcludedOU}", ou);
                    }
                }
            }

            // Log Intune configuration if enabled
            if (settings?.DataSources.EnableIntune == true)
            {
                _logger.LogInformation("Intune MaxConcurrentRequests: {MaxConcurrentRequests}", intuneHelperSettings?.MaxConcurrentRequests);
                _logger.LogInformation("Intune EnableHardwareInfoRetrieval: {EnableHardwareInfo}", intuneHelperSettings?.EnableHardwareInfoRetrieval);
                _logger.LogInformation("Intune EnableSoftwareInfoRetrieval: {EnableSoftwareInfo}", intuneHelperSettings?.EnableSoftwareInfoRetrieval);
            }

            // print all unified ExtensionAttributeMappings one per line
            if (settings?.ExtensionAttributeMappings != null)
            {
                foreach (var mapping in settings.ExtensionAttributeMappings)
                {
                    _logger.LogInformation("Unified ExtensionAttributeMapping: {Mapping}", mapping.ToString());
                }
            }

            _logger.LogInformation("AttributesToLoad: {AttributesToLoad}", 
                string.Join(", ", entraADHelperSettings?.AttributesToLoad ?? Enumerable.Empty<string>()));

            _logger.LogInformation("ExportPath: {ExportPath}", settings?.ExportPath);
            _logger.LogInformation("ExportFileNamePrefix: {ExportFileNamePrefix}", settings?.ExportFileNamePrefix);
        }
    }
}