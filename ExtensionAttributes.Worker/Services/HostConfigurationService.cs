using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace ExtensionAttributes.Automation.WorkerSvc.Services
{
    public class HostConfigurationService
    {
        public static void ConfigureHost(HostApplicationBuilder builder)
        {
            ConfigureLogging(builder.Configuration);
            RegisterEarlyServices(builder.Services);
        }

        public static void ConfigureWebHost(WebApplicationBuilder builder)
        {
            ConfigureLogging(builder.Configuration);
            RegisterEarlyServices(builder.Services);
        }

        private static void ConfigureLogging(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            // Log startup information
            Log.Information("Serilog configured successfully");
            Log.Information("Application starting at: {Timestamp}", DateTimeOffset.Now);
            Log.Information("Machine Name: {MachineName}", Environment.MachineName);
            Log.Information("Operating System: {OS}", Environment.OSVersion);
            Log.Information("Framework Version: {Framework}", Environment.Version);
            Log.Information("Process ID: {ProcessId}", Environment.ProcessId);
        }

        private static void RegisterEarlyServices(IServiceCollection services)
        {
            // Register services needed for configuration validation and command line parsing
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
            services.AddSingleton<ConsoleDisplayService>();
            services.AddSingleton<CommandLineService>();
            services.AddSingleton<ConfigurationValidationService>();
        }
    }
}