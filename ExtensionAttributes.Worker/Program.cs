using RGP.ExtensionAttributes.Automation.WorkerSvc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                // Create and configure the host builder
                var builder = Host.CreateApplicationBuilder(args);
                HostConfigurationService.ConfigureHost(builder);

                // Create service provider for early services
                using var earlyServiceProvider = builder.Services.BuildServiceProvider();
                var consoleService = earlyServiceProvider.GetRequiredService<ConsoleDisplayService>();
                var commandLineService = earlyServiceProvider.GetRequiredService<CommandLineService>();
                var configValidationService = earlyServiceProvider.GetRequiredService<ConfigurationValidationService>();

                // Show application header and logo
                consoleService.ShowApplicationHeader();
                consoleService.ShowLogo();

                // Parse command line arguments
                var options = commandLineService.ParseArguments(args);

                if (options.ShowHelp)
                {
                    commandLineService.ShowUsage();
                    return 0;
                }

                // Log startup information
                Log.Information("------------------------------------------------------------------------------------------------------");
                Log.Information("------------------------------------ STARTING WORKER SERVICE -----------------------------------------");
                Log.Information("------------------------------------------------------------------------------------------------------");

                // Validate configuration
                if (!configValidationService.ValidateConfiguration(builder.Configuration))
                {
                    Log.Error("Configuration validation failed. Please check your configuration files.");
                    return 1;
                }

                Log.Debug("Configuration validation successful");
                configValidationService.LogConfigurationValues(builder.Configuration);

                // Register services
                ServiceRegistrationService.RegisterServices(builder);

                // Execute based on run mode
                return options.Mode switch
                {
                    RunMode.Service => await ApplicationRunner.RunServiceAsync(builder),
                    RunMode.Console => await ApplicationRunner.RunConsoleAsync(builder, args),
                    RunMode.Device => await ApplicationRunner.RunDeviceAsync(builder, options.DeviceName ?? "Unknown"),
                    RunMode.DeviceById => await ApplicationRunner.RunDeviceByIdAsync(builder, options.DeviceId ?? "Unknown"),
                    _ => 1
                };
            }
            catch (ArgumentNullException ex)
            {
                Log.Error(ex, "Worker Service failed with ArgumentNullException: {exception}", ex.Message);
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                Log.Error(ex, "Worker Service failed with FileNotFoundException: {exception}", ex.Message);
                return 1;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, "Worker Service failed with UnauthorizedAccessException: {exception}", ex.Message);
                return 1;
            }
            catch (TaskCanceledException ex)
            {
                Log.Warning(ex, "Worker Service was cancelled: {exception}", ex.Message);
                return 1;
            }
            catch (OperationCanceledException ex)
            {
                Log.Warning(ex, "Worker Service was cancelled: {exception}", ex.Message);
                return 1;
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                Log.Error(ex, "Worker Service failed with CryptographicException: {exception}. Verify you have required permissions to access certificate private key. This generally requires admin privileges on the running machine.", ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Worker Service terminated unexpectedly: {exception}", ex.Message);
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
