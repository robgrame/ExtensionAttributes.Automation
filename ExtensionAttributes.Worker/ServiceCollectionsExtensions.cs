using ExtensionAttributes.Automation.WorkerSvc.Config;
using AD.Automation;
using AD.Helper.Config;
using Azure.Automation;
using Azure.Automation.Config;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using System.Net;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));
        services.Configure<ADHelperSettings>(configuration.GetSection(nameof(ADHelperSettings)));
        services.Configure<EntraADHelperSettings>(configuration.GetSection(nameof(EntraADHelperSettings)));

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

        services.AddHttpClient("GraphAPI", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(5);
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            Credentials = CredentialCache.DefaultCredentials
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(nameof(EntraADHelperSettings)).Get<EntraADHelperSettings>();

        // Add GraphServiceClient
        services.AddSingleton<GraphServiceClient>(provider =>
        {
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            if (settings == null || settings.TenantId == null || settings.ClientId == null || settings.ClientSecret == null)
            {
                throw new ArgumentNullException("EntraADHelperSettings, TenantId, ClientId, and ClientSecret must not be null.");
            }

            var clientSecretCredential = new ClientSecretCredential(
                settings.TenantId, settings.ClientId, settings.ClientSecret, options);

            return new GraphServiceClient(clientSecretCredential);
        });

        // Add helper services
        services.AddSingleton<IADHelper, ADHelper>();
        services.AddSingleton<IEntraADHelper, EntraADHelper>();

        return services;
    }

}

