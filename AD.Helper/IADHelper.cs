using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;

namespace AD.Automation

{
    public interface IADHelper
    {
        Task<DirectoryEntry> GetDirectoryEntryAsync(string distinguishedName);
        Task<DirectoryEntry> GetDirectoryEntryWithAttributeAsync(string distinguishedName, string computerAttribute);
        Task<string> GetComputerAttributeAsync(string distinguishedName, string computerAttribute);
        IAsyncEnumerable<DirectoryEntry> GetDirectoryEntriesAsyncEnumerable(string fqdnContainerName);
    }
}
