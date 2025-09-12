using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using RGP.ExtensionAttributes.Automation.WorkerSvc.Jobs;
using RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils;
using RGP.ExtensionAttributes.Automation.WorkerSvc.HealthChecks;
using RGP.ExtensionAttributes.Automation.WorkerSvc.Services;
using AD.Helper.Config;
using Azure.Automation.Config;
using Azure.Automation.Intune.Config;
using AD.Automation;
using Azure.Automation;
using Azure.Automation.Authentication;
using Azure.Automation.Intune;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Quartz;
using Serilog;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Services
{
    public class ServiceRegistrationService
    {
        public static void RegisterServices(HostApplicationBuilder builder)
        {
            // Configure options
            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
            builder.Services.Configure<ADHelperSettings>(builder.Configuration.GetSection(nameof(ADHelperSettings)));
            builder.Services.Configure<EntraADHelperSettings>(builder.Configuration.GetSection(nameof(EntraADHelperSettings)));
            builder.Services.Configure<IntuneHelperSettings>(builder.Configuration.GetSection(nameof(IntuneHelperSettings)));
            builder.Services.Configure<NotificationSettings>(builder.Configuration.GetSection(nameof(NotificationSettings)));

            // Get configuration for service registration
            var entraADHelperSettings = builder.Configuration.GetSection(nameof(EntraADHelperSettings)).Get<EntraADHelperSettings>();
            var appSettings = builder.Configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
            
            if (entraADHelperSettings == null)
            {
                throw new InvalidOperationException("EntraADHelperSettings configuration is required.");
            }

            if (appSettings == null)
            {
                throw new InvalidOperationException("AppSettings configuration is required.");
            }

            // Register HTTP client for Graph API with Polly resilience policies
            builder.Services.AddHttpClient("GraphAPI", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(60); // Extended timeout for resilience
                client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                Credentials = CredentialCache.DefaultCredentials
            })
            .ConfigureHttpMessageHandlerBuilder(handlerBuilder =>
            {
                // Add logging for HTTP requests
                var loggerFactory = handlerBuilder.Services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("GraphAPI.HttpClient");
                
                // Apply Polly policies for resilience
                handlerBuilder.AdditionalHandlers.Add(new PolicyHandler(PollyPolicies.GetGraphApiPolicy(logger)));
            });

            // Register GraphServiceClient
            builder.Services.AddSingleton<GraphServiceClient>(provider =>
            {
                var options = new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                TokenCredential credential;
                
                if (entraADHelperSettings.UseClientSecret)
                {
                    if (string.IsNullOrWhiteSpace(entraADHelperSettings.ClientSecret))
                    {
                        throw new InvalidOperationException("ClientSecret is required when UseClientSecret is true.");
                    }

                    credential = new ClientSecretCredential(
                        entraADHelperSettings.TenantId, 
                        entraADHelperSettings.ClientId, 
                        entraADHelperSettings.ClientSecret, 
                        options);
                }
                else
                {
                    Log.Debug("Searching for certificate with thumbprint: {Thumbprint}", entraADHelperSettings.CertificateThumbprint);

                    using var store = new X509Store(StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    var certificate = store.Certificates
                        .Find(X509FindType.FindByThumbprint, entraADHelperSettings.CertificateThumbprint ?? string.Empty, validOnly: false)
                        .OfType<X509Certificate2>()
                        .FirstOrDefault();

                    if (certificate != null)
                    {
                        Log.Debug("Certificate found.");
                        credential = new ClientCertificateCredential(
                            entraADHelperSettings.TenantId, 
                            entraADHelperSettings.ClientId, 
                            certificate, 
                            options);
                    }
                    else
                    {
                        Log.Warning("Certificate not found.");
                        throw new InvalidOperationException($"Certificate with thumbprint {entraADHelperSettings.CertificateThumbprint} not found.");
                    }
                }

                // Create GraphServiceClient with TokenCredential
                return new GraphServiceClient(credential);
            });

            // Register helper services
            builder.Services.AddSingleton<IADHelper, ADHelper>();
            builder.Services.AddSingleton<IEntraADHelper, EntraADHelper>();
            builder.Services.AddSingleton<IIntuneHelper, IntuneHelper>();
            builder.Services.AddSingleton<AuthenticationHandler>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();
            
            // Register job utility services
            builder.Services.AddSingleton<IntuneExtensionAttributeHelper>();
            builder.Services.AddSingleton<UnifiedExtensionAttributeHelper>(); // NEW: Unified helper

            // Register Health Checks based on enabled data sources
            var healthChecksBuilder = builder.Services.AddHealthChecks()
                .AddCheck<ConfigurationHealthCheck>("configuration", HealthStatus.Unhealthy, ["config"]);

            // Add Entra AD health check (always needed for extension attributes)
            healthChecksBuilder.AddCheck<EntraADHealthCheck>("entraad", HealthStatus.Unhealthy, ["entraad", "graph"]);

            // Add Active Directory health check if enabled
            if (appSettings.DataSources.EnableActiveDirectory)
            {
                healthChecksBuilder.AddCheck<ActiveDirectoryHealthCheck>("activedirectory", HealthStatus.Unhealthy, ["ad"]);
            }

            // Add Intune health check if enabled
            if (appSettings.DataSources.EnableIntune)
            {
                healthChecksBuilder.AddCheck<IntuneHealthCheck>("intune", HealthStatus.Unhealthy, ["intune", "graph"]);
            }

            Log.Information("Registered health checks for enabled data sources - AD: {ADEnabled}, Intune: {IntuneEnabled}",
                appSettings.DataSources.EnableActiveDirectory, appSettings.DataSources.EnableIntune);
        }

        public static void ConfigureQuartz(HostApplicationBuilder builder)
        {
            // Add Quartz to the services collection
            builder.Services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));

            var quartzOptions = builder.Configuration.GetSection("Quartz:QuartzScheduler").Get<QuartzOptions>();

            if (quartzOptions == null)
            {
                Log.Error("Failed to load QuartzOptions from configuration.");
                throw new InvalidOperationException("QuartzOptions configuration is required.");
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
                    var triggerName = jobConfig["TriggerName"];
                    var triggerGroup = jobConfig["TriggerGroup"];
                    var cronExpression = jobConfig["CronExpression"];

                    if (string.IsNullOrEmpty(jobName))
                    {
                        Log.Error("JobName is null or empty in configuration.");
                        continue;
                    }

                    // Register different job types based on configuration
                    switch (jobName)
                    {
                        case nameof(SetComputerExtensionAttributeJob):
                            RegisterJob<SetComputerExtensionAttributeJob>(q, jobName, jobGroup, jobDescription, triggerName, triggerGroup, cronExpression);
                            break;
                        case nameof(SetIntuneExtensionAttributeJob):
                            RegisterJob<SetIntuneExtensionAttributeJob>(q, jobName, jobGroup, jobDescription, triggerName, triggerGroup, cronExpression);
                            break;
                        case nameof(SetUnifiedExtensionAttributeJob):
                            RegisterJob<SetUnifiedExtensionAttributeJob>(q, jobName, jobGroup, jobDescription, triggerName, triggerGroup, cronExpression);
                            break;
                        default:
                            Log.Warning("Unknown job type: {JobName}", jobName);
                            break;
                    }
                }
            });

            // Add Quartz.NET hosted service
            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            builder.Services.AddSingleton(provider => 
                provider.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);
        }

        private static void RegisterJob<T>(IServiceCollectionQuartzConfigurator q, string? jobName, string? jobGroup, 
            string? jobDescription, string? triggerName, string? triggerGroup, string? cronExpression) 
            where T : class, IJob
        {
            if (string.IsNullOrEmpty(jobName)) return;

            var jobKey = new JobKey(jobName, jobGroup);
            q.AddJob<T>(j => j
                .WithIdentity(jobKey)
                .WithDescription(jobDescription));

            if (!string.IsNullOrEmpty(triggerName) && !string.IsNullOrEmpty(cronExpression))
            {
                q.AddTrigger(t => t
                    .WithIdentity(triggerName, triggerGroup ?? "DefaultGroup")
                    .ForJob(jobKey)
                    .WithCronSchedule(cronExpression));

                Log.Information("Configured job: {jobName} with trigger: {triggerName}", jobName, triggerName);
                
                // Print the job schedule in human readable format
                try
                {
                    var cron = new CronExpression(cronExpression);
                    var nextFireTime = cron.GetNextValidTimeAfter(DateTimeOffset.Now);
                    Log.Information("Next fire time for {jobName} occurs at: {nextFireTime}", 
                        jobName, nextFireTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                catch (Exception ex)
                {
                    Log.Warning("Could not parse cron expression {cronExpression}: {error}", cronExpression, ex.Message);
                }
            }
        }

        public static void ConfigureWindowsService(HostApplicationBuilder builder)
        {
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "ExtensionAttributesWorkerSvc";
            });
        }
    }

    /// <summary>
    /// HTTP message handler that applies Polly policies
    /// </summary>
    public class PolicyHandler : DelegatingHandler
    {
        private readonly Polly.IAsyncPolicy<HttpResponseMessage> _policy;

        public PolicyHandler(Polly.IAsyncPolicy<HttpResponseMessage> policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await _policy.ExecuteAsync(async () =>
            {
                return await base.SendAsync(request, cancellationToken);
            });
        }
    }
}