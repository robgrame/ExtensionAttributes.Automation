using Nimbus.ExtensionAttributes.WorkerSvc.Config;

namespace Nimbus.ExtensionAttributes.Tests;

public class ConfigurationTests
{
    [Fact]
    public void AppSettings_RequiredProperties_CanBeSet()
    {
        var settings = new AppSettings
        {
            CertificateThumbprint = "ABC123",
            ExportPath = @"C:\Temp",
            ExportFileNamePrefix = "Test",
            ExtensionAttributeMappings = new List<ExtensionAttributeMapping>()
        };

        Assert.Equal("ABC123", settings.CertificateThumbprint);
        Assert.Equal(@"C:\Temp", settings.ExportPath);
        Assert.False(settings.DryRun);
    }

    [Fact]
    public void ExtensionAttributeMapping_Properties_AreSettable()
    {
        var mapping = new ExtensionAttributeMapping
        {
            ExtensionAttribute = "extensionAttribute1",
            DataSource = DataSourceType.ActiveDirectory,
            SourceAttribute = "distinguishedName",
            DefaultValue = "Unknown"
        };

        Assert.Equal("extensionAttribute1", mapping.ExtensionAttribute);
        Assert.Equal(DataSourceType.ActiveDirectory, mapping.DataSource);
        Assert.Equal("distinguishedName", mapping.SourceAttribute);
        Assert.Equal("Unknown", mapping.DefaultValue);
        Assert.False(mapping.UseHardwareInfo);
    }

    [Fact]
    public void DataSourceSettings_Defaults_AreCorrect()
    {
        var settings = new DataSourceSettings();
        Assert.True(settings.EnableActiveDirectory);
        Assert.False(settings.EnableIntune);
        Assert.Equal("ActiveDirectory", settings.PreferredDataSource);
    }

    [Fact]
    public void NotificationSettings_Defaults_AreReasonable()
    {
        var settings = new Nimbus.ExtensionAttributes.WorkerSvc.Services.NotificationSettings();
        Assert.False(settings.EnableEmailNotifications);
        Assert.False(settings.EnableTeamsNotifications);
        Assert.False(settings.EnableSlackNotifications);
        Assert.Equal(587, settings.SmtpPort);
        Assert.Equal(10, settings.FailedDevicesThreshold);
        Assert.Equal(3, settings.ConsecutiveFailuresThreshold);
    }

    [Fact]
    public void ExtensionAttributeMapping_ToString_IncludesAllFields()
    {
        var mapping = new ExtensionAttributeMapping
        {
            ExtensionAttribute = "extensionAttribute1",
            DataSource = DataSourceType.ActiveDirectory,
            SourceAttribute = "distinguishedName",
            Regex = "(?<=OU=)([^,]+)",
            DefaultValue = "Unknown"
        };

        var result = mapping.ToString();
        Assert.Contains("extensionAttribute1", result);
        Assert.Contains("distinguishedName", result);
        Assert.Contains("ActiveDirectory", result);
    }
}
