using Nimbus.ExtensionAttributes.WorkerSvc.Logging;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

namespace Nimbus.ExtensionAttributes.WorkerSvc.Services
{
    /// <summary>
    /// Enhanced logging configuration service with CMTrace support
    /// </summary>
    public static class LoggingService
    {
        /// <summary>
        /// Configure logging with CMTrace support
        /// </summary>
        public static void ConfigureLogging(IConfiguration configuration, string? componentName = null)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId();

            // Add component enricher if specified
            if (!string.IsNullOrEmpty(componentName))
            {
                loggerConfig.Enrich.WithComponent(componentName);
            }

            // Add CMTrace file sink programmatically for better control
            var logsPath = configuration["Logging:CMTrace:Path"] ?? "C:\\Temp\\Automation\\Logs";
            var applicationName = configuration["Logging:CMTrace:ApplicationName"] ?? "ExtensionAttributesWorker";
            var enableCMTrace = configuration.GetValue<bool>("Logging:CMTrace:Enabled", true);

            if (enableCMTrace)
            {
                // Ensure logs directory exists
                Directory.CreateDirectory(logsPath);

                // Add CMTrace formatted log file
                loggerConfig.WriteTo.CMTraceFile(
                    path: Path.Combine(logsPath, "ExtensionAttributes.CMTrace.log"),
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    applicationName: applicationName,
                    fileSizeLimitBytes: 20 * 1024 * 1024, // 20MB
                    retainedFileCountLimit: 10
                );

                Log.Information("CMTrace logging enabled - Output: {LogPath}", Path.Combine(logsPath, "ExtensionAttributes.CMTrace.log"));
            }

            Log.Logger = loggerConfig.CreateLogger();
        }

        /// <summary>
        /// Configure component-specific logging
        /// </summary>
        public static ILogger CreateComponentLogger<T>(string? componentName = null)
        {
            var actualComponentName = componentName ?? typeof(T).Name;
            
            return Log.ForContext("Component", actualComponentName)
                     .ForContext<T>();
        }

        /// <summary>
        /// Log system startup information in CMTrace-friendly format
        /// </summary>
        public static void LogStartupInformation(string mode)
        {
            Log.Information("=== Extension Attributes Automation Worker Service ===");
            Log.Information("Startup Mode: {Mode}", mode);
            Log.Information("Version: {Version}", GetApplicationVersion());
            Log.Information("Machine: {MachineName}", Environment.MachineName);
            Log.Information("OS: {OS}", Environment.OSVersion);
            Log.Information("Framework: {Framework}", Environment.Version);
            Log.Information("Process ID: {ProcessId}", Environment.ProcessId);
            Log.Information("Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
            Log.Information("Startup Time: {StartupTime}", DateTimeOffset.Now);
            Log.Information("=========================================================");
        }

        /// <summary>
        /// Log operation summary in structured format
        /// </summary>
        public static void LogOperationSummary(string operation, int processedCount, int successCount, int failureCount, TimeSpan duration)
        {
            Log.Information("=== Operation Summary: {Operation} ===", operation);
            Log.Information("Processed: {ProcessedCount} devices", processedCount);
            Log.Information("Successful: {SuccessCount} devices", successCount);
            Log.Information("Failed: {FailureCount} devices", failureCount);
            Log.Information("Success Rate: {SuccessRate:P1}", processedCount > 0 ? (double)successCount / processedCount : 0);
            Log.Information("Duration: {Duration}", duration);
            Log.Information("Rate: {Rate:F1} devices/minute", processedCount > 0 ? processedCount / duration.TotalMinutes : 0);
            Log.Information("=====================================");
        }

        private static string GetApplicationVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                return fileVersionInfo.FileVersion ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}