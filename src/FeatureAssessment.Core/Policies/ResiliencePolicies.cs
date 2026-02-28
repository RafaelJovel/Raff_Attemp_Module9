using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Net;

namespace FeatureAssessment.Core.Policies;

/// <summary>
/// Factory for creating Polly resilience policies for HTTP requests to Ollama.
/// Implements retry, timeout, and circuit breaker patterns.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates a combined resilience pipeline for Ollama HTTP requests.
    /// Includes: Timeout → Retry → Circuit Breaker
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="timeoutSeconds">Request timeout in seconds (default: 30).</param>
    /// <returns>A resilience pipeline with retry, timeout, and circuit breaker policies.</returns>
    public static ResiliencePipeline<HttpResponseMessage> CreateOllamaPipeline(
        int maxRetries = 3,
        int timeoutSeconds = 30)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(TimeSpan.FromSeconds(timeoutSeconds))
            .AddRetry(CreateRetryOptions(maxRetries))
            .AddCircuitBreaker(CreateCircuitBreakerOptions())
            .Build();
    }

    /// <summary>
    /// Creates retry strategy options for transient HTTP failures.
    /// Uses exponential backoff with jitter.
    /// </summary>
    private static RetryStrategyOptions<HttpResponseMessage> CreateRetryOptions(int maxRetries)
    {
        return new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = maxRetries,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>()
                .HandleResult(response =>
                    response.StatusCode == HttpStatusCode.RequestTimeout ||
                    response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                    response.StatusCode == HttpStatusCode.GatewayTimeout ||
                    (int)response.StatusCode >= 500)
        };
    }

    /// <summary>
    /// Creates circuit breaker options to prevent cascading failures.
    /// Opens circuit after 5 consecutive failures, half-opens after 30 seconds.
    /// </summary>
    private static CircuitBreakerStrategyOptions<HttpResponseMessage> CreateCircuitBreakerOptions()
    {
        return new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>()
                .HandleResult(response => (int)response.StatusCode >= 500)
        };
    }
}
