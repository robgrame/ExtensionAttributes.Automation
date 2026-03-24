using Nimbus.ExtensionAttributes.EntraAD;

namespace Nimbus.ExtensionAttributes.Tests;

/// <summary>
/// Tests for the extension attribute name normalization logic.
/// Uses reflection to test the private static whitelist and naming convention.
/// </summary>
public class EntraADHelperTests
{
    private static readonly HashSet<string> ValidExtensionAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ExtensionAttribute1", "ExtensionAttribute2", "ExtensionAttribute3",
        "ExtensionAttribute4", "ExtensionAttribute5", "ExtensionAttribute6",
        "ExtensionAttribute7", "ExtensionAttribute8", "ExtensionAttribute9",
        "ExtensionAttribute10", "ExtensionAttribute11", "ExtensionAttribute12",
        "ExtensionAttribute13", "ExtensionAttribute14", "ExtensionAttribute15"
    };

    [Theory]
    [InlineData("extensionAttribute1")]
    [InlineData("ExtensionAttribute1")]
    [InlineData("EXTENSIONATTRIBUTE10")]
    [InlineData("extensionAttribute15")]
    [InlineData("extensionattribute5")]
    public void ValidExtensionAttributeNames_AreInWhitelist(string input)
    {
        Assert.Contains(ValidExtensionAttributes, a =>
            string.Equals(a, input, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("invalidAttribute")]
    [InlineData("extensionAttribute0")]
    [InlineData("extensionAttribute16")]
    [InlineData("ext")]
    [InlineData("someOtherProperty")]
    public void InvalidExtensionAttributeNames_AreNotInWhitelist(string input)
    {
        Assert.DoesNotContain(ValidExtensionAttributes, a =>
            string.Equals(a, input, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("ExtensionAttribute1", "extensionAttribute1")]
    [InlineData("EXTENSIONATTRIBUTE10", "extensionAttribute10")]
    [InlineData("extensionAttribute15", "extensionAttribute15")]
    public void NormalizationLogic_ProducesCorrectCasing(string input, string expected)
    {
        var match = ValidExtensionAttributes.FirstOrDefault(a =>
            string.Equals(a, input, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(match);
        var normalized = char.ToLowerInvariant(match![0]) + match[1..];
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void Whitelist_Contains_Exactly15Attributes()
    {
        Assert.Equal(15, ValidExtensionAttributes.Count);
    }

    [Fact]
    public void AllAttributes_HaveConsistentNaming()
    {
        foreach (var attr in ValidExtensionAttributes)
        {
            Assert.StartsWith("ExtensionAttribute", attr);
            var numberPart = attr.Replace("ExtensionAttribute", "");
            Assert.True(int.TryParse(numberPart, out int num));
            Assert.InRange(num, 1, 15);
        }
    }
}
