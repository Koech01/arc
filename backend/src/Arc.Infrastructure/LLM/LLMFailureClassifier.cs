namespace Arc.Infrastructure.LLM;


/// <summary>
/// Deterministically classifies HTTP failures for LLM operations.
/// Transient failures (e.g., 503, 429, timeouts) are retryable.
/// Non-transient failures (e.g., 400, 401, 404) are not.
/// </summary>
public static class LLMFailureClassifier
{
    public static bool IsTransientFailure(HttpResponseMessage response)
    {
        return response.StatusCode switch
        {
            // Service unavailable, too many requests, gateway timeout, service timeout
            System.Net.HttpStatusCode.ServiceUnavailable => true,
            System.Net.HttpStatusCode.TooManyRequests => true,
            System.Net.HttpStatusCode.GatewayTimeout => true,
            System.Net.HttpStatusCode.RequestTimeout => true,
            // 5xx errors are generally transient
            >= System.Net.HttpStatusCode.InternalServerError => true,
            _ => false
        };
    }

    public static bool IsTransientFailure(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            OperationCanceledException => true,
            HttpRequestException hre => hre.InnerException is TimeoutException,
            _ => false
        };
    }
}