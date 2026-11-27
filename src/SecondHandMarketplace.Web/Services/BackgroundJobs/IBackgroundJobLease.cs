namespace SecondHandMarketplace.Web.Services.BackgroundJobs;

/// <summary>Coordinates singleton background work across web replicas when Redis is configured.</summary>
public interface IBackgroundJobLease
{
    Task<IBackgroundJobLeaseHandle?> TryAcquireAsync(string jobName, CancellationToken cancellationToken = default);
}

public interface IBackgroundJobLeaseHandle : IAsyncDisposable
{
    CancellationToken LeaseLost { get; }
}
