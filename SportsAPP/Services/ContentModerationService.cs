using SportsAPP.Models;
using System.Text.RegularExpressions;

namespace SportsAPP.Services
{
    public class ContentModerationService : IContentModerationService
    {
        private readonly GroqApiClient? _groqClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ContentModerationService> _logger;

        // Multilingual profanity lists
        private static readonly HashSet<string> BannedWordsRomanian = new(StringComparer.OrdinalIgnoreCase)
        {
            "prost", "idiot", "prostule", "pula", "muie", "cacat", "nenorocit", 
            "fuck", "retardat", "curva", "curve", "jeg", "jegoasa", "pulă", 
            "muist", "târfă", "tarfa", "cretinule", "nemernic", "nemernicule"
        };

        private static readonly HashSet<string> BannedWordsEnglish = new(StringComparer.OrdinalIgnoreCase)
        {
            "fuck", "shit", "bitch", "asshole", "cunt", "dick", "pussy", 
            "bastard", "damn", "piss", "cock", "slut", "whore", "fag", 
            "nigger", "retard", "idiot", "stupid", "dumb", "moron"
        };

        private static readonly HashSet<string> BannedWordsOther = new(StringComparer.OrdinalIgnoreCase)
        {
            // Spanish
            "puta", "mierda", "joder", "cabrón", "pendejo", "culero",
            // French
            "merde", "putain", "connard", "salope",
            // German
            "scheiße", "arschloch", "fotze", "hurensohn"
        };

        public ContentModerationService(
            IConfiguration configuration,
            ILogger<ContentModerationService> logger,
            GroqApiClient? groqClient = null)
        {
            _configuration = configuration;
            _logger = logger;
            _groqClient = groqClient;
        }

        public async Task<ModerationResult> ModerateAsync(string content, ModerationLevel maxLevel = ModerationLevel.AIAnalysis)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new ModerationResult
                {
                    IsApproved = false,
                    Reason = "Conținutul nu poate fi gol.",
                    Level = ModerationLevel.LocalFilter
                };
            }

            // Level 1: Local profanity filter (fast)
            var localResult = CheckLocalFilter(content);
            if (!localResult.IsApproved)
            {
                return localResult;
            }

            // Level 2: AI Analysis (if enabled and requested)
            if (maxLevel >= ModerationLevel.AIAnalysis && _groqClient != null)
            {
                var aiEnabled = _configuration.GetValue<bool>("GroqApi:Enabled", false);
                if (aiEnabled)
                {
                    try
                    {
                        var aiResult = await CheckAIModeration(content);
                        if (!aiResult.IsApproved)
                        {
                            return aiResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AI moderation failed, falling back to local filter only");
                        // Continue if AI fails - graceful degradation
                    }
                }
            }

            return new ModerationResult
            {
                IsApproved = true,
                Level = ModerationLevel.LocalFilter,
                ConfidenceScore = 1.0
            };
        }

        private ModerationResult CheckLocalFilter(string content)
        {
            var contentLower = content.ToLower();
            
            // Remove common evasion techniques
            var normalized = Regex.Replace(contentLower, @"[*_\-\s]+", "");
            
            // Check Romanian words
            foreach (var word in BannedWordsRomanian)
            {
                if (contentLower.Contains(word) || normalized.Contains(word.ToLower()))
                {
                    return new ModerationResult
                    {
                        IsApproved = false,
                        Reason = "Conținutul conține limbaj inadecvat sau ofensator.",
                        Level = ModerationLevel.LocalFilter,
                        ConfidenceScore = 1.0
                    };
                }
            }

            // Check English words
            foreach (var word in BannedWordsEnglish)
            {
                if (contentLower.Contains(word) || normalized.Contains(word.ToLower()))
                {
                    return new ModerationResult
                    {
                        IsApproved = false,
                        Reason = "Content contains inappropriate or offensive language.",
                        Level = ModerationLevel.LocalFilter,
                        ConfidenceScore = 1.0
                    };
                }
            }

            // Check other languages
            foreach (var word in BannedWordsOther)
            {
                if (contentLower.Contains(word) || normalized.Contains(word.ToLower()))
                {
                    return new ModerationResult
                    {
                        IsApproved = false,
                        Reason = "El contenido contiene lenguaje inapropiado.",
                        Level = ModerationLevel.LocalFilter,
                        ConfidenceScore = 1.0
                    };
                }
            }

            return new ModerationResult
            {
                IsApproved = true,
                Level = ModerationLevel.LocalFilter,
                ConfidenceScore = 0.8
            };
        }

        private async Task<ModerationResult> CheckAIModeration(string content)
        {
            if (_groqClient == null)
            {
                return new ModerationResult { IsApproved = true, Level = ModerationLevel.LocalFilter };
            }

            var prompt = $@"Analyze the following text for inappropriate content. Check for:
- Profanity or vulgar language
- Personal attacks or insults
- Hate speech or discrimination
- Threats or violence
- Spam or malicious content

Text to analyze: ""{content}""

Respond ONLY with one of these formats:
- APPROVED if the content is acceptable
- BLOCKED: [brief reason] if the content violates guidelines

Response:";

            var response = await _groqClient.CompleteAsync(prompt);
            
            if (response.StartsWith("BLOCKED", StringComparison.OrdinalIgnoreCase))
            {
                var reason = response.Length > 8 ? response.Substring(8).Trim() : "Conținut inadecvat detectat de AI";
                return new ModerationResult
                {
                    IsApproved = false,
                    Reason = reason,
                    Level = ModerationLevel.AIAnalysis,
                    ConfidenceScore = 0.9
                };
            }

            return new ModerationResult
            {
                IsApproved = true,
                Level = ModerationLevel.AIAnalysis,
                ConfidenceScore = 0.95
            };
        }
    }
}
