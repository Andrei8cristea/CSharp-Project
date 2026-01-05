using SportsAPP.Models;

namespace SportsAPP.Services
{
    public interface IContentModerationService
    {
        Task<ModerationResult> ModerateAsync(string content, ModerationLevel maxLevel = ModerationLevel.AIAnalysis);
    }
}
