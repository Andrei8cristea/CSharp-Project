using SportsAPP.Models;

namespace SportsAPP.Services
{
    public interface IRateLimitService
    {
        Task<bool> IsAllowedAsync(string userId, RateLimitType type);
        int GetRemainingCount(string userId, RateLimitType type);
    }
}
