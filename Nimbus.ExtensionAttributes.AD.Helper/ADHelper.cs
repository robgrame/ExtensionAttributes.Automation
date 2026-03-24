using Nimbus.ExtensionAttributes.AD.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nimbus.ExtensionAttributes.AD
{
    /// <summary>
    /// Utility class for parsing LDAP Distinguished Names safely
    /// </summary>
    public static class DistinguishedNameParser
    {
        // Matches RDN components, handling escaped commas within values
        private static readonly Regex RdnRegex = new(
            @"(?<type>[A-Za-z]+)=(?<value>(?:[^,\\]|\\.)*)(?:,|$)",
            RegexOptions.Compiled);

        /// <summary>
        /// Extracts the CN (Common Name) value from a Distinguished Name
        /// </summary>
        public static string? ExtractComputerName(string distinguishedName)
        {
            var match = RdnRegex.Match(distinguishedName);
            while (match.Success)
            {
                if (match.Groups["type"].Value.Equals("CN", StringComparison.OrdinalIgnoreCase))
                {
                    return UnescapeValue(match.Groups["value"].Value);
                }
                match = match.NextMatch();
            }
            return null;
        }

        /// <summary>
        /// Extracts the container path (everything after the first RDN component)
        /// </summary>
        public static string ExtractContainerPath(string distinguishedName)
        {
            var firstComma = FindUnescapedComma(distinguishedName);
            if (firstComma < 0)
                return distinguishedName;

            return distinguishedName[(firstComma + 1)..];
        }

        private static int FindUnescapedComma(string dn)
        {
            for (int i = 0; i < dn.Length; i++)
            {
                if (dn[i] == '\\')
                {
                    i++; // skip escaped character
                    continue;
                }
                if (dn[i] == ',')
                    return i;
            }
            return -1;
        }

        private static string UnescapeValue(string value)
        {
            return value.Replace("\\,", ",").Replace("\\\\", "\\");
        }
    }

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
            ArgumentException.ThrowIfNullOrWhiteSpace(fqdnContainerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(computerName);

            using var rootEntry = new DirectoryEntry($"LDAP://{fqdnContainerName}");
            using var searcher = CreateSearcher(rootEntry, $"(&(objectClass=computer)(cn={EscapeLdapFilter(computerName)}))");
            LoadAttributes(searcher);

            var result = await Task.Run(() => searcher.FindOne());
            return result?.GetDirectoryEntry() ?? throw new InvalidOperationException($"Computer '{computerName}' not found.");
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async IAsyncEnumerable<DirectoryEntry> GetDirectoryEntriesAsyncEnumerable(string fqdnContainerName)
        {
            _logger.LogTrace("Getting Directory Entries from {fqdnContainerName}", fqdnContainerName);

            try
            {
                if (!DirectoryEntry.Exists($"LDAP://{fqdnContainerName}"))
                {
                    _logger.LogError("Directory {fqdnContainerName} does not exist", fqdnContainerName);
                    yield break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Directory {fqdnContainerName} is not valid. Check Domain FQDN value", fqdnContainerName);
                yield break;
            }

            using var rootEntry = new DirectoryEntry($"LDAP://{fqdnContainerName}");
            using var searcher = CreateSearcher(rootEntry, "(&(objectClass=computer)(objectCategory=computer))");
            LoadAttributes(searcher);

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
            var (computerName, containerPath) = ParseDistinguishedName(distinguishedName);

            using var rootEntry = new DirectoryEntry($"LDAP://{containerPath}");
            using var searcher = CreateSearcher(rootEntry, $"(&(objectClass=computer)(cn={EscapeLdapFilter(computerName)}))");
            LoadAttributes(searcher);

            var result = await Task.Run(() => searcher.FindOne());
            return result?.GetDirectoryEntry() ?? throw new InvalidOperationException($"Computer '{computerName}' not found.");
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task<DirectoryEntry> GetDirectoryEntryWithAttributeAsync(string distinguishedName, string computerAttribute)
        {
            _logger.LogTrace("Getting Directory Entry from {distinguishedName} with attribute {computerAttribute}", distinguishedName, computerAttribute);
            var (computerName, containerPath) = ParseDistinguishedName(distinguishedName);

            using var rootEntry = new DirectoryEntry($"LDAP://{containerPath}");
            using var searcher = CreateSearcher(rootEntry, $"(&(objectClass=computer)(cn={EscapeLdapFilter(computerName)}))");
            LoadAttributes(searcher, computerAttribute);

            var result = await Task.Run(() => searcher.FindOne());
            return result?.GetDirectoryEntry() ?? throw new InvalidOperationException($"Computer '{computerName}' not found.");
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task<DirectoryEntry> GetDirectoryEntryWithAttributesAsync(string distinguishedName, List<string> computerAttributes)
        {
            _logger.LogTrace("Getting Directory Entry from {distinguishedName} with {Count} additional attributes", distinguishedName, computerAttributes.Count);
            var (computerName, containerPath) = ParseDistinguishedName(distinguishedName);

            using var rootEntry = new DirectoryEntry($"LDAP://{containerPath}");
            using var searcher = CreateSearcher(rootEntry, $"(&(objectClass=computer)(cn={EscapeLdapFilter(computerName)}))");
            LoadAttributes(searcher, computerAttributes.ToArray());

            var result = await Task.Run(() => searcher.FindOne());
            return result?.GetDirectoryEntry() ?? throw new InvalidOperationException($"Computer '{computerName}' not found.");
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public async Task<string> GetComputerAttributeAsync(string distinguishedName, string computerAttribute)
        {
            _logger.LogTrace("Getting computer attribute {computerAttribute} from {distinguishedName}", computerAttribute, distinguishedName);
            var (computerName, containerPath) = ParseDistinguishedName(distinguishedName);

            using var rootEntry = new DirectoryEntry($"LDAP://{containerPath}");
            using var searcher = CreateSearcher(rootEntry, $"(&(objectClass=computer)(cn={EscapeLdapFilter(computerName)}))");
            LoadAttributes(searcher, computerAttribute);

            var result = await Task.Run(() => searcher.FindOne());
            if (result == null)
                throw new InvalidOperationException($"Computer '{computerName}' not found in Directory.");

            using var directoryEntry = result.GetDirectoryEntry();
            if (directoryEntry.Properties[computerAttribute].Count > 0)
            {
                return directoryEntry.Properties[computerAttribute][0]?.ToString() ?? string.Empty;
            }

            _logger.LogWarning("Computer attribute {computerAttribute} not found for {computerName}", computerAttribute, computerName);
            return string.Empty;
        }

        #region Private Helper Methods

        /// <summary>
        /// Parses a Distinguished Name into computer name and container path
        /// </summary>
        private (string ComputerName, string ContainerPath) ParseDistinguishedName(string distinguishedName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(distinguishedName);

            var computerName = DistinguishedNameParser.ExtractComputerName(distinguishedName);
            if (string.IsNullOrWhiteSpace(computerName))
            {
                throw new ArgumentException($"Could not extract computer name from DN: {distinguishedName}", nameof(distinguishedName));
            }

            var containerPath = DistinguishedNameParser.ExtractContainerPath(distinguishedName);
            _logger.LogTrace("Parsed DN — Computer: {ComputerName}, Container: {ContainerPath}", computerName, containerPath);

            return (computerName, containerPath);
        }

        /// <summary>
        /// Creates a DirectorySearcher with standard configuration
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private DirectorySearcher CreateSearcher(DirectoryEntry rootEntry, string filter)
        {
            return new DirectorySearcher(rootEntry)
            {
                SearchRoot = rootEntry,
                Filter = filter,
                SearchScope = SearchScope.Subtree,
                Asynchronous = true,
                ClientTimeout = TimeSpan.FromMilliseconds(_adHelperSettings.ClientTimeout),
                PageSize = _adHelperSettings.PageSize
            };
        }

        /// <summary>
        /// Loads default and additional attributes into the searcher
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private void LoadAttributes(DirectorySearcher searcher, params string[] additionalAttributes)
        {
            foreach (var attribute in _adHelperSettings.AttributesToLoad)
            {
                searcher.PropertiesToLoad.Add(attribute);
            }

            foreach (var attr in additionalAttributes)
            {
                if (!searcher.PropertiesToLoad.Contains(attr))
                {
                    searcher.PropertiesToLoad.Add(attr);
                }
            }
        }

        /// <summary>
        /// Escapes special characters in LDAP filter values to prevent injection
        /// </summary>
        private static string EscapeLdapFilter(string value)
        {
            return value
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }

        #endregion
    }
}
