using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SecondHandMarketplace.Web.Services.Health;

public sealed class S3StorageHealthCheck(IAmazonS3 s3, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucket = configuration["Storage:S3:Bucket"];
            if (string.IsNullOrWhiteSpace(bucket))
                return HealthCheckResult.Unhealthy("S3 bucket is not configured.");
            await s3.HeadBucketAsync(new HeadBucketRequest { BucketName = bucket }, cancellationToken);
            return HealthCheckResult.Healthy("S3 bucket is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("S3 bucket is unavailable.", exception);
        }
    }
}
