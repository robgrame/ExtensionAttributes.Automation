using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Reflection;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Services
{
    public class HostConfigurationService
    {
        public static void ConfigureHost(HostApplicationBuilder builder)
        {
            var exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? string.Empty)!;

            // Add configuration files
            builder.Configuration.AddJsonFile(Path.Combine(exePath, "appsettings.json"), optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile(Path.Combine(exePath, "schedule.json"), optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile(Path.Combine(exePath, "logging.json"), optional: false, reloadOnChange: true);

            // Configure logging
            builder.Logging.ClearProviders();

            // Add Serilog to the builder
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            // Add Serilog to the logging pipeline
            builder.Logging.AddSerilog(Log.Logger);

            // Register application services
            builder.Services.AddSingleton<CommandLineService>();
            builder.Services.AddSingleton<ConsoleDisplayService>();
            builder.Services.AddSingleton<ConfigurationValidationService>();
        }
    }
}