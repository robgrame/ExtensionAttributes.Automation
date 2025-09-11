using RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils;
using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Services
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

                logger.LogInformation("Running Worker Service for device: {deviceName}", deviceName);
                logger.LogError("Unfortunately this scenario is not yet implemented");
                
                // TODO: Implement device-specific logic using the unified approach
                // This could process extension attributes for a single device from either AD or Intune
                await Task.CompletedTask;
                
                return 1; // Return error code until implemented
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Device operation failed: {exception}", ex.Message);
                return 1;
            }
        }
    }
}