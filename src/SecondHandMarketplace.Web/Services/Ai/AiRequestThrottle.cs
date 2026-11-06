using SecondHandMarketplace.Web.Common.Constants;

namespace SecondHandMarketplace.Web.Services.Ai;

public sealed record AiThrottleLimits(int PerMinute, int PerHour);

/// <summary>Hạn mức mặc định theo yêu cầu C3 (lần 2). Tham số hóa để unit test không phải chờ thời gian thật.</summary>
public sealed class AiThrottleOptions
{
    public AiThrottleLimits User { get; init; } = new(5, 30);
    public AiThrottleLimits Admin { get; init; } = new(10, 60);

    /// <summary>Số request AI đang được xử lý đồng thời tối đa của MỖI tài khoản.</summary>
    public int MaxConcurrentPerUser { get; init; } = 2;

    /// <summary>
    /// Thời gian tối đa một request được phép chờ (quota + slot đồng thời). Cần chờ lâu hơn (ví dụ hết quota giờ)
    /// thì từ chối ngay bằng 429 kèm Retry-After thay vì giữ kết nối hàng chục phút.
    /// </summary>
    public TimeSpan MaxWait { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Gợi ý Retry-After khi bị từ chối vì quá nhiều request đang xử lý.</summary>
    public TimeSpan BusyRetryAfter { get; init; } = TimeSpan.FromSeconds(10);
}

public enum AiThrottleRejection { None, RateLimited, Busy }

public sealed class AiThrottleAcquireResult
{
    private AiThrottleAcquireResult(IDisposable? lease, AiThrottleRejection rejection, TimeSpan retryAfter)
    {
        Lease = lease;
        Rejection = rejection;
        RetryAfter = retryAfter;
    }

    public bool Acquired => Lease is not null;

    /// <summary>Phải Dispose khi xử lý xong (kể cả lỗi/hủy) để trả slot đồng thời.</summary>
    public IDisposable? Lease { get; }

    public AiThrottleRejection Rejection { get; }
    public TimeSpan RetryAfter { get; }

    internal static AiThrottleAcquireResult Granted(IDisposable lease) => new(lease, AiThrottleRejection.None, TimeSpan.Zero);
    internal static AiThrottleAcquireResult Rejected(AiThrottleRejection reason, TimeSpan retryAfter) => new(null, reason, retryAfter);
}

public interface IAiRequestThrottle
{
    /// <summary>
    /// Chờ đến khi tài khoản còn quota (phút VÀ giờ) rồi mới chờ slot đồng thời, nên request chỉ đang đợi quota không chiếm slot.
    /// Ném OperationCanceledException khi <paramref name="cancellationToken"/> bị hủy (client ngắt kết nối); mọi tài nguyên
    /// đã giữ được trả lại. Hết thời gian chờ tối đa thì trả về kết quả bị từ chối (không ném exception).
    /// </summary>
    Task<AiThrottleAcquireResult> AcquireAsync(string userId, string role, CancellationToken cancellationToken = default);
}

/// <summary>
/// Giới hạn theo từng tài khoản (khóa là User ID lấy từ claim đã xác thực, không phải IP, không nhận từ client):
///  - Cửa sổ trượt: USER 5/phút và 30/giờ, ADMIN 10/phút và 60/giờ, áp dụng ĐỒNG THỜI.
///  - Tối đa 2 request đang xử lý đồng thời mỗi tài khoản.
/// Trạng thái lưu trong bộ nhớ của một tiến trình (khởi động lại hoặc chạy nhiều instance thì không chia sẻ).
/// </summary>
public sealed class AiRequestThrottle : IAiRequestThrottle
{
    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

    private readonly TimeProvider _time;
    private readonly AiThrottleOptions _options;
    private readonly object _sync = new();
    private readonly Dictionary<string, UserState> _users = new(StringComparer.Ordinal);
    private DateTimeOffset _lastSweep;

    public AiRequestThrottle(TimeProvider time, AiThrottleOptions? options = null)
    {
        _time = time;
        _options = options ?? new AiThrottleOptions();
        _lastSweep = _time.GetUtcNow();
    }

    /// <summary>Số tài khoản đang được theo dõi (chỉ để chẩn đoán/kiểm thử; tài khoản không hoạt động được dọn tự động).</summary>
    public int TrackedUserCount
    {
        get { lock (_sync) return _users.Count; }
    }

    public async Task<AiThrottleAcquireResult> AcquireAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var limits = role == AppRoles.Admin ? _options.Admin : _options.User;
        var state = Enter(userId);

        DateTimeOffset? reservation = null;
        var leased = false;
        try
        {
            var start = _time.GetUtcNow();
            using var timeoutSource = new CancellationTokenSource(_options.MaxWait, _time);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

            try
            {
                // 1) Chờ quota trước: chưa giữ slot đồng thời trong lúc chỉ đang đợi quota.
                var (stamp, mustWait) = await ReserveQuotaAsync(state, limits, start + _options.MaxWait, linked.Token);
                if (stamp is null) return AiThrottleAcquireResult.Rejected(AiThrottleRejection.RateLimited, RoundUp(mustWait));
                reservation = stamp;

                // 2) Rồi mới chờ slot xử lý đồng thời của chính tài khoản này.
                await state.Concurrency.WaitAsync(linked.Token);
                leased = true;
                return AiThrottleAcquireResult.Granted(new Lease(this, state));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Hết thời gian chờ tối đa (không phải client hủy).
                return reservation is null
                    ? AiThrottleAcquireResult.Rejected(AiThrottleRejection.RateLimited, RoundUp(CurrentWait(state, limits)))
                    : AiThrottleAcquireResult.Rejected(AiThrottleRejection.Busy, _options.BusyRetryAfter);
            }
        }
        finally
        {
            if (!leased)
            {
                // Request chưa được xử lý thì không tính vào quota; không giữ lại slot nào.
                if (reservation is { } stamp) Refund(state, stamp);
                Exit(state);
            }
        }
    }

    private async Task<(DateTimeOffset? Stamp, TimeSpan Wait)> ReserveQuotaAsync(
        UserState state, AiThrottleLimits limits, DateTimeOffset deadline, CancellationToken ct)
    {
        // Mỗi tài khoản chỉ một request tại một thời điểm đang "xếp hàng chờ quota": các request cùng tài khoản được phục vụ lần lượt.
        await state.QuotaGate.WaitAsync(ct);
        try
        {
            while (true)
            {
                var now = _time.GetUtcNow();
                TimeSpan wait;
                lock (state.History)
                {
                    Purge(state.History, now);
                    wait = ComputeWait(state.History, now, limits);
                    if (wait <= TimeSpan.Zero)
                    {
                        state.History.Add(now);
                        return (now, TimeSpan.Zero);
                    }
                }

                // Phải chờ quá thời gian cho phép thì từ chối ngay, không ngủ vô ích.
                if (now + wait > deadline) return (null, wait);
                await Task.Delay(wait, _time, ct);
            }
        }
        finally
        {
            state.QuotaGate.Release();
        }
    }

    /// <summary>
    /// Thời gian phải chờ để một request mới thỏa CẢ HAI hạn mức. Với lịch sử tăng dần: request thứ (limit) gần nhất
    /// phải hết hạn thì mới có chỗ trống; lấy mức chờ lớn hơn của phút và giờ nên hết chỗ giờ thì giải phóng phút không đủ.
    /// </summary>
    internal static TimeSpan ComputeWait(List<DateTimeOffset> history, DateTimeOffset now, AiThrottleLimits limits)
    {
        var wait = TimeSpan.Zero;

        var minuteStart = now - Minute;
        var inMinute = 0;
        for (var i = history.Count - 1; i >= 0 && history[i] > minuteStart; i--) inMinute++;
        if (inMinute >= limits.PerMinute)
            wait = Max(wait, history[^limits.PerMinute] + Minute - now);

        if (history.Count >= limits.PerHour)
            wait = Max(wait, history[^limits.PerHour] + Hour - now);

        return wait;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a >= b ? a : b;

    private static TimeSpan RoundUp(TimeSpan value) =>
        TimeSpan.FromSeconds(Math.Max(1, Math.Ceiling(value.TotalSeconds)));

    private static void Purge(List<DateTimeOffset> history, DateTimeOffset now)
    {
        var cutoff = now - Hour;
        history.RemoveAll(t => t <= cutoff);
    }

    private TimeSpan CurrentWait(UserState state, AiThrottleLimits limits)
    {
        var now = _time.GetUtcNow();
        lock (state.History)
        {
            Purge(state.History, now);
            return ComputeWait(state.History, now, limits);
        }
    }

    private static void Refund(UserState state, DateTimeOffset stamp)
    {
        lock (state.History) state.History.Remove(stamp);
    }

    private UserState Enter(string userId)
    {
        lock (_sync)
        {
            var now = _time.GetUtcNow();
            if (now - _lastSweep >= SweepInterval)
            {
                Sweep(now);
                _lastSweep = now;
            }

            if (!_users.TryGetValue(userId, out var state))
            {
                state = new UserState(_options.MaxConcurrentPerUser);
                _users[userId] = state;
            }
            state.References++;
            return state;
        }
    }

    private void Exit(UserState state)
    {
        lock (_sync) state.References--;
    }

    /// <summary>Gọi trong _sync: bỏ tài khoản không còn request nào và đã hết lịch sử trong 1 giờ để bộ nhớ không phình mãi.</summary>
    private void Sweep(DateTimeOffset now)
    {
        foreach (var key in _users.Keys.ToList())
        {
            var state = _users[key];
            if (state.References != 0) continue;
            lock (state.History)
            {
                Purge(state.History, now);
                if (state.History.Count == 0) _users.Remove(key);
            }
        }
    }

    private sealed class UserState
    {
        public UserState(int maxConcurrent) => Concurrency = new SemaphoreSlim(maxConcurrent, maxConcurrent);

        public List<DateTimeOffset> History { get; } = new();
        public SemaphoreSlim QuotaGate { get; } = new(1, 1);
        public SemaphoreSlim Concurrency { get; }
        public int References { get; set; }   // chỉ đọc/ghi trong _sync
    }

    private sealed class Lease : IDisposable
    {
        private readonly AiRequestThrottle _owner;
        private readonly UserState _state;
        private int _disposed;

        public Lease(AiRequestThrottle owner, UserState state)
        {
            _owner = owner;
            _state = state;
        }

        public void Dispose()
        {
            // Idempotent: Dispose nhiều lần không trả dư slot.
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _state.Concurrency.Release();
            _owner.Exit(_state);
        }
    }
}
