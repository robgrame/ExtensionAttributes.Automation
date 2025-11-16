using Nimbus.ExtensionAttributes.WorkerSvc.JobUtils;
using Nimbus.ExtensionAttributes.WorkerSvc.Config;
using Nimbus.ExtensionAttributes.AD.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System.Runtime.Versioning;

namespace Nimbus.ExtensionAttributes.WorkerSvc.Jobs
{
    [DisallowConcurrentExecution]

    class SetComputerExtensionAttributeJob : IJob
    {
        private readonly ILogger<SetComputerExtensionAttributeJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        public SetComputerExtensionAttributeJob(IServiceProvider serviceProvider, ILogger<SetComputerExtensionAttributeJob> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

        }

        [SupportedOSPlatform("windows")]
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Job {jobname} started.", nameof(SetComputerExtensionAttributeJob));

                await ComputerExtensionAttributeHelper.SetExtensionAttributeAsync(_serviceProvider);

                // retrieve next fire time of the job from the context
                var nextFireTime = context.NextFireTimeUtc?.DateTime;
                if (nextFireTime.HasValue)
                {
                    _logger.LogInformation("Job {jobname} next fire time {firetime}:", nameof(SetComputerExtensionAttributeJob), nextFireTime.Value);
                }
                else
                {
                    _logger.LogWarning("Job {jobname} Next fire time is not available.", nameof(SetComputerExtensionAttributeJob));
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Job {jobname} was cancelled: {exception}", nameof(SetComputerExtensionAttributeJob), ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Job {jobname} was cancelled: {exception}", nameof(SetComputerExtensionAttributeJob), ex.Message);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                _logger.LogError("Job {jobname} failed with CryptographicException: {exception}", nameof(SetComputerExtensionAttributeJob), "Verify you have required permissions to acess certificate private key. It generally implies to have admin privileges on running machine.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {jobname} failed with exception: {exception}", nameof(SetComputerExtensionAttributeJob), ex.Message);
            }
        }
    }
}
