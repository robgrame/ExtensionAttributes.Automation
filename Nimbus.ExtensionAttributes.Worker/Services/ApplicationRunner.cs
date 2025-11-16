using Nimbus.ExtensionAttributes.WorkerSvc.JobUtils;
using Nimbus.ExtensionAttributes.WorkerSvc.Config;
using Nimbus.ExtensionAttributes.WorkerSvc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace Nimbus.ExtensionAttributes.WorkerSvc.Services
{
    public class ApplicationRunner
    {
        public static async Task<int> RunServiceAsync(HostApplicationBuilder builder)
        {
            try
            {
                ServiceRegistrationService.ConfigureQuartz(builder);
                ServiceRegistrationService.ConfigureWindowsService(builder);

                Log.Information("Building Worker Service host");
                var host = builder.Build();

                Log.Information("Starting Worker Service");
                await host.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Service host terminated unexpectedly");
                return 1;
            }
        }

        public static async Task<int> RunConsoleAsync(HostApplicationBuilder builder, string[] args)
        {
            try
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILogger<ApplicationRunner>>();
                var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

                logger.LogInformation("Running in console mode with arguments: {args}", string.Join(" ", args));

                if (!OperatingSystem.IsWindows())
                {
                    logger.LogError("The extension attribute operations are only supported on Windows.");
                    return 1;
                }

                // Use the new unified helper that handles both AD and Intune based on configuration
                logger.LogInformation("Processing extension attributes using unified approach...");
                var unifiedHelper = serviceProvider.GetRequiredService<UnifiedExtensionAttributeHelper>();
                
                var processedCount = await unifiedHelper.ProcessExtensionAttributesAsync();
                logger.LogInformation("Unified processing completed. Processed {ProcessedCount} devices", processedCount);

                logger.LogInformation("Console operation completed successfully");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Console operation failed: {exception}", ex.Message);
                return 1;
            }
        }

        public static async Task<int> RunDeviceAsync(HostApplicationBuilder builder, string deviceName)
        {
            try
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILogger<ApplicationRunner>>();
                var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

                logger.LogInformation("Running in device-specific mode for device: {deviceName}", deviceName);

                if (!OperatingSystem.IsWindows())
                {
                    logger.LogError("The extension attribute operations are only supported on Windows.");
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    logger.LogError("Device name cannot be null or empty");
                    return 1;
                }

                // Log configuration information
                logger.LogInformation("Data Sources - ActiveDirectory: {EnableAD}, Intune: {EnableIntune}, Preferred: {Preferred}",
                    appSettings.DataSources.EnableActiveDirectory, 
                    appSettings.DataSources.EnableIntune, 
                    appSettings.DataSources.PreferredDataSource);

                logger.LogInformation("Extension Attribute Mappings configured: {MappingCount}", 
                    appSettings.ExtensionAttributeMappings.Count);

                // Process single device using unified helper
                var unifiedHelper = serviceProvider.GetRequiredService<UnifiedExtensionAttributeHelper>();
                
                logger.LogInformation("Processing extension attributes for single device: {DeviceName}", deviceName);
                var processed = await unifiedHelper.ProcessSingleDeviceAsync(deviceName);
                
                if (processed)
                {
                    logger.LogInformation("? Successfully processed device: {DeviceName}", deviceName);
                    
                    // Show summary of what was processed
                    var enabledMappings = appSettings.ExtensionAttributeMappings.Where(m => 
                        (m.DataSource == DataSourceType.ActiveDirectory && appSettings.DataSources.EnableActiveDirectory) ||
                        (m.DataSource == DataSourceType.Intune && appSettings.DataSources.EnableIntune)
                    ).ToList();

                    logger.LogInformation("Processed {EnabledMappings} extension attribute mappings:", enabledMappings.Count);
                    foreach (var mapping in enabledMappings)
                    {
                        logger.LogInformation("  • {ExtensionAttribute} <- {SourceAttribute} ({DataSource})", 
                            mapping.ExtensionAttribute, mapping.SourceAttribute, mapping.DataSource);
                    }

                    return 0;
                }
                else
                {
                    logger.LogError("? Failed to process device: {DeviceName}", deviceName);
                    logger.LogInformation("Possible reasons:");
                    logger.LogInformation("  • Device not found in Entra AD");
                    logger.LogInformation("  • Device not found in configured data sources (AD/Intune)");
                    logger.LogInformation("  • Network connectivity issues");
                    logger.LogInformation("  • Insufficient permissions");
                    
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Device operation failed for {deviceName}: {exception}", deviceName, ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// Run device processing by Entra AD Device ID
        /// </summary>
        /// <param name="builder">Host application builder</param>
        /// <param name="deviceId">Entra AD Device ID</param>
        /// <returns>Exit code</returns>
        public static async Task<int> RunDeviceByIdAsync(HostApplicationBuilder builder, string deviceId)
        {
            try
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILogger<ApplicationRunner>>();
                var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

                logger.LogInformation("Running in device-specific mode for device ID: {deviceId}", deviceId);

                if (!OperatingSystem.IsWindows())
                {
                    logger.LogError("The extension attribute operations are only supported on Windows.");
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    logger.LogError("Device ID cannot be null or empty");
                    return 1;
                }

                // Process single device by ID using unified helper
                var unifiedHelper = serviceProvider.GetRequiredService<UnifiedExtensionAttributeHelper>();
                
                logger.LogInformation("Processing extension attributes for device ID: {DeviceId}", deviceId);
                var processed = await unifiedHelper.ProcessSingleDeviceByIdAsync(deviceId);
                
                if (processed)
                {
                    logger.LogInformation("? Successfully processed device with ID: {DeviceId}", deviceId);
                    return 0;
                }
                else
                {
                    logger.LogError("? Failed to process device with ID: {DeviceId}", deviceId);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Device operation by ID failed for {deviceId}: {exception}", deviceId, ex.Message);
                return 1;
            }
        }
    }
}