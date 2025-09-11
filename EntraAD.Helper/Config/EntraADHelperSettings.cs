using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Automation.Config
{
    public class EntraADHelperSettings
    {
        public string TokenEndpoint { get; set; } = "https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        public int TokenExpirationBuffer { get; set; } = 15; // in minutes
        public required string? TenantId { get; set; } = null;
        public required string? ClientId { get; set; } = null;
        public string? ClientSecret { get; set; }
        public string? CertificateThumbprint { get; set; }
        public required bool UseClientSecret { get; set; } = true;
        public required string[] AttributesToLoad { get; set; } = new string[] {"cn","distinguishedName","operatingSystem","operatingSystemVersion" };
        public int PageSize { get; set; } = 1000;
        public int ClientTimeout { get; set; } = 60000;
    }
}
