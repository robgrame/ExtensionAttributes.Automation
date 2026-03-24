using Nimbus.ExtensionAttributes.AD;

namespace Nimbus.ExtensionAttributes.Tests;

public class DistinguishedNameParserTests
{
    [Theory]
    [InlineData("CN=WORKSTATION01,OU=Computers,DC=contoso,DC=com", "WORKSTATION01")]
    [InlineData("CN=SERVER-01,OU=Servers,OU=IT,DC=lab,DC=local", "SERVER-01")]
    [InlineData("CN=PC\\,Special,OU=Test,DC=corp,DC=net", "PC,Special")]
    public void ExtractComputerName_ValidDN_ReturnsComputerName(string dn, string expected)
    {
        var result = DistinguishedNameParser.ExtractComputerName(dn);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("OU=Computers,DC=contoso,DC=com")]
    [InlineData("DC=contoso,DC=com")]
    public void ExtractComputerName_NoCN_ReturnsNull(string dn)
    {
        var result = DistinguishedNameParser.ExtractComputerName(dn);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("CN=PC01,OU=Computers,DC=contoso,DC=com", "OU=Computers,DC=contoso,DC=com")]
    [InlineData("CN=PC\\,01,OU=Computers,DC=contoso,DC=com", "OU=Computers,DC=contoso,DC=com")]
    public void ExtractContainerPath_ValidDN_ReturnsContainerPath(string dn, string expected)
    {
        var result = DistinguishedNameParser.ExtractContainerPath(dn);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExtractContainerPath_SingleComponent_ReturnsSame()
    {
        var result = DistinguishedNameParser.ExtractContainerPath("DC=com");
        Assert.Equal("DC=com", result);
    }
}
