using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Nimbus.ExtensionAttributes.WorkerSvc.JobUtils;
using System.Runtime.Versioning;

namespace Nimbus.ExtensionAttributes.WorkerSvc.Jobs
{
    [DisallowConcurrentExecution]
    public class SetIntuneExtensionAttributeJob : IJob
    {
        private readonly ILogger<SetIntuneExtensionAttributeJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SetIntuneExtensionAttributeJob(IServiceProvider serviceProvider, ILogger<SetIntuneExtensionAttributeJob> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [SupportedOSPlatform("windows")]
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job {JobName} started.", nameof(SetIntuneExtensionAttributeJob));

                // Get the IntuneExtensionAttributeHelper from the service provider
                var intuneHelper = _serviceProvider.GetRequiredService<IntuneExtensionAttributeHelper>();

                // Process Intune-based extension attributes
                var processedCount = await intuneHelper.ProcessIntuneBasedExtensionAttributesAsync();
                
                _logger.LogInformation("Job {JobName} completed. Processed {ProcessedCount} devices.", 
                    nameof(SetIntuneExtensionAttributeJob), processedCount);

                // Retrieve next fire time of the job from the context
                var nextFireTime = context.NextFireTimeUtc?.DateTime;
                if (nextFireTime.HasValue)
                {
                    _logger.LogInformation("Job {JobName} next fire time: {FireTime}", 
                        nameof(SetIntuneExtensionAttributeJob), nextFireTime.Value);
                }
                else
                {
                    _logger.LogWarning("Job {JobName} next fire time is not available.", nameof(SetIntuneExtensionAttributeJob));
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Job {JobName} was cancelled: {Exception}", nameof(SetIntuneExtensionAttributeJob), ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Job {JobName} was cancelled: {Exception}", nameof(SetIntuneExtensionAttributeJob), ex.Message);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                _logger.LogError(ex, "Job {JobName} failed with CryptographicException: {Exception}. Verify you have required permissions to access certificate private key. This generally requires admin privileges on running machine.", 
                    nameof(SetIntuneExtensionAttributeJob), ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobName} failed with exception: {Exception}", nameof(SetIntuneExtensionAttributeJob), ex.Message);
            }
        }
    }
}