using Microsoft.Extensions.Caching.Memory;
using SportsAPP.Models;

namespace SportsAPP.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public RateLimitService(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<bool> IsAllowedAsync(string userId, RateLimitType type)
        {
            if (!_configuration.GetValue<bool>("RateLimiting:Enabled", true))
            {
                return true; // Rate limiting disabled
            }

            var limit = GetLimit(type);
            var cacheKey = $"RateLimit_{type}_{userId}";

            await _semaphore.WaitAsync();
            try
            {
                if (!_cache.TryGetValue(cacheKey, out int currentCount))
                {
                    currentCount = 0;
                }

                if (currentCount >= limit)
                {
                    return false; // Limit exceeded
                }

                // Increment counter
                currentCount++;
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                };
                _cache.Set(cacheKey, currentCount, cacheOptions);

                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public int GetRemainingCount(string userId, RateLimitType type)
        {
            var limit = GetLimit(type);
            var cacheKey = $"RateLimit_{type}_{userId}";

            if (_cache.TryGetValue(cacheKey, out int currentCount))
            {
                return Math.Max(0, limit - currentCount);
            }

            return limit;
        }

        private int GetLimit(RateLimitType type)
        {
            return type switch
            {
                RateLimitType.Post => _configuration.GetValue<int>("RateLimiting:PostsPerHour", 10),
                RateLimitType.Comment => _configuration.GetValue<int>("RateLimiting:CommentsPerHour", 30),
                _ => 10
            };
        }
    }
}
