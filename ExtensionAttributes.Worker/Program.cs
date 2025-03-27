
using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using RGP.ExtensionAttributes.Automation.WorkerSvc.Jobs;
using RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils;
using AD.Automation;
using AD.Helper.Config;
using Azure.Automation;
using Azure.Automation.Authentication;
using Azure.Automation.Config;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Quartz;
using Quartz.Util;
using Serilog;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.Cosh;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            bool isService = false;
            bool isConsole = false;
            bool isDevice = false;         

            try
            {
                var builder = Host.CreateApplicationBuilder(args);

                var exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? string.Empty)!;

                builder.Configuration.AddJsonFile(Path.Combine(exePath, "appsettings.json"), optional: false, reloadOnChange: true);
                builder.Configuration.AddJsonFile(Path.Combine(exePath, "schedule.json"), optional: false, reloadOnChange: true);
                builder.Configuration.AddJsonFile(Path.Combine(exePath, "logging.json"), optional: false, reloadOnChange: true);

                builder.Logging.ClearProviders();

                // Set the default console color to blue
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.BackgroundColor = ConsoleColor.Black;

                Console.WriteLine("RGP Extension Attributes Automation Worker Service", Console.ForegroundColor = ConsoleColor.Green);
                Console.WriteLine("Version: {0}", System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly()?.Location ?? string.Empty).FileVersion?.ToString() ?? "Unknown version");
                Console.WriteLine("Copyright (c) 2025 RGP Bytes");
                Console.WriteLine("All rights reserved.");
                Console.WriteLine();
                Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);

                if (null == args || args.Length == 0)
                {
                    isService = false;
                    isConsole = false;
                    isDevice = false;

                    Console.WriteLine("Usage: ExtensionAttributes.WorkerSvc.exe [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --service, -s       Run the worker service as a Windows Service");
                    Console.WriteLine("  --console, -c       Run the worker service as a console application");
                    Console.WriteLine("  --device, -d -computername [hostname] [TBD]  Run for a specific device");
                    Console.WriteLine("  --help, -h         Show this help message");
                    Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);
                    return;
                }
                // check if args contains --schedule
                if (args[0].Contains("--service") || args[0].Contains("-s"))
                {
                    isService = true;
                }
                else if (args[0].Contains("--console") || args[0].Contains("-c"))
                {
                    isConsole = true;
                }
                else if (args[0].Contains("--device") || args[0].Contains("-d"))
                {
                    isDevice = true;
                }
                else if (args[0].Contains("--help") || args[0].Contains("-h"))
                {
                    isService = false;
                    isConsole = false;
                    isDevice = false;
                    Console.WriteLine("Usage: ExtensionAttributes.WorkerSvc.exe [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --service, -s       Run the worker service as a Windows Service");
                    Console.WriteLine("  --console, -c       Run the worker service as a console application");
                    Console.WriteLine("  --device, -d -computername [hostname] [TBD]  Run for a specific device");
                    Console.WriteLine("  --help, -h         Show this help message");
                    Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);
                    return;
                }
                else if (args[0].TrimEmptyToNull == null || args.Length == 0)
                {
                    isService = false;
                    isConsole = false;
                    isDevice = false;
                    Console.WriteLine("Usage: ExtensionAttributes.WorkerSvc.exe [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --service, -s       Run the worker service as a Windows Service");
                    Console.WriteLine("  --console, -c       Run the worker service as a console application");
                    Console.WriteLine("  --device, -d -computername [hostname] [TBD]  Run for a specific device");
                    Console.WriteLine("  --help, -h         Show this help message");
                    Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);
                    return;
                }
                else
                {
                    Console.WriteLine("Invalid argument. Use --help or -h for usage information.");
                    Console.WriteLine("");
                    Console.WriteLine("Usage: ExtensionAttributes.WorkerSvc.exe [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --service, -s       Run the worker service as a Windows Service");
                    Console.WriteLine("  --console, -c       Run the worker service as a console application");
                    Console.WriteLine("  --device, -d -computername [hostname] [TBD]  Run for a specific device");
                    Console.WriteLine("  --help, -h         Show this help message");
                    Console.WriteLine("------------------------------------------------------------------------------------------------------", Console.ForegroundColor = ConsoleColor.White);
                    return;
                }


                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;

                // Add Serilog to the builder
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(builder.Configuration)
                    .CreateLogger();

                // Add Serilog to the logging pipeline
                builder.Logging.AddSerilog(Log.Logger);

                string logo = File.ReadAllText(Path.Combine(exePath, "logo.txt"));
                string[] logoLines = File.ReadAllLines(Path.Combine(exePath, "logo.txt"));

                // Colorize the logo based on letter. A, C, I will be blue, rest will be dark gray. If space, colorize with white
                for (int i = 0; i < logoLines.Length; i++)
                {
                    for (int j = 0; j < logoLines[i].Length; j++)
                    {
                        if (logoLines[i][j] == 'A' || logoLines[i][j] == 'C' || logoLines[i][j] == 'I')
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                        }
                        else if (logoLines[i][j] == ' ')
                        {                            
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                        }
                        Console.Write(logoLines[i][j]);
                    }
                    Console.WriteLine();
                }

                Console.ResetColor();



                Log.Information("------------------------------------------------------------------------------------------------------");
                Log.Information("------------------------------------ STARTING WORKER SERVICE -----------------------------------------");
                Log.Information("------------------------------------------------------------------------------------------------------");


                builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings))); //IOptions
                builder.Services.Configure<ADHelperSettings>(builder.Configuration.GetSection(nameof(ADHelperSettings)));
                builder.Services.Configure<EntraADHelperSettings>(builder.Configuration.GetSection(nameof(EntraADHelperSettings)));

                var settings = builder.Configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
                var adHelperSettings = builder.Configuration.GetSection(nameof(ADHelperSettings)).Get<ADHelperSettings>();
                var entraADHelperSettings = builder.Configuration.GetSection(nameof(EntraADHelperSettings)).Get<EntraADHelperSettings>();

                // Check for null settings
                if (settings == null)
                {
                    Log.Error("Failed to load AppSettings from configuration.");
                    return;
                }
                else
                {
                    Log.Debug("AppSettings loaded successfully");
                }

                // Print out the Worker Service Assembly name and version
                string assemblyVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly()?.Location ?? string.Empty).FileVersion?.ToString() ?? "Unknown version";

                Log.Information("Worker Service Assembly: {AssemblyName} with version: {version}", Assembly.GetExecutingAssembly().GetName().Name, assemblyVersion);
                Log.Information("Worker Service arguments: {args}", string.Join(" ", args));
                // Print out the AppSettings values
                Log.Information("ClientId: {ClientId}", entraADHelperSettings?.ClientId ?? "ClientID not provided");
                Log.Information("TenantId: {TenantId}", entraADHelperSettings?.TenantId ?? "TenantId not provided");
                Log.Information("UseClientSecret: {UseClient} ", entraADHelperSettings?.UseClientSecret);
                Log.Information("ClientSecret: {ClientSecret}", entraADHelperSettings?.ClientSecret == null ? "ClientSecret not provided" : "**********");
                Log.Information("CertificateThumbprint: {CertificateThumbprint}", entraADHelperSettings?.CertificateThumbprint ?? "CertificateThumbprint not provided");

                Log.Information("RootOrganizationaUnitDN: {RootOrganizationaUnitDN}", adHelperSettings.RootOrganizationaUnitDN == null ? "RootOrganizationaUnitDN not provided" : adHelperSettings.RootOrganizationaUnitDN);

                Log.Information("ExtensionAttributeMappings: {ExtensionAttributeMappings}", string.Join(", ", settings.ExtensionAttributeMappings.Select(m => m.ToString())));
                Log.Information("ExcludedOUs: {ExcludedOUs}", adHelperSettings.ExcludedOUs);
                Log.Information("ExportPath: {ExportPath}", settings.ExportPath);
                Log.Information("ExportFileNamePrefix: {ExportFileNamePrefix}", settings.ExportFileNamePrefix);

                // Register a HTTP client for Graph API
                builder.Services.AddHttpClient("GraphAPI", client =>
                {
                    // Add the Accept header to the HttpClient
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromSeconds(30);

                    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"); // Set the base address for the HttpClient              
                
                }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    Credentials = CredentialCache.DefaultCredentials
                });


                builder.Services.AddSingleton<GraphServiceClient>(provider =>
                {
                    var options = new TokenCredentialOptions
                    {
                        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                    };

                    if (entraADHelperSettings.UseClientSecret)
                    {
                        return new GraphServiceClient(new ClientSecretCredential(entraADHelperSettings.TenantId, entraADHelperSettings.ClientId, entraADHelperSettings.ClientSecret, options));
                    }
                    else
                    {
                        Log.Debug("Searching for certificate with thumbprint: {Thumbprint}", entraADHelperSettings.CertificateThumbprint);

                        using var store = new X509Store(StoreLocation.LocalMachine);
                        store.Open(OpenFlags.ReadOnly);
                        var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, entraADHelperSettings.CertificateThumbprint ?? string.Empty, validOnly: false).OfType<X509Certificate2>().FirstOrDefault();

                        if (certificate != null)
                        {
                            Log.Debug("Certificate found.");
                        }
                        else
                        {
                            Log.Warning("Certificate not found.");
                            return null;
                        }
         
                        return new GraphServiceClient(new ClientCertificateCredential(entraADHelperSettings.TenantId, entraADHelperSettings.ClientId, certificate, options));
                    }
                });

                builder.Services.AddSingleton<IADHelper, ADHelper>();
                builder.Services.AddSingleton<IEntraADHelper, EntraADHelper>();
                builder.Services.AddSingleton<AuthenticationHandler>();

                #region Quartz Configuration

                // check if args contains --schedule
                if (isService)
                {
                    // Add Quartz to the services collection
                    builder.Services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));

                    var quartzOptions = builder.Configuration.GetSection("Quartz:QuartzScheduler").Get<QuartzOptions>();

                    if (quartzOptions == null)
                    {
                        Log.Error("Failed to load QuartzOptions from configuration.");
                        return;
                    }
                    else
                    {
                        Log.Debug("Successfully loaded QuartzOptions");
                    }

                    // Add Quartz services
                    builder.Services.AddQuartz(q =>
                    {
                        // Configure jobs and triggers from configuration
                        var jobs = builder.Configuration.GetSection("Quartz:QuartzJobs").GetChildren();

                        foreach (var jobConfig in jobs)
                        {
                            var jobName = jobConfig["JobName"];
                            var jobGroup = jobConfig["JobGroup"];
                            var jobDescription = jobConfig["JobDescription"];
                            var jobType = jobConfig["JobType"];

                            if (string.IsNullOrEmpty(jobName))
                            {
                                Log.Error("JobName is null or empty in configuration.");
                                continue;
                            }

                            if (jobName == nameof(SetComputerExtensionAttributeJob))
                            {
                                var jobKey = new JobKey(jobName, jobGroup);
                                q.AddJob<SetComputerExtensionAttributeJob>(j => j
                                    .WithIdentity(jobKey)
                                    .WithDescription(jobDescription));

                                q.AddTrigger(t => t
                                    .WithIdentity(jobConfig["TriggerName"], jobConfig["TriggerGroup"])
                                    .ForJob(jobKey)
                                    .WithCronSchedule(jobConfig["CronExpression"]));

                                Log.Information("Configured job: {jobName} with trigger: {triggerName}", jobName, jobConfig["TriggerName"]);
                                // print the job schedule in human readable format
                                var cronExpression = jobConfig["CronExpression"];
                                var cron = new CronExpression(cronExpression);
                                var nextFireTime = cron.GetNextValidTimeAfter(DateTimeOffset.Now);

                                var humanReadableSchedule = $"Next fire time for {jobName} is: {nextFireTime?.ToString("yyyy-MM-dd HH:mm:ss")}";
                                Log.Information(humanReadableSchedule);

                            }
                        }
                    });

                    // Add Quartz.NET hosted service
                    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

                    builder.Services.AddSingleton(provider => provider.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);

                    #endregion

                    // Add Windows Service
                    builder.Services.AddWindowsService(options =>
                    {
                        options.ServiceName = "ExtensionAttributesWorkerSvc";
                    });

                    // Build the host
                    Log.Information("Building Worker Service host");
                    var host = builder.Build();


                    // Run the host
                    Log.Information("Starting Working Service");
                    await host.RunAsync();

                    return;

                }
                else if (isDevice)
                {
                    var deviceName = args[1];

                    Log.Information("Running Worker Service for device: {deviceName}", deviceName);
                    Log.Error("Unfortunately this scenario is not yet implememted");
                    return;
                }
                else if (isConsole)
                {
                    // Create a service provider
                    var serviceProvider = builder.Services.BuildServiceProvider();

                    // Get the required services
                    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                    // Call the static method
                    if (OperatingSystem.IsWindows())
                    {
                        await ComputerExtensionAttributeHelper.SetExtensionAttributeAsync(serviceProvider);
                    }
                    else
                    {
                        logger.LogError("The SetExtensionAttributeAsync method is only supported on Windows.");
                    }

                    return;
                }

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Worker Service terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
