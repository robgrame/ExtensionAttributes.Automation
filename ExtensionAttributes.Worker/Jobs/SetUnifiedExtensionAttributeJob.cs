using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using ExtensionAttributes.Automation.WorkerSvc.JobUtils;
using System.Runtime.Versioning;

namespace ExtensionAttributes.Automation.WorkerSvc.Jobs
{
    [DisallowConcurrentExecution]
    public class SetUnifiedExtensionAttributeJob : IJob
    {
        private readonly ILogger<SetUnifiedExtensionAttributeJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SetUnifiedExtensionAttributeJob(IServiceProvider serviceProvider, ILogger<SetUnifiedExtensionAttributeJob> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [SupportedOSPlatform("windows")]
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job {JobName} started.", nameof(SetUnifiedExtensionAttributeJob));

                // Get the UnifiedExtensionAttributeHelper from the service provider
                var unifiedHelper = _serviceProvider.GetRequiredService<UnifiedExtensionAttributeHelper>();

                // Process extension attributes from all enabled data sources
                var processedCount = await unifiedHelper.ProcessExtensionAttributesAsync();
                
                _logger.LogInformation("Job {JobName} completed. Processed {ProcessedCount} devices.", 
                    nameof(SetUnifiedExtensionAttributeJob), processedCount);

                // Retrieve next fire time of the job from the context
                var nextFireTime = context.NextFireTimeUtc?.DateTime;
                if (nextFireTime.HasValue)
                {
                    _logger.LogInformation("Job {JobName} next fire time: {FireTime}", 
                        nameof(SetUnifiedExtensionAttributeJob), nextFireTime.Value);
                }
                else
                {
                    _logger.LogWarning("Job {JobName} next fire time is not available.", nameof(SetUnifiedExtensionAttributeJob));
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Job {JobName} was cancelled: {Exception}", nameof(SetUnifiedExtensionAttributeJob), ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Job {JobName} was cancelled: {Exception}", nameof(SetUnifiedExtensionAttributeJob), ex.Message);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                _logger.LogError(ex, "Job {JobName} failed with CryptographicException: {Exception}. Verify you have required permissions to access certificate private key. This generally requires admin privileges on running machine.", 
                    nameof(SetUnifiedExtensionAttributeJob), ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobName} failed with exception: {Exception}", nameof(SetUnifiedExtensionAttributeJob), ex.Message);
            }
        }
    }
}