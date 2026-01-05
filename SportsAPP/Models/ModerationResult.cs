namespace SportsAPP.Models
{
    public class ModerationResult
    {
        public bool IsApproved { get; set; }
        public string? Reason { get; set; }
        public ModerationLevel Level { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public enum ModerationLevel
    {
        LocalFilter = 1,
        AIAnalysis = 2,
        AdminReview = 3
    }

    public enum RateLimitType
    {
        Post = 1,
        Comment = 2
    }
}
