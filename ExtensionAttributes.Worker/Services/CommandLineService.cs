using Microsoft.Extensions.Logging;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Services
{
    public enum RunMode
    {
        Unknown,
        Service,
        Console,
        Device,
        Help
    }

    public class CommandLineOptions
    {
        public RunMode Mode { get; set; } = RunMode.Unknown;
        public string? DeviceName { get; set; }
        public bool ShowHelp { get; set; }
    }

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

            if (args == null || args.Length == 0)
            {
                options.ShowHelp = true;
                return options;
            }

            var firstArg = args[0].ToLowerInvariant();

            switch (firstArg)
            {
                case "--service" or "-s":
                    options.Mode = RunMode.Service;
                    break;
                case "--console" or "-c":
                    options.Mode = RunMode.Console;
                    break;
                case "--device" or "-d":
                    options.Mode = RunMode.Device;
                    if (args.Length > 1)
                    {
                        options.DeviceName = args[1];
                    }
                    break;
                case "--help" or "-h":
                    options.Mode = RunMode.Help;
                    options.ShowHelp = true;
                    break;
                default:
                    options.ShowHelp = true;
                    _logger.LogWarning("Invalid argument: {Argument}", firstArg);
                    break;
            }

            return options;
        }

        public void ShowUsage()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Usage: ExtensionAttributes.WorkerSvc.exe [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --service, -s       Run the worker service as a Windows Service");
            Console.WriteLine("  --console, -c       Run the worker service as a console application");
            Console.WriteLine("  --device, -d -computername [hostname] [TBD]  Run for a specific device");
            Console.WriteLine("  --help, -h         Show this help message");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
        }
    }
}