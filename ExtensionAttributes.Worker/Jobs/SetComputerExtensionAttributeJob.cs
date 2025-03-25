using RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils;
using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using AD.Helper.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Jobs
{
    [DisallowConcurrentExecution]
    class SetComputerExtensionAttributeJob : IJob
    {
        private readonly ILogger<SetComputerExtensionAttributeJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AppSettings _appSettings;
        private readonly ADHelperSettings _adHelperSettings;

        public SetComputerExtensionAttributeJob(IServiceProvider serviceProvider, ILogger<SetComputerExtensionAttributeJob> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogDebug("-----------------------------------------------------------------");
            _logger.LogDebug("--------- STARTING Set Computer Extension Attribute Job ---------");
            _logger.LogDebug("_________________________________________________________________");
            _logger.LogDebug("-------- Executing SetComputerExtensionAttributeJob ...----------");

            await ComputerExtensionAttributeHelper.SetExtensionAttributeAsync(_serviceProvider);

        }
    }
}
