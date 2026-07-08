using System.Threading.RateLimiting;

namespace Services.Options;

public class RateLimitingOptions : DelegatingHandler
{
    private readonly RateLimiter _limiter;
    
    public RateLimitingOptions()
    {
        _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions()
        {
            TokenLimit = 10,
            TokensPerPeriod = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken ct)
    {
        using var lease = await _limiter.AcquireAsync(1, ct);
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("Rate limit is not acquired");
        }

        return await base.SendAsync(request, ct);
    }
    
    
}