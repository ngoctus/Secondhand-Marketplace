using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace SecondHandMarketplace.Web.Security;

/// <summary>Shares the named fixed-window limits between web instances when Redis is configured.</summary>
public sealed class DistributedRateLimitMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis,
    ILogger<DistributedRateLimitMiddleware> logger)
{
    private const string IncrementScript = "local n=redis.call('INCR',KEYS[1]); if n==1 then redis.call('PEXPIRE',KEYS[1],ARGV[1]); end; return {n,redis.call('PTTL',KEYS[1])}";
    private static readonly IReadOnlyDictionary<string, (int Limit, TimeSpan Window)> Policies =
        new Dictionary<string, (int, TimeSpan)>(StringComparer.OrdinalIgnoreCase)
        {
            ["login"] = (5, TimeSpan.FromMinutes(15)),
            ["password-reset"] = (5, TimeSpan.FromMinutes(10)),
            ["appeal"] = (5, TimeSpan.FromMinutes(15)),
            ["register"] = (5, TimeSpan.FromMinutes(15))
        };

    public async Task InvokeAsync(HttpContext context)
    {
        var policyName = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        if (policyName is null || !Policies.TryGetValue(policyName, out var policy))
        {
            await next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
        var key = (RedisKey)$"remarket:rate:{policyName}:{clientIp}";
        try
        {
            var result = (RedisResult[]?)await redis.GetDatabase().ScriptEvaluateAsync(
                IncrementScript,
                [key],
                [(long)policy.Window.TotalMilliseconds]);
            var count = (long)result![0];
            if (count > policy.Limit)
            {
                var ttlMs = Math.Max(1, (long)result[1]);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(ttlMs / 1000d)).ToString();
                return;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Distributed rate limiter failed for policy {PolicyName}.", policyName);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = "5";
            return;
        }

        await next(context);
    }
}
