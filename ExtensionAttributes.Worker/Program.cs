using ExtensionAttributes.Automation.WorkerSvc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace ExtensionAttributes.Automation.WorkerSvc
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                // Parse command line arguments early to determine mode
                var tempBuilder = Host.CreateApplicationBuilder(args);
                HostConfigurationService.ConfigureHost(tempBuilder);
                
                using var earlyServiceProvider = tempBuilder.Services.BuildServiceProvider();
                var commandLineService = earlyServiceProvider.GetRequiredService<CommandLineService>();
                var options = commandLineService.ParseArguments(args);

                // Show help if requested
                if (options.ShowHelp)
                {
                    var consoleService = earlyServiceProvider.GetRequiredService<ConsoleDisplayService>();
                    consoleService.ShowApplicationHeader();
                    consoleService.ShowLogo();
                    commandLineService.ShowUsage();
                    return 0;
                }

                // Handle web application mode separately
                if (options.Mode == RunMode.WebApp)
                {
                    return await RunWebApplicationAsync(args);
                }

                // For all other modes, use regular Host builder
                var builder = Host.CreateApplicationBuilder(args);
                HostConfigurationService.ConfigureHost(builder);

                // Create service provider for startup services
                using var startupServiceProvider = builder.Services.BuildServiceProvider();
                var consoleDisplayService = startupServiceProvider.GetRequiredService<ConsoleDisplayService>();
                var configValidationService = startupServiceProvider.GetRequiredService<ConfigurationValidationService>();

                // Show application header and logo
                consoleDisplayService.ShowApplicationHeader();
                consoleDisplayService.ShowLogo();

                // Log startup information
                Log.Information("------------------------------------------------------------------------------------------------------");
                Log.Information("------------------------------------ STARTING WORKER SERVICE -----------------------------------------");
                Log.Information("------------------------------------------------------------------------------------------------------");
                Log.Information("Running in {Mode} mode", options.Mode);

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

        private static async Task<int> RunWebApplicationAsync(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Configure host services
                HostConfigurationService.ConfigureWebHost(builder);

                // Validate configuration
                using var tempServiceProvider = builder.Services.BuildServiceProvider();
                var configValidationService = tempServiceProvider.GetRequiredService<ConfigurationValidationService>();
                
                if (!configValidationService.ValidateConfiguration(builder.Configuration))
                {
                    Log.Error("Configuration validation failed. Please check your configuration files.");
                    return 1;
                }

                Log.Information("------------------------------------------------------------------------------------------------------");
                Log.Information("------------------------------------ STARTING WEB APPLICATION ------------------------------------");
                Log.Information("------------------------------------------------------------------------------------------------------");
                Log.Information("Running in WebApp mode");

                // Register services for web application
                ServiceRegistrationService.RegisterWebServices(builder);

                Log.Information("Building Web Application");
                var app = builder.Build();

                // Configure the web application pipeline
                ServiceRegistrationService.ConfigureWebApplication(app);

                Log.Information("Starting Web Application on http://localhost:5000");
                Log.Information("Dashboard available at: http://localhost:5000");
                Log.Information("Health checks UI at: http://localhost:5000/health-ui");
                Log.Information("API documentation at: http://localhost:5000/api-docs");

                await app.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Web application terminated unexpectedly: {exception}", ex.Message);
                return 1;
            }
        }
    }
}
