using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using Azure.Core;

namespace ExtensionAttributes.Automation.WorkerSvc.Services
{
    /// <summary>
    /// HTTP message handler that adds Azure authentication headers to Graph API REST calls
    /// </summary>
    public class GraphApiAuthenticationHandler : DelegatingHandler
    {
        private readonly TokenCredential _tokenCredential;
        private readonly ILogger<GraphApiAuthenticationHandler> _logger;
        private readonly string[] _scopes = { "https://graph.microsoft.com/.default" };

        public GraphApiAuthenticationHandler(TokenCredential tokenCredential, ILogger<GraphApiAuthenticationHandler> logger)
        {
            _tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Get access token for Microsoft Graph
                var tokenRequestContext = new TokenRequestContext(_scopes);
                var accessToken = await _tokenCredential.GetTokenAsync(tokenRequestContext, cancellationToken);

                // Add the authorization header
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
                
                _logger.LogDebug("Added authentication header to Graph API request: {Method} {Uri}", 
                    request.Method, request.RequestUri);

                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate Graph API request: {Method} {Uri}", 
                    request.Method, request.RequestUri);
                
                // Continue without authentication header - let the request fail naturally
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}