using StackExchange.Redis;

namespace SecondHandMarketplace.Web.Services.BackgroundJobs;

public sealed class RedisBackgroundJobLease(
    IConnectionMultiplexer? redis,
    ILogger<RedisBackgroundJobLease> logger) : IBackgroundJobLease
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RenewalInterval = TimeSpan.FromSeconds(30);
    private const string RenewScript = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('PEXPIRE', KEYS[1], ARGV[2]) else return 0 end";
    private const string ReleaseScript = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";

    public async Task<IBackgroundJobLeaseHandle?> TryAcquireAsync(string jobName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (redis is null)
            return LocalLease.Instance;

        var key = (RedisKey)$"remarket:background-job:{jobName}";
        var token = Guid.NewGuid().ToString("N");
        var acquired = await redis.GetDatabase().StringSetAsync(key, token, LeaseDuration, When.NotExists);
        return acquired ? new RedisLease(redis, key, token, LeaseDuration, RenewalInterval, logger) : null;
    }

    private sealed class LocalLease : IBackgroundJobLeaseHandle
    {
        public static LocalLease Instance { get; } = new();
        public CancellationToken LeaseLost => CancellationToken.None;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RedisLease : IBackgroundJobLeaseHandle
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly RedisKey _key;
        private readonly string _token;
        private readonly TimeSpan _duration;
        private readonly TimeSpan _renewalInterval;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _stopRenewal = new();
        private readonly CancellationTokenSource _leaseLost = new();
        private readonly Task _renewal;
        private int _disposed;

        public RedisLease(IConnectionMultiplexer redis, RedisKey key, string token,
            TimeSpan duration, TimeSpan renewalInterval, ILogger logger)
        {
            _redis = redis;
            _key = key;
            _token = token;
            _duration = duration;
            _renewalInterval = renewalInterval;
            _logger = logger;
            _renewal = RenewAsync();
        }

        public CancellationToken LeaseLost => _leaseLost.Token;

        private async Task RenewAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(_renewalInterval, _stopRenewal.Token);
                    var renewed = (long)await _redis.GetDatabase().ScriptEvaluateAsync(
                        RenewScript, [_key], [_token, (long)_duration.TotalMilliseconds]);
                    if (renewed == 0)
                    {
                        _logger.LogError("Distributed background-job lease {LeaseKey} was lost.", (string)_key!);
                        _leaseLost.Cancel();
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (_stopRenewal.IsCancellationRequested) { }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not renew distributed background-job lease {LeaseKey}.", (string)_key!);
                _leaseLost.Cancel();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            await _stopRenewal.CancelAsync();
            await _renewal;
            try
            {
                await _redis.GetDatabase().ScriptEvaluateAsync(ReleaseScript, [_key], [_token]);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not release distributed background-job lease {LeaseKey}.", (string)_key!);
            }
            _leaseLost.Dispose();
            _stopRenewal.Dispose();
        }
    }
}
