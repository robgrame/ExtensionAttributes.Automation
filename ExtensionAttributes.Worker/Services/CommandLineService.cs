using Microsoft.Extensions.Logging;

namespace ExtensionAttributes.Automation.WorkerSvc.Services
{
    public class CommandLineService
    {
        private readonly ILogger<CommandLineService> _logger;

        public CommandLineService(ILogger<CommandLineService> logger)
        {
            _logger = logger;
        }

        public CommandLineOptions ParseArguments(string[] args)
        {
            var options = new CommandLineOptions();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();

                switch (arg)
                {
                    case "--help":
                    case "-h":
                        options.ShowHelp = true;
                        break;

                    case "--service":
                    case "-s":
                        options.Mode = RunMode.Service;
                        break;

                    case "--console":
                    case "-c":
                        options.Mode = RunMode.Console;
                        break;

                    case "--web":
                    case "-w":
                    case "--dashboard":
                        options.Mode = RunMode.WebApp;
                        break;

                    case "--device":
                    case "-d":
                    case "--computername":
                        options.Mode = RunMode.Device;
                        // Get the next argument as device name
                        if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                        {
                            options.DeviceName = args[i + 1];
                            i++; // Skip next argument since we consumed it
                        }
                        else
                        {
                            _logger.LogError("Device name must be provided after --device option");
                            options.ShowHelp = true;
                        }
                        break;

                    case "--deviceid":
                        options.Mode = RunMode.DeviceById;
                        // Get the next argument as device ID
                        if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                        {
                            options.DeviceId = args[i + 1];
                            i++; // Skip next argument since we consumed it
                        }
                        else
                        {
                            _logger.LogError("Device ID must be provided after --deviceid option");
                            options.ShowHelp = true;
                        }
                        break;

                    default:
                        // If we're in device mode and haven't set a device name yet, treat this as the device name
                        if (options.Mode == RunMode.Device && string.IsNullOrEmpty(options.DeviceName) && !arg.StartsWith('-'))
                        {
                            options.DeviceName = args[i];
                        }
                        else if (options.Mode == RunMode.DeviceById && string.IsNullOrEmpty(options.DeviceId) && !arg.StartsWith('-'))
                        {
                            options.DeviceId = args[i];
                        }
                        else if (!arg.StartsWith('-'))
                        {
                            _logger.LogWarning("Unknown argument: {Argument}", args[i]);
                        }
                        break;
                }
            }

            // Validate device name/ID if in device mode
            if (options.Mode == RunMode.Device && string.IsNullOrEmpty(options.DeviceName))
            {
                _logger.LogError("Device name is required when using --device option");
                options.ShowHelp = true;
            }

            if (options.Mode == RunMode.DeviceById && string.IsNullOrEmpty(options.DeviceId))
            {
                _logger.LogError("Device ID is required when using --deviceid option");
                options.ShowHelp = true;
            }

            return options;
        }

        public void ShowUsage()
        {
            Console.WriteLine("Usage: ExtensionAttributes.WorkerSvc.exe [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --service, -s                    Run the worker service as a Windows Service");
            Console.WriteLine("  --console, -c                    Run the worker service as a console application");
            Console.WriteLine("  --web, -w, --dashboard           Run with web dashboard and REST API");
            Console.WriteLine("  --device, -d [device-name]       Process extension attributes for a specific device by name");
            Console.WriteLine("  --deviceid [device-id]           Process extension attributes for a specific device by Entra AD Device ID");
            Console.WriteLine("  --help, -h                       Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  ExtensionAttributes.WorkerSvc.exe --console");
            Console.WriteLine("  ExtensionAttributes.WorkerSvc.exe --web");
            Console.WriteLine("  ExtensionAttributes.WorkerSvc.exe --device DESKTOP-ABC123");
            Console.WriteLine("  ExtensionAttributes.WorkerSvc.exe --deviceid \"abc123-def456-ghi789\"");
            Console.WriteLine("  ExtensionAttributes.WorkerSvc.exe --service");
            Console.WriteLine();
            Console.WriteLine("Web Dashboard:");
            Console.WriteLine("  When using --web mode, the following endpoints are available:");
            Console.WriteLine("  • Dashboard:          http://localhost:5000");
            Console.WriteLine("  • Health Checks UI:   http://localhost:5000/health-ui");
            Console.WriteLine("  • API Documentation:  http://localhost:5000/api-docs");
            Console.WriteLine("  • REST API:           http://localhost:5000/api/status/*");
        }
    }

    public class CommandLineOptions
    {
        public RunMode Mode { get; set; } = RunMode.Console;
        public bool ShowHelp { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceId { get; set; }
    }

    public enum RunMode
    {
        Console,
        Service,
        Device,
        DeviceById,
        WebApp
    }
}