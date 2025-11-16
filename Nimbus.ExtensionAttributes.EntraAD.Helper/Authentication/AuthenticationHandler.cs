using Nimbus.ExtensionAttributes.EntraAD.Config;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Nimbus.ExtensionAttributes.EntraAD.Authentication
{
    public class AuthenticationHandler
    {
        private readonly EntraADHelperSettings _settings;
        private readonly ILogger<AuthenticationHandler> _logger;
        private AccessToken? _accessToken;
        private DateTime _tokenExpiration;

        public AuthenticationHandler(IOptions<EntraADHelperSettings> settings, ILogger<AuthenticationHandler> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<AccessToken> GetAccessTokenAsync()
        {
            try
            {

                // Check if the access token is cached and not expired within the buffer time
                if (_accessToken != null && _accessToken.Value.ExpiresOn.UtcDateTime > DateTime.UtcNow.AddMinutes(_settings.TokenExpirationBuffer))
                {
                    _logger.LogTrace("Using cached access token.");
                    return _accessToken.Value;
                }

                // Check if ClientId and TenantId are set
                if (string.IsNullOrEmpty(_settings.ClientId) || string.IsNullOrEmpty(_settings.TenantId))
                {
                    _logger.LogError("ClientId or TenantId is not set.");
                    throw new Exception("ClientId or TenantId is not set.");
                }

                _logger.LogTrace("Fetching new access token...");

                TokenCredential credential;
                if (_settings.UseClientSecret)
                {
                    credential = new ClientSecretCredential(_settings.TenantId, _settings.ClientId, _settings.ClientSecret);
                }
                else
                {
                    if (string.IsNullOrEmpty(_settings.CertificateThumbprint))
                    {
                        _logger.LogError("Certificate thumbprint is not set.");
                        throw new Exception("Certificate thumbprint is not set.");
                    }
                    var certificate = FindCertificateByThumbprint(_settings.CertificateThumbprint);
                    if (certificate == null)
                    {
                        _logger.LogError("Certificate with thumbprint {Thumbprint} not found", _settings.CertificateThumbprint);
                        throw new Exception("Certificate not found");
                    }

                    credential = new ClientCertificateCredential(_settings.TenantId, _settings.ClientId, certificate);
                }

                var tokenRequestContext = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
                var accessToken = await credential.GetTokenAsync(tokenRequestContext, default);

                _accessToken = accessToken;
                _tokenExpiration = accessToken.ExpiresOn.UtcDateTime;

                return accessToken;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Failed to access the certificate's private key: {Message}", ex.Message);
                throw new AuthenticationFailedException("ClientCertificateCredential authentication failed: Keyset does not exist", ex);
            }
            catch (CredentialUnavailableException ex)
            {
                _logger.LogError(ex, "Credential is unavailable. Please ensure the credential is correctly configured.");
                throw new InvalidOperationException("Credential is unavailable. Please ensure the credential is correctly configured.", ex);
            }
            catch (AuthenticationFailedException ex)
            {
                _logger.LogError(ex, "Authentication failed while getting access token.");
                throw new InvalidOperationException("Authentication failed. Please check your credentials and try again.", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument provided. Please check the configuration settings.");
                throw new InvalidOperationException("Invalid argument provided. Please check the configuration settings.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get access token.");
                throw new InvalidOperationException("An unexpected error occurred while getting the access token.", ex);
            }
        }

        private X509Certificate2? FindCertificateByThumbprint(string thumbprint)
        {
            _logger.LogTrace("Searching for certificate with thumbprint: {Thumbprint}", thumbprint);

            using var store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false).OfType<X509Certificate2>().FirstOrDefault();

            if (certificate != null)
            {
                _logger.LogTrace("Certificate found.");
            }
            else
            {
                _logger.LogWarning("Certificate not found.");
            }

            return certificate;
        }
    }
}
