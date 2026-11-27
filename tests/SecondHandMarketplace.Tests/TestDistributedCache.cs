using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace SecondHandMarketplace.Tests;

internal sealed class TestDistributedCache : IDistributedCache
{
    private readonly ConcurrentDictionary<string, byte[]> _values = new(StringComparer.Ordinal);

    public byte[]? Get(string key) => _values.TryGetValue(key, out var value) ? value.ToArray() : null;
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _values[key] = value.ToArray();
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    { Set(key, value, options); return Task.CompletedTask; }
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    public void Remove(string key) => _values.TryRemove(key, out _);
    public Task RemoveAsync(string key, CancellationToken token = default)
    { Remove(key); return Task.CompletedTask; }
}
