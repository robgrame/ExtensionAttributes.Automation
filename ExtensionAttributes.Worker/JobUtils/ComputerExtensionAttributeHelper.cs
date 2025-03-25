using RGP.ExtensionAttributes.Automation.WorkerSvc.Config;
using AD.Automation;
using AD.Helper.Config;
using Azure.Automation;
using Azure.Automation.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Quartz.Util;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;


namespace RGP.ExtensionAttributes.Automation.WorkerSvc.JobUtils
{
    public static class ComputerExtensionAttributeHelper
    {
        // Static collection to keep track of updated devices
        private static readonly ConcurrentBag<Tuple<Device, string>> UpdatedDevices = new ConcurrentBag<Tuple<Device, string>>();
        private static ILogger? _logger;


        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static async Task SetExtensionAttributeAsync(IServiceProvider serviceProvider)
        {

            // Retrieve settings from DI container
            using var scope = serviceProvider.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger("ComputerExtensionAttributeHelper");

            _logger.LogDebug("-----------------------------------------------------------------");
            _logger.LogDebug("Starting Set Computer Extension Attribute Job...");
            _logger.LogDebug("-----------------------------------------------------------------");
            var appSettings = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
            var adHelperSettings = scope.ServiceProvider.GetRequiredService<IOptions<ADHelperSettings>>().Value;
            var entraADHelperSettings = scope.ServiceProvider.GetRequiredService<IOptions<EntraADHelperSettings>>().Value;

            // Get the ADHelper and EntraADHelper instances
            var adHelper = scope.ServiceProvider.GetRequiredService<IADHelper>();
            var entraADHelper = scope.ServiceProvider.GetRequiredService<IEntraADHelper>();

            await foreach (var directoryEntry in adHelper.GetDirectoryEntriesAsyncEnumerable(adHelperSettings.RootOrganizationaUnitDN))
            {
                _logger.LogDebug(">>>>>>>>>>>> Processing Computer name: {ComputerName} >>>>>>>>>>>", directoryEntry.Name);
               
                var rootOUName = adHelperSettings.RootOrganizationaUnitDN.Split(',')[0];

                // Retrieve the rootOUName distinguishedName property
                var distinguishedName = directoryEntry.Properties["distinguishedName"].Value?.ToString();
                _logger.LogDebug("DistinguishedName: {distinguishedName}", distinguishedName);

                // Check if the distinguishedName is null or empty                  
                if (string.IsNullOrEmpty(distinguishedName))
                {
                    _logger.LogWarning("DistinguishedName is null for {ComputerName}. Skipping to next computer object..", directoryEntry.Name);
                    continue;
                }

                _logger.LogDebug("Computer DistinguishedName: {distinguishedName}", distinguishedName);

                // Check if the distinguishedName is part of the excluded OUs
                if (adHelperSettings.ExcludedOUs?.Any(excludedOU => distinguishedName.Contains(excludedOU, StringComparison.OrdinalIgnoreCase)) == true)
                {
                    _logger.LogDebug("DistinguishedName {distinguishedName} is part of excluded OUs. Skipping...", distinguishedName);
                    continue;
                }


                _logger.LogTrace("Processing Extension Attributes Mapping...");
                foreach (var mapping in appSettings.ExtensionAttributeMappings)
                {
                    _logger.LogDebug("ExtensionAttributeMapping: {ExtensionAttributeMapping}", mapping.ToString());
                }


                // Retrieve the Entra AD Device by name
                _logger.LogDebug("Getting Entra AD Device for Computer name: {ComputerName}", directoryEntry.Properties["cn"].Value.ToString());
                var entraADDevice = await entraADHelper.GetDeviceByNameAsync(directoryEntry.Properties["cn"].Value.ToString());

                if (entraADDevice != null)
                {
                    _logger.LogDebug("Entra AD Device {ComputerName} has Entra Device ID: {DeviceId}", entraADDevice.DisplayName, entraADDevice.Id);
                    // Retrieve the ExtensionAttribute mapping object
                    foreach (var mapping in appSettings.ExtensionAttributeMappings)
                    {
                        var extensionAttributeValue = await entraADHelper.GetExtensionAttribute(entraADDevice.Id, mapping.ExtensionAttribute);
                        _logger.LogDebug("Retrieved extensionAttribute {extensionAttribute} with value: {ExtensionAttributeValue}", mapping.ExtensionAttribute, extensionAttributeValue);

                        var currentComputerAttributeValue = await adHelper.GetComputerAttributeAsync(distinguishedName, mapping.ComputerAttribute);

                        // Check if the currentComputerAttributeValue is null or empty
                        if (string.IsNullOrWhiteSpace(currentComputerAttributeValue))
                        {
                            _logger.LogWarning("ComputerAttribute {ComputerAttribute} is null or empty for {ComputerName}. Skip processing {extensionAttribute}", mapping.ComputerAttribute, directoryEntry.Name, mapping.ExtensionAttribute);
                            continue;
                        }

                        _logger.LogDebug("Retrieved Computer attribute {ComputerAttribute} with value: {ComputerAttributeValue}", mapping.ComputerAttribute, currentComputerAttributeValue);

                        
                        string? expectedComputerAttributeValue = null;
                        //applying regex if it exists to ComputerAttribute
                        if (!string.IsNullOrWhiteSpace(mapping.Regex))
                        {
                            _logger.LogTrace("Applying regex {regex} to ComputerAttribute: {ComputerAttribute}", mapping.Regex, mapping.ComputerAttribute);
                            var regex = new Regex(mapping.Regex);
                            var match = regex.Match(currentComputerAttributeValue);
                            if (match.Success)
                            {
                                expectedComputerAttributeValue = match.Value;
                                _logger.LogDebug("Regex applied to ComputerAttribute: {CurrentComputerAttributeValue}", currentComputerAttributeValue);
                                _logger.LogDebug("Extracted value using Regex: {ExpectedComputerAttributeValue}", expectedComputerAttributeValue);
                            }
                            else
                            {
                                _logger.LogWarning("Regex did not match any value in ComputerAttribute: {ComputerAttribute}", mapping.ComputerAttribute);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("No regex applied to ComputerAttribute: {ComputerAttribute}", mapping.ComputerAttribute);
                            expectedComputerAttributeValue = currentComputerAttributeValue;
                        }
                        
                        // Check if the expectedComputerAttributeValue is null or empty
                        _logger.LogDebug("Current ComputerAttribute value: {CurrentComputerAttributeValue}", currentComputerAttributeValue);
                        _logger.LogDebug("Expected ComputerAttribute value: {ExpectedComputerAttributeValue}", expectedComputerAttributeValue);
                        _logger.LogDebug("Comparing ExtensionAttribute value with expected ComputerAttribute value");
                        // Compare the ExtensionAttribute value with the extensionAttributeValue
                        if ((extensionAttributeValue != null && extensionAttributeValue != expectedComputerAttributeValue) || extensionAttributeValue == null)
                        {
                            _logger.LogDebug("Updating ExtensionAttribute value from {OldValue} to {NewValue}", extensionAttributeValue, expectedComputerAttributeValue);
                            await entraADHelper.SetExtensionAttributeValue(entraADDevice.Id, mapping.ExtensionAttribute, expectedComputerAttributeValue);
                        }
                        else
                        {
                            _logger.LogDebug("No update needed for ExtensionAttribute {extensionAttribute}. Current value {currentExtensionAttributeValue} match expected value: {ExtensionAttributeValue}",mapping.ExtensionAttribute,  extensionAttributeValue, expectedComputerAttributeValue);
                        }

                        _logger.LogDebug("########### ExtensionAttribute {ExtensionAttribute} for Device ID: {DeviceId} completed ###########", mapping.ExtensionAttribute, entraADDevice.Id);
                    }
                }


                //else if (appSettings.AttributeToSet.IsNullOrWhiteSpace() && appSettings.ExtensionAttributeMappings.Count == 0)
                //{
                //    _logger.LogWarning("AttributeToSet is not set in EntraADHelperSettings and no ExtensionAttributeMappings found. Skipping...");
                //    continue;
                //}
                //else
                //{

                //    // Extract the departmentOUName from the distinguishedName using regex
                //    string departmentOUName = null;
                //    var regex = new Regex(adHelperSettings.DistinguishedNameRegEx);
                //    _logger.LogTrace("Regex Pattern: {pattern}", adHelperSettings.DistinguishedNameRegEx);
                //    _logger.LogTrace("Root OU Name: {rootOUName}", rootOUName);

                //    var match = regex.Match(distinguishedName);
                //    if (match.Success)
                //    {
                //        departmentOUName = match.Groups["departmentOUName"].Value;
                //        _logger.LogDebug("OU Name extracted from Distinguished Name: {departmentOUName}", departmentOUName);
                //    }
                //    else
                //    {
                //        _logger.LogWarning("No match found for DistinguishedName: {distinguishedName}", distinguishedName);
                //    }

                //    var entraADDevice = await entraADHelper.GetDeviceByNameAsync(directoryEntry.Properties["name"].Value.ToString());

                //    if (entraADDevice != null)
                //    {
                //        _logger.LogDebug("Entra AD Device ID: {DeviceId}", entraADDevice.Id);

                //        // Retrieve the ExtensionAttribute value
                //        var extensionAttributeValue = await entraADHelper.GetExtensionAttribute(entraADDevice.Id, appSettings.AttributeToSet);
                //        _logger.LogDebug("Retrieved extensionAttribute {extensionAttribute} Value: {ExtensionAttributeValue}", appSettings.AttributeToSet, extensionAttributeValue);

                //        _logger.LogDebug("Comparing ExtensionAttribute value with departmentOUName");
                //        // Compare the ExtensionAttribute value with the departmentOUName
                //        if ((extensionAttributeValue != null && extensionAttributeValue != departmentOUName) || extensionAttributeValue == null)
                //        {
                //            _logger.LogDebug("Updating ExtensionAttribute value from {OldValue} to {NewValue}", extensionAttributeValue, departmentOUName);
                //            await entraADHelper.SetExtensionAttributeValue(entraADDevice.Id, appSettings.AttributeToSet, departmentOUName);

                //            // Add the device to the updated devices collection
                //            UpdatedDevices.Add(Tuple.Create(entraADDevice, departmentOUName));
                //        }
                //        else
                //        {
                //            _logger.LogDebug("No update needed for ExtensionAttribute value: {ExtensionAttributeValue}", extensionAttributeValue);
                //        }
                //    }
                //    else
                //    {
                //        _logger.LogWarning("Entra AD Device not found for {ComputerName}", directoryEntry.Name);
                //    }


                //}

                _logger.LogDebug("-----------------------------------------------------------------");
                _logger.LogDebug("--------- ENDING Set Computer Extension Attribute Job -----------");
                _logger.LogDebug("_________________________________________________________________");

                if (UpdatedDevices.Count > 0)
                {
                    _logger.LogInformation("Exporting updated devices:");
                    foreach (var deviceInfo in UpdatedDevices)
                    {
                        var device = deviceInfo.Item1;
                        var departmentOUName = deviceInfo.Item2;
                        _logger.LogInformation($"Exporting Device ID: {device.Id}, Device Name: {device.DisplayName}, Department OU Name: {departmentOUName}");
                    }

                    var exportTask = await ExportHelper.ExportDevicesToCsvAsync(serviceProvider, UpdatedDevices, ExportHelper.GetCsvFileName(appSettings.ExportFileNamePrefix));
                    if (exportTask)
                    {
                        _logger.LogDebug("Exported updated devices to CSV file successfully.");
                        UpdatedDevices.Clear();
                    }
                    else
                    {
                        _logger.LogError("Failed to export updated devices to CSV file.");
                    }
                }
                else
                {
                    _logger.LogInformation("No devices were updated.");
                }
            }
        }

        public static IEnumerable<Tuple<Device, string>> GetUpdatedDevices()
        {
            return UpdatedDevices;
        }
    }
}
