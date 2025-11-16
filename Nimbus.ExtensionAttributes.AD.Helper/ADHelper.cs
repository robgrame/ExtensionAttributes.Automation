using Nimbus.ExtensionAttributes.AD.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Nimbus.ExtensionAttributes.AD
{
    public class ADHelper : IADHelper
    {
        private readonly ILogger<IADHelper> _logger;
        private readonly ADHelperSettings _adHelperSettings;

        public ADHelper(ILogger<IADHelper> logger, IOptions<ADHelperSettings> adHelperSettings)
        {
            _logger = logger;
            _adHelperSettings = adHelperSettings.Value;
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task<DirectoryEntry> GetDirectoryEntrybyComputerNameAsync(string fqdnContainerName, string computerName)
        {
            _logger.LogTrace("Getting Directory Entry from {fqdnContainerName} for computer {computerName}", fqdnContainerName, computerName);
            // check if fqdnContainerName exists
            if (string.IsNullOrWhiteSpace(fqdnContainerName))
            {
                _logger.LogError("FQDN Container Name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("FQDN Container Name cannot be null or empty.", nameof(fqdnContainerName));
            }
            // check if computerName exists
            if (string.IsNullOrWhiteSpace(computerName))
            {
                _logger.LogError("Computer Name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Computer Name cannot be null or empty.", nameof(computerName));
            }
            using var rootEntry = new DirectoryEntry($"LDAP://{fqdnContainerName}");
            using var searcher = new DirectorySearcher(rootEntry)
            {
                SearchRoot = rootEntry,
                Filter = $"(&(objectClass=computer)(cn={computerName}))",
                SearchScope = SearchScope.Subtree,
                Asynchronous = true,
                ClientTimeout = TimeSpan.FromSeconds(_adHelperSettings.ClientTimeout),
                PageSize = _adHelperSettings.PageSize
            };
            foreach (var attribute in _adHelperSettings.AttributesToLoad)
            {
                searcher.PropertiesToLoad.Add(attribute);
            }
            var result = await Task.Run(() => searcher.FindOne());
            return result?.GetDirectoryEntry() ?? throw new InvalidOperationException("Computer not found.");
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async IAsyncEnumerable<DirectoryEntry> GetDirectoryEntriesAsyncEnumerable(string fqdnContainerName)
        {
            _logger.LogTrace("Getting Directory Entries from {fqdnContainerName}", fqdnContainerName);

            try
            {
                // check if fqdnContainerName exists
                if (DirectoryEntry.Exists($"LDAP://{fqdnContainerName}"))
                {
                    _logger.LogTrace("Directory {fqdnContainerName} is  valid", fqdnContainerName);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Directory {fqdnContainerName} is not valid. Check Domain FQDN value", fqdnContainerName);
                yield break;
            }



            using var rootEntry = new DirectoryEntry($"LDAP://{fqdnContainerName}");
            using var searcher = new DirectorySearcher(rootEntry)
            {
                SearchRoot = rootEntry,
                Filter = "(&(objectClass=computer)(objectCategory=computer))",
                SearchScope = SearchScope.Subtree,
                Asynchronous = true,
                ClientTimeout = TimeSpan.FromSeconds(_adHelperSettings.ClientTimeout),
                PageSize = _adHelperSettings.PageSize
            };

            foreach (var attribute in _adHelperSettings.AttributesToLoad)
            {
                searcher.PropertiesToLoad.Add(attribute);
            }

            using var results = await Task.Run(() => searcher.FindAll());

            foreach (SearchResult result in results)
            {
                yield return result.GetDirectoryEntry();
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task<DirectoryEntry> GetDirectoryEntryAsync(string distinguishedName)
        {

            _logger.LogTrace("Getting Directory Entry from {distinguishedName}", distinguishedName);
            // check if distinguishedName exists
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                _logger.LogError("Distinguished Name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Distinguished Name cannot be null or empty.", nameof(distinguishedName));
            }

            // extract the computer name from the distinguished name
            _logger.LogTrace("Extracting computer name from distinguished name {distinguishedName}", distinguishedName);
            var computerName = distinguishedName.Split(',').FirstOrDefault(x => x.StartsWith("CN="))?.Substring(3);
            _logger.LogTrace("Extracted computer name {computerName}", computerName);
            if (string.IsNullOrWhiteSpace(computerName))
            {
                _logger.LogError("Computer name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Computer name cannot be null or empty.", nameof(distinguishedName));
            }

            // extract the container name from the distinguished name
            _logger.LogTrace("Extracting container name from distinguished name {distinguishedName}", distinguishedName);
            var fqdnContainerName = distinguishedName.Split(',').Skip(1).FirstOrDefault();
            _logger.LogTrace("Extracted container name {fqdnContainerName}", fqdnContainerName);


            using var rootEntry = new DirectoryEntry($"LDAP://{fqdnContainerName}");
            using var searcher = new DirectorySearcher(rootEntry)
            {
                SearchRoot = rootEntry,
                Filter = $"(&(objectClass=computer)(cn={computerName}))",
                SearchScope = SearchScope.Subtree,
                Asynchronous = true,
                ClientTimeout = TimeSpan.FromSeconds(_adHelperSettings.ClientTimeout),
                PageSize = _adHelperSettings.PageSize
            };
            foreach (var attribute in _adHelperSettings.AttributesToLoad)
            {
                searcher.PropertiesToLoad.Add(attribute);
            }
            var result = await Task.Run(() => searcher.FindOne());
            return result?.GetDirectoryEntry() ?? throw new InvalidOperationException("Computer not found.");
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task<DirectoryEntry> GetDirectoryEntryWithAttributeAsync(string distinguishedName, string computerAttribute)
        {

            _logger.LogTrace("Getting Directory Entry from {distinguishedName}", distinguishedName);
            // check if distinguishedName exists
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                _logger.LogError("Distinguished Name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Distinguished Name cannot be null or empty.", nameof(distinguishedName));
            }

            // extract the computer name from the distinguished name
            _logger.LogTrace("Extracting computer name from distinguished name {distinguishedName}", distinguishedName);
            var computerName = distinguishedName.Split(',').FirstOrDefault(x => x.StartsWith("CN="))?.Substring(3);
            _logger.LogTrace("Extracted computer name {computerName}", computerName);
            if (string.IsNullOrWhiteSpace(computerName))
            {
                _logger.LogError("Computer name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Computer name cannot be null or empty.", nameof(distinguishedName));
            }

            // extract the container name from the distinguished name
            _logger.LogTrace("Extracting container name from distinguished name {distinguishedName}", distinguishedName);

            // Skip the first element (CN=ComputerName) and join the rest to form the container name
            // This assumes that the distinguished name is in the format CN=ComputerName,OU=ContainerName,DC=DomainName
            // The container name is everything after the first comma
            var fqdnContainerName = distinguishedName.Split(',').Skip(1).Aggregate(new StringBuilder(), (sb, s) =>
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }
                sb.Append(s);
                return sb;
            }).ToString();



            _logger.LogTrace("Extracted container name {fqdnContainerName}", fqdnContainerName);

            using var rootEntry = new DirectoryEntry($"LDAP://{fqdnContainerName}");
            using var searcher = new DirectorySearcher(rootEntry)
            {
                SearchRoot = rootEntry,
                Filter = $"(&(objectClass=computer)(cn={computerName}))",
                SearchScope = SearchScope.Subtree,
                Asynchronous = true,
                ClientTimeout = TimeSpan.FromSeconds(_adHelperSettings.ClientTimeout),
                PageSize = _adHelperSettings.PageSize
            };


            // add the attributes to load
            foreach (var attribute in _adHelperSettings.AttributesToLoad)
            {
                searcher.PropertiesToLoad.Add(attribute);
            }

            // check if the computer attribute is already in the attributes to load
            _logger.LogTrace("Checking if computer attribute {computerAttribute} is in the attributes to load", computerAttribute);
            bool isIncluded = _adHelperSettings.AttributesToLoad.Contains(computerAttribute);
            if (isIncluded)
            {
                _logger.LogTrace("Computer attribute {computerAttribute} is already in the attributes to load", computerAttribute);
            }
            else
            {
                _logger.LogTrace("Computer attribute {computerAttribute} is not in the attributes to load", computerAttribute);
            }
            // if the computer attribute is not in the attributes to load, add it
            if (!isIncluded)
            {
                _logger.LogTrace("Adding computer attribute {computerAttribute} to searcher", computerAttribute);
                searcher.PropertiesToLoad.Add(computerAttribute);
            }
            
            var result = await Task.Run(() => searcher.FindOne());
            return result?.GetDirectoryEntry() ?? throw new InvalidOperationException("Computer not found.");
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task<DirectoryEntry> GetDirectoryEntryWithAttributesAsync(string distinguishedName, List<string> computerAttributes)
        {

            _logger.LogTrace("Getting Directory Entry from {distinguishedName}", distinguishedName);
            // check if distinguishedName exists
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                _logger.LogError("Distinguished Name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Distinguished Name cannot be null or empty.", nameof(distinguishedName));
            }

            // extract the computer name from the distinguished name
            _logger.LogTrace("Extracting computer name from distinguished name {distinguishedName}", distinguishedName);
            var computerName = distinguishedName.Split(',').FirstOrDefault(x => x.StartsWith("CN="))?.Substring(3);
            _logger.LogTrace("Extracted computer name {computerName}", computerName);
            if (string.IsNullOrWhiteSpace(computerName))
            {
                _logger.LogError("Computer name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Computer name cannot be null or empty.", nameof(distinguishedName));
            }

            // extract the container name from the distinguished name
            _logger.LogTrace("Extracting container name from distinguished name {distinguishedName}", distinguishedName);

            // Skip the first element (CN=ComputerName) and join the rest to form the container name
            // This assumes that the distinguished name is in the format CN=ComputerName,OU=ContainerName,DC=DomainName
            // The container name is everything after the first comma
            var fqdnContainerName = distinguishedName.Split(',').Skip(1).Aggregate(new StringBuilder(), (sb, s) =>
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }
                sb.Append(s);
                return sb;
            }).ToString();

            _logger.LogTrace("Extracted container name {fqdnContainerName}", fqdnContainerName);

            using var rootEntry = new DirectoryEntry($"LDAP://{fqdnContainerName}");
            using var searcher = new DirectorySearcher(rootEntry)
            {
                SearchRoot = rootEntry,
                Filter = $"(&(objectClass=computer)(cn={computerName}))",
                SearchScope = SearchScope.Subtree,
                Asynchronous = true,
                ClientTimeout = TimeSpan.FromSeconds(_adHelperSettings.ClientTimeout),
                PageSize = _adHelperSettings.PageSize
            };


            // add the attributes to load
            foreach (var attribute in _adHelperSettings.AttributesToLoad)
            {
                searcher.PropertiesToLoad.Add(attribute);
            }
            // add the computer attributes to load
            foreach (var computerAttribute in computerAttributes)
            {
                // check if the computer attribute is already in the attributes to load
                if (!searcher.PropertiesToLoad.Contains(computerAttribute))
                {
                    _logger.LogTrace("Computer attribute {computerAttribute} is not in the attributes to load", computerAttribute);
                }
                else
                {
                    _logger.LogTrace("Computer attribute {computerAttribute} is already in the attributes to load", computerAttribute);
                }
                // if the computer attribute is not in the attributes to load, add it
                _logger.LogTrace("Adding computer attribute {computerAttribute} to searcher", computerAttribute);
                searcher.PropertiesToLoad.Add(computerAttribute);
            }

            var result = await Task.Run(() => searcher.FindOne());
            return result?.GetDirectoryEntry() ?? throw new InvalidOperationException("Computer not found.");
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task<string> GetComputerAttributeAsync(string distinguishedName, string computerAttribute)
        {
            _logger.LogTrace("Getting computer attribute from computer distinguished name {distinguishedName}", distinguishedName);
            // check if distinguishedName exists
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                _logger.LogError("Distinguished Name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Distinguished Name cannot be null or empty.", nameof(distinguishedName));
            }

            // extract the computer name from the distinguished name
            _logger.LogTrace("Extracting computer name from distinguished name {distinguishedName}", distinguishedName);
            var computerName = distinguishedName.Split(',').FirstOrDefault(x => x.StartsWith("CN="))?.Substring(3);
            _logger.LogTrace("Extracted computer name {computerName}", computerName);
            if (string.IsNullOrWhiteSpace(computerName))
            {
                _logger.LogError("Computer name is null or empty. Cannot get Directory Entry.");
                throw new ArgumentException("Computer name cannot be null or empty.", nameof(distinguishedName));
            }

            // extract the container name from the distinguished name
            _logger.LogTrace("Extracting container name from distinguished name {distinguishedName}", distinguishedName);
            // Skip the first element (CN=ComputerName) and join the rest to form the container name
            // This assumes that the distinguished name is in the format CN=ComputerName,OU=ContainerName,DC=DomainName
            // The container name is everything after the first comma
            var fqdnContainerName = distinguishedName.Split(',').Skip(1).Aggregate(new StringBuilder(), (sb, s) =>
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }
                sb.Append(s);
                return sb;
            }).ToString();
            _logger.LogTrace("Extracted container name {fqdnContainerName}", fqdnContainerName);
            using var rootEntry = new DirectoryEntry($"LDAP://{fqdnContainerName}");
            using var searcher = new DirectorySearcher(rootEntry)
            {
                SearchRoot = rootEntry,
                Filter = $"(&(objectClass=computer)(cn={computerName}))",
                SearchScope = SearchScope.Subtree,
                Asynchronous = true,
                ClientTimeout = TimeSpan.FromSeconds(_adHelperSettings.ClientTimeout),
                PageSize = _adHelperSettings.PageSize
            };
            // Add the attributes to load, including the computer attribute if not already present
            foreach (var attribute in _adHelperSettings.AttributesToLoad)
            {
                searcher.PropertiesToLoad.Add(attribute);
            }

            if (!_adHelperSettings.AttributesToLoad.Contains(computerAttribute))
            {
                _logger.LogTrace("Adding computer attribute {computerAttribute} to searcher", computerAttribute);
                searcher.PropertiesToLoad.Add(computerAttribute);
            }
            else
            {
                _logger.LogTrace("Computer attribute {computerAttribute} is already in the attributes to load", computerAttribute);
            }

            var result = await Task.Run(() => searcher.FindOne());
            if (result != null)
            {
                var directoryEntry = result.GetDirectoryEntry();
                if (directoryEntry.Properties[computerAttribute].Count > 0)
                {
                    return directoryEntry.Properties[computerAttribute][0]?.ToString() ?? string.Empty;
                }
                else
                {
                    _logger.LogWarning("Computer attribute {computerAttribute} not found in Directory Entry", computerAttribute);
                    return string.Empty;
                }
            }
            else
            {
                _logger.LogError("Computer not found in Directory Entry");
                throw new InvalidOperationException("Computer not found in Directory Entry");
            }

        }
    }
}
