using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System.Globalization;
using System.Text;

namespace Nimbus.ExtensionAttributes.WorkerSvc.Logging
{
    /// <summary>
    /// Custom Serilog formatter that outputs logs in CMTrace format
    /// Perfect for enterprise environments and System Center administrators
    /// </summary>
    public class CMTraceFormatter : ITextFormatter
    {
        private readonly string _applicationName;

        public CMTraceFormatter(string applicationName = "ExtensionAttributesWorker")
        {
            _applicationName = applicationName;
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            // Extract message
            var message = logEvent.RenderMessage();
            
            // Handle exceptions
            if (logEvent.Exception != null)
            {
                message += $" Exception: {logEvent.Exception}";
            }

            // Escape XML special characters in message
            message = EscapeXmlContent(message);

            // Determine CMTrace log type based on Serilog level
            var logType = GetCMTraceLogType(logEvent.Level);

            // Get timestamp components
            var timestamp = logEvent.Timestamp;
            var timeString = timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var dateString = timestamp.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
            var timezone = timestamp.ToString("zzz", CultureInfo.InvariantCulture).Replace(":", "");

            // Extract component name from source context or use default
            var component = GetComponentName(logEvent);

            // Get thread ID
            var threadId = Environment.CurrentManagedThreadId;

            // Get source file and line (if available)
            var sourceInfo = GetSourceInfo(logEvent);

            // Build CMTrace formatted log entry
            var logEntry = $"<![LOG[{message}]LOG]!>" +
                          $"<time=\"{timeString}{timezone}\" " +
                          $"date=\"{dateString}\" " +
                          $"component=\"{component}\" " +
                          $"context=\"\" " +
                          $"type=\"{logType}\" " +
                          $"thread=\"{threadId}\" " +
                          $"file=\"{sourceInfo}\">";

            output.WriteLine(logEntry);
        }

        private static string EscapeXmlContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            return content
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private static int GetCMTraceLogType(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => 1,    // Information
                LogEventLevel.Debug => 1,      // Information  
                LogEventLevel.Information => 1, // Information
                LogEventLevel.Warning => 2,    // Warning
                LogEventLevel.Error => 3,      // Error
                LogEventLevel.Fatal => 3,      // Error
                _ => 1
            };
        }

        private string GetComponentName(LogEvent logEvent)
        {
            // Try to get component from source context
            if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
            {
                var sourceContextValue = sourceContext.ToString().Trim('"');
                
                // Extract class name from full namespace
                var lastDot = sourceContextValue.LastIndexOf('.');
                if (lastDot >= 0 && lastDot < sourceContextValue.Length - 1)
                {
                    return sourceContextValue.Substring(lastDot + 1);
                }
                
                return sourceContextValue;
            }

            // Try to get from other properties
            if (logEvent.Properties.TryGetValue("Component", out var component))
            {
                return component.ToString().Trim('"');
            }

            return _applicationName;
        }

        private static string GetSourceInfo(LogEvent logEvent)
        {
            // Try to get source file information
            var sourceFile = "Unknown";
            var lineNumber = "0";

            if (logEvent.Properties.TryGetValue("SourceFilePath", out var filePath))
            {
                sourceFile = Path.GetFileName(filePath.ToString().Trim('"'));
            }

            if (logEvent.Properties.TryGetValue("SourceLineNumber", out var lineNum))
            {
                lineNumber = lineNum.ToString().Trim('"');
            }

            return $"{sourceFile}:{lineNumber}";
        }
    }

    /// <summary>
    /// Serilog enricher to add component name to log events
    /// </summary>
    public class ComponentEnricher : ILogEventEnricher
    {
        private readonly string _componentName;

        public ComponentEnricher(string componentName)
        {
            _componentName = componentName;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Component", _componentName));
        }
    }

    /// <summary>
    /// Extension methods for easy CMTrace configuration
    /// </summary>
    public static class CMTraceLoggerConfigurationExtensions
    {
        /// <summary>
        /// Write logs to file in CMTrace format
        /// </summary>
        public static LoggerConfiguration CMTraceFile(
            this LoggerSinkConfiguration sinkConfiguration,
            string path,
            LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
            string applicationName = "ExtensionAttributesWorker",
            long? fileSizeLimitBytes = 1024 * 1024 * 10, // 10MB default
            int retainedFileCountLimit = 5)
        {
            return sinkConfiguration.File(
                new CMTraceFormatter(applicationName),
                path,
                restrictedToMinimumLevel,
                fileSizeLimitBytes,
                retainedFileCountLimit: retainedFileCountLimit,
                rollOnFileSizeLimit: true,
                shared: true,
                encoding: Encoding.UTF8);
        }

        /// <summary>
        /// Add component enricher to logger
        /// </summary>
        public static LoggerConfiguration WithComponent(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            string componentName)
        {
            return enrichmentConfiguration.With(new ComponentEnricher(componentName));
        }
    }
}