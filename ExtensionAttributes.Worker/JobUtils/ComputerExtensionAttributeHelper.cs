using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using AD.Automation;
using Azure.Automation;
using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using System.Runtime.Versioning;
using AD.Helper.Config;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils
{
    public static class ComputerExtensionAttributeHelper
    {
        /// <summary>
        /// Sets the extension attribute for all computers in the specified OU based on the parent OU name.
        /// This method is specifically for Active Directory-based extension attributes.
        /// </summary>
        /// <param name="serviceProvider">The service provider to retrieve services from.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SupportedOSPlatform("windows")]
        public static async Task SetExtensionAttributeAsync(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(ComputerExtensionAttributeHelper));
            var adHelper = serviceProvider.GetRequiredService<IADHelper>();
            var entraADHelper = serviceProvider.GetRequiredService<IEntraADHelper>();
            var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
            var adHelperSettings = serviceProvider.GetRequiredService<IOptions<ADHelperSettings>>().Value;

            try
            {
                logger.LogInformation("Starting Legacy AD-only extension attribute processing");

                // Filter only Active Directory mappings for backward compatibility
                var adMappings = appSettings.ExtensionAttributeMappings
                    .Where(m => m.DataSource == DataSourceType.ActiveDirectory)
                    .ToList();

                if (!adMappings.Any())
                {
                    logger.LogWarning("No Active Directory extension attribute mappings found");
                    return;
                }

                logger.LogInformation("Found {MappingCount} Active Directory extension attribute mappings", adMappings.Count);

                // Get all computers from the root OU
                var computers = adHelper.GetDirectoryEntriesAsyncEnumerable(adHelperSettings.RootOrganizationaUnitDN);
                
                int processedComputers = 0;
                int successfulUpdates = 0;

                await foreach (var directoryEntry in computers)
                {
                    try
                    {
                        var computerName = directoryEntry.Name?.Replace("CN=", "");
                        if (string.IsNullOrEmpty(computerName))
                        {
                            logger.LogWarning("Computer name is null or empty for {DirectoryEntry}", directoryEntry.Path);
                            continue;
                        }

                        logger.LogTrace("Processing computer: {ComputerName}", computerName);

                        // Get the Entra AD device by name
                        var entraDevice = await entraADHelper.GetDeviceByNameAsync(computerName);
                        if (entraDevice == null)
                        {
                            logger.LogWarning("Entra AD device not found for computer: {ComputerName}", computerName);
                            continue;
                        }

                        logger.LogDebug("Found Entra AD device for computer: {ComputerName} with DeviceId: {DeviceId}", computerName, entraDevice.DeviceId);

                        // Process each Active Directory mapping
                        foreach (var mapping in adMappings)
                        {
                            await ProcessADMapping(directoryEntry, entraDevice, mapping, adHelper, entraADHelper, logger);
                        }

                        processedComputers++;
                        successfulUpdates++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing computer: {DirectoryEntry}", directoryEntry.Path);
                    }
                }

                logger.LogInformation("Legacy AD processing completed. Processed {ProcessedComputers} computers with {SuccessfulUpdates} successful updates", 
                    processedComputers, successfulUpdates);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SetExtensionAttributeAsync: {Error}", ex.Message);
                throw;
            }
        }

        private static async Task ProcessADMapping(
            System.DirectoryServices.DirectoryEntry directoryEntry,
            Microsoft.Graph.Models.Device entraDevice,
            ExtensionAttributeMapping mapping,
            IADHelper adHelper,
            IEntraADHelper entraADHelper,
            ILogger logger)
        {
            try
            {
                var computerName = directoryEntry.Name?.Replace("CN=", "");
                var distinguishedName = directoryEntry.Path;

                logger.LogDebug("Retrieving SourceAttribute {SourceAttribute} for Computer name: {ComputerName}", mapping.SourceAttribute, computerName);
                var currentComputerAttributeValue = await adHelper.GetComputerAttributeAsync(distinguishedName, mapping.SourceAttribute);
                logger.LogInformation("Retrieved SourceAttribute {SourceAttribute} with value: {ComputerAttributeValue} for Computer name: {ComputerName}", mapping.SourceAttribute, currentComputerAttributeValue, computerName);

                if (string.IsNullOrEmpty(currentComputerAttributeValue))
                {
                    logger.LogWarning("SourceAttribute {SourceAttribute} is null or empty for {ComputerName}. Using default value for {extensionAttribute}", 
                        mapping.SourceAttribute, directoryEntry.Name, mapping.ExtensionAttribute);
                    currentComputerAttributeValue = mapping.DefaultValue ?? string.Empty;
                }

                logger.LogDebug("Retrieved Computer attribute {SourceAttribute} with value: {ComputerAttributeValue}", mapping.SourceAttribute, currentComputerAttributeValue);

                string extractedValue = currentComputerAttributeValue;

                // Apply regex if specified
                if (!string.IsNullOrEmpty(mapping.Regex))
                {
                    logger.LogTrace("Applying regex {regex} to SourceAttribute: {SourceAttribute}", mapping.Regex, mapping.SourceAttribute);

                    var regex = new Regex(mapping.Regex);
                    var match = regex.Match(currentComputerAttributeValue ?? string.Empty);

                    if (match.Success)
                    {
                        extractedValue = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                        logger.LogDebug("Regex matched. Extracted value: {ExtractedValue}", extractedValue);
                    }
                    else
                    {
                        logger.LogWarning("Regex did not match any value in SourceAttribute: {SourceAttribute}", mapping.SourceAttribute);
                        extractedValue = mapping.DefaultValue ?? string.Empty;
                    }
                }
                else
                {
                    logger.LogDebug("No regex applied to SourceAttribute: {SourceAttribute}", mapping.SourceAttribute);
                }

                // Get current extension attribute value
                var currentExtensionAttributeValue = await entraADHelper.GetExtensionAttribute(entraDevice.DeviceId!, mapping.ExtensionAttribute);
                logger.LogDebug("Current extension attribute value for {ExtensionAttribute}: {CurrentValue}", mapping.ExtensionAttribute, currentExtensionAttributeValue);

                // Only update if values are different
                if (!string.Equals(currentExtensionAttributeValue, extractedValue, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("Updating {ExtensionAttribute} for computer {ComputerName}: '{OldValue}' -> '{NewValue}'",
                        mapping.ExtensionAttribute, computerName, currentExtensionAttributeValue ?? "null", extractedValue);

                    var result = await entraADHelper.SetExtensionAttributeValue(entraDevice.DeviceId!, mapping.ExtensionAttribute, extractedValue);

                    if (!string.IsNullOrEmpty(result))
                    {
                        logger.LogInformation("Successfully updated {ExtensionAttribute} for computer {ComputerName}", mapping.ExtensionAttribute, computerName);
                    }
                    else
                    {
                        logger.LogError("Failed to update {ExtensionAttribute} for computer {ComputerName}", mapping.ExtensionAttribute, computerName);
                    }
                }
                else
                {
                    logger.LogDebug("No update needed for {ExtensionAttribute} on computer {ComputerName}. Values are the same.", mapping.ExtensionAttribute, computerName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing AD mapping for {ExtensionAttribute}: {Error}", mapping.ExtensionAttribute, ex.Message);
            }
        }
    }
}
