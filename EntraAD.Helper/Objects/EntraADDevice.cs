using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Azure.Automation.Objects
{


    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);

    public class PhysicalIds
    {
        [JsonPropertyName("OrderId")]
        public string? OrderId { get; set; }
        [JsonPropertyName("ZTDID")]
        public string? ZTDID { get; set; }
        [JsonPropertyName("HWID")]
        public string? HWID { get; set; }
    }


    public class AlternativeSecurityId
    {
        [JsonPropertyName("type")]
        public int? Type { get; set; }

        [JsonPropertyName("identityProvider")]
        public string? IdentityProvider { get; set; }

        [JsonPropertyName("key")]
        public required string Key { get; set; }
    }

    public class ExtensionAttributes
    {
        [JsonPropertyName("extensionAttribute1")]
        public string? ExtensionAttribute1 { get; set; }

        [JsonPropertyName("extensionAttribute2")]
        public string? ExtensionAttribute2 { get; set; }

        [JsonPropertyName("extensionAttribute3")]
        public string? ExtensionAttribute3 { get; set; }

        [JsonPropertyName("extensionAttribute4")]
        public string? ExtensionAttribute4 { get; set; }

        [JsonPropertyName("extensionAttribute5")]
        public string? ExtensionAttribute5 { get; set; }

        [JsonPropertyName("extensionAttribute6")]
        public string? ExtensionAttribute6 { get; set; }

        [JsonPropertyName("extensionAttribute7")]
        public string? ExtensionAttribute7 { get; set; }

        [JsonPropertyName("extensionAttribute8")]
        public string? ExtensionAttribute8 { get; set; }

        [JsonPropertyName("extensionAttribute9")]
        public string? ExtensionAttribute9 { get; set; }

        [JsonPropertyName("extensionAttribute10")]
        public string? ExtensionAttribute10 { get; set; }

        [JsonPropertyName("extensionAttribute11")]
        public string? ExtensionAttribute11 { get; set; }

        [JsonPropertyName("extensionAttribute12")]
        public string? ExtensionAttribute12 { get; set; }

        [JsonPropertyName("extensionAttribute13")]
        public string? ExtensionAttribute13 { get; set; }

        [JsonPropertyName("extensionAttribute14")]
        public string? ExtensionAttribute14 { get; set; }

        [JsonPropertyName("extensionAttribute15")]
        public string? ExtensionAttribute15 { get; set; }
    }

    public class EntraADDevice
    {
        [JsonPropertyName("@odata.context")]
        public required string OdataContext { get; set; }

        [JsonPropertyName("@microsoft.graph.tips")]
        public required string MicrosoftGraphTips { get; set; }

        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("deletedDateTime")]
        public DateTime? DeletedDateTime { get; set; }

        [JsonPropertyName("accountEnabled")]
        public bool? AccountEnabled { get; set; }

        [JsonPropertyName("approximateLastSignInDateTime")]
        public DateTime? ApproximateLastSignInDateTime { get; set; }

        [JsonPropertyName("complianceExpirationDateTime")]
        public DateTime? ComplianceExpirationDateTime { get; set; }

        [JsonPropertyName("createdDateTime")]
        public DateTime? CreatedDateTime { get; set; }

        [JsonPropertyName("deviceCategory")]
        public string? DeviceCategory { get; set; }

        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("deviceMetadata")]
        public string? DeviceMetadata { get; set; }

        [JsonPropertyName("deviceOwnership")]
        public string? DeviceOwnership { get; set; }

        [JsonPropertyName("deviceVersion")]
        public int? DeviceVersion { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("domainName")]
        public string? DomainName { get; set; }

        [JsonPropertyName("enrollmentProfileName")]
        public string? EnrollmentProfileName { get; set; }

        [JsonPropertyName("enrollmentType")]
        public string? EnrollmentType { get; set; }

        [JsonPropertyName("externalSourceName")]
        public string? ExternalSourceName { get; set; }

        [JsonPropertyName("isCompliant")]
        public bool? IsCompliant { get; set; }

        [JsonPropertyName("isManaged")]
        public object? IsManaged { get; set; }

        [JsonPropertyName("isRooted")]
        public object? IsRooted { get; set; }

        [JsonPropertyName("managementType")]
        public object? ManagementType { get; set; }

        [JsonPropertyName("manufacturer")]
        public object? Manufacturer { get; set; }

        [JsonPropertyName("mdmAppId")]
        public object? MdmAppId { get; set; }

        [JsonPropertyName("model")]
        public object? Model { get; set; }

        [JsonPropertyName("onPremisesLastSyncDateTime")]
        public object? OnPremisesLastSyncDateTime { get; set; }

        [JsonPropertyName("onPremisesSyncEnabled")]
        public object? OnPremisesSyncEnabled { get; set; }

        [JsonPropertyName("operatingSystem")]
        public string? OperatingSystem { get; set; } = "Unknown";

        [JsonPropertyName("operatingSystemVersion")]
        public string? OperatingSystemVersion { get; set; } = "Unknown";

        [JsonPropertyName("physicalIds")]
        public List<string>? PhysicalIds { get; set; }

        [JsonPropertyName("profileType")]
        public string? ProfileType { get; set; }

        [JsonPropertyName("registrationDateTime")]
        public DateTime? RegistrationDateTime { get; set; }

        [JsonPropertyName("sourceType")]
        public string? SourceType { get; set; }

        [JsonPropertyName("systemLabels")]
        public List<string>? SystemLabels { get; set; }

        [JsonPropertyName("trustType")]
        public string? TrustType { get; set; }

        [JsonPropertyName("extensionAttributes")]
        public required ExtensionAttributes ExtensionAttributes { get; set; }

        [JsonPropertyName("alternativeSecurityIds")]
        public List<AlternativeSecurityId>? AlternativeSecurityIds { get; set; }
    }


}
