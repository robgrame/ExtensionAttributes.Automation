using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace RGP.ExtensionAttributes.Automation.WorkerSvc.Services
{
    public static class PollyPolicies
    {
        /// <summary>
        /// Creates a retry policy for HTTP requests with exponential backoff
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // HttpRequestException and 5XX, 408 status codes
                .OrResult(msg => !msg.IsSuccessStatusCode && ShouldRetry(msg.StatusCode))
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2, 4, 8 seconds
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var statusCode = outcome.Result?.StatusCode ?? HttpStatusCode.InternalServerError;
                        logger.LogWarning("HTTP request retry {RetryCount}/3 after {Delay}s. Status: {StatusCode}, Reason: {Reason}",
                            retryCount, timespan.TotalSeconds, statusCode, outcome.Result?.ReasonPhrase ?? outcome.Exception?.Message);
                    });
        }

        /// <summary>
        /// Creates a circuit breaker policy for HTTP requests
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, timespan) =>
                    {
                        logger.LogError("Circuit breaker opened for {Duration}s due to: {Exception}",
                            timespan.TotalSeconds, exception.Exception?.Message ?? exception.Result?.ReasonPhrase);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("Circuit breaker closed - requests will be allowed");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("Circuit breaker half-open - testing if requests can succeed");
                    });
        }

        /// <summary>
        /// Creates a timeout policy for HTTP requests
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout, ILogger logger)
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(timeout, (context, timespan, task) =>
            {
                logger.LogWarning("HTTP request timed out after {Timeout}s", timespan.TotalSeconds);
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Creates a combined policy with retry, circuit breaker, and timeout
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger, TimeSpan? timeout = null)
        {
            var timeoutPolicy = GetTimeoutPolicy(timeout ?? TimeSpan.FromSeconds(30), logger);
            var retryPolicy = GetRetryPolicy(logger);
            var circuitBreakerPolicy = GetCircuitBreakerPolicy(logger);

            // Apply policies in order: Timeout -> Retry -> Circuit Breaker
            return Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
        }

        /// <summary>
        /// Policy specifically for Microsoft Graph API calls
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetGraphApiPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => ShouldRetryGraphCall(msg.StatusCode))
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt =>
                    {
                        // Handle Graph API throttling with exponential backoff
                        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                        return baseDelay.Add(jitter);
                    },
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var statusCode = outcome.Result?.StatusCode ?? HttpStatusCode.InternalServerError;
                        
                        if (statusCode == HttpStatusCode.TooManyRequests)
                        {
                            // Check for Retry-After header
                            var retryAfter = outcome.Result?.Headers?.RetryAfter?.Delta;
                            if (retryAfter.HasValue)
                            {
                                logger.LogWarning("Graph API throttled. Retry {RetryCount}/5 after {RetryAfter}s (Retry-After header)",
                                    retryCount, retryAfter.Value.TotalSeconds);
                            }
                            else
                            {
                                logger.LogWarning("Graph API throttled. Retry {RetryCount}/5 after {Delay}s",
                                    retryCount, timespan.TotalSeconds);
                            }
                        }
                        else
                        {
                            logger.LogWarning("Graph API request retry {RetryCount}/5 after {Delay}s. Status: {StatusCode}",
                                retryCount, timespan.TotalSeconds, statusCode);
                        }
                    });
        }

        /// <summary>
        /// General retry policy for async operations (non-HTTP)
        /// </summary>
        public static IAsyncPolicy GetGeneralRetryPolicy(ILogger logger, int retryCount = 3)
        {
            return Policy
                .Handle<Exception>(ex => !(ex is ArgumentException || ex is ArgumentNullException))
                .WaitAndRetryAsync(
                    retryCount: retryCount,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        logger.LogWarning("Operation retry {RetryCount}/{MaxRetries} after {Delay}s. Exception: {Exception}",
                            retryCount, retryCount, timespan.TotalSeconds, exception.Message);
                    });
        }

        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.RequestTimeout => true,
                HttpStatusCode.TooManyRequests => true,
                HttpStatusCode.InternalServerError => true,
                HttpStatusCode.BadGateway => true,
                HttpStatusCode.ServiceUnavailable => true,
                HttpStatusCode.GatewayTimeout => true,
                _ => false
            };
        }

        private static bool ShouldRetryGraphCall(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.TooManyRequests => true, // 429 - Graph API throttling
                HttpStatusCode.InternalServerError => true, // 500
                HttpStatusCode.BadGateway => true, // 502
                HttpStatusCode.ServiceUnavailable => true, // 503
                HttpStatusCode.GatewayTimeout => true, // 504
                HttpStatusCode.RequestTimeout => true, // 408
                _ => false
            };
        }
    }
}