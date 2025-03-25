using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils
{
    internal class ExportHelper
    {
        private static readonly SemaphoreSlim _globalLock = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private static string? _currentFileName;
        private static readonly object _lock = new object();
        private static ILogger? _logger;

        public static SemaphoreSlim GlobalLock
        {
            get { return _globalLock; }
        }

        public static string GetCsvFileName(string filenaNamePrefix)
        {
            lock (_lock)
            {
                if (_currentFileName == null || !IsSameDay(_currentFileName, filenaNamePrefix))
                {
                    _currentFileName = $"{filenaNamePrefix}-{DateTime.Now:yyyyMMdd}.csv";
                }
                return _currentFileName;
            }
        }

        private static bool IsSameDay(string fileName,string fileNamePrefix)
        {
            var datePart = fileName.Substring(fileNamePrefix.Length, 8);

            return DateTime.TryParseExact(
                datePart,
                "yyyyMMdd",
                null,
                System.Globalization.DateTimeStyles.None,
                out var fileDate
            ) && fileDate.Date == DateTime.Now.Date;
        }

        public static async Task<bool> ExportDevicesToCsvAsync<T>(IServiceProvider serviceProvider, IEnumerable<T> deviceList, string fileName)
        {

            // Retrieve settings from DI container
            using var scope = serviceProvider.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger(nameof(ExportHelper));
            _logger.LogDebug("Starting to export Computer objects with updated extension attributes...");

            var appSettings = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

            if (deviceList == null || !deviceList.Any())
            {
                _logger.LogWarning("No devices to export. Device list is empty.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogError("File name is null or empty. Cannot export devices.");
                return false;
            }
            _logger.LogDebug("Exporting devices to CSV file path {csvFilePath}", appSettings.ExportPath);
            _logger.LogDebug("Exporting devices to CSV filename {fileName}", fileName);

            string filePath = Path.Combine(appSettings.ExportPath, fileName);
            _logger.LogDebug("Exporting devices to CSV file {csvfilepath}", filePath);

            await _semaphore.WaitAsync();

            try
            {
                bool pathExists = Directory.Exists(appSettings.ExportPath);
                _logger.LogDebug("Export path exists: {pathExists}", pathExists);

                if (!pathExists)
                {
                    _logger.LogError("Export path does not exist: {exportPath}. Tool is not able to export device entries. Please review required path", appSettings.ExportPath);
                    return false;
                }

                bool fileExists = File.Exists(filePath);
                _logger.LogDebug("File {filePath} exists: {fileExists}", filePath, fileExists);

                CsvConfiguration csvHelperConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = !fileExists,
                    IgnoreBlankLines = true,
                    TrimOptions = TrimOptions.Trim,
                    ShouldQuote = static (args) => true
                };

                _logger.LogDebug("Writing updated devices to CSV file {csvfilepath}", filePath);

                using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, csvHelperConfig, false))
                {
                    await csv.WriteRecordsAsync(deviceList);
                    csv.Flush();
                }

                _logger.LogDebug("CSV file created successfully at {FilePath}", filePath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while writing to CSV file. Make sure export path is available");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
