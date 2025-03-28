using RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils;
using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using AD.Helper.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System.Runtime.Versioning;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Jobs
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
    }
}
