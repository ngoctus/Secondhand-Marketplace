using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace SecondHandMarketplace.Web.Services.Health;

public sealed class RedisHealthCheck(IConnectionMultiplexer connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await connection.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis responded in {latency.TotalMilliseconds:0} ms.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", exception);
        }
    }
}
