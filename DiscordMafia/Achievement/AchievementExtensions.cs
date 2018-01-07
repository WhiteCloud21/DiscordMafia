using DiscordMafia.Config.Lang;
using DiscordMafia.Services;

namespace DiscordMafia.Achievement
{
    public static class AchievementExtensions
    {
        public static string GetName(this Achievement achievement, ILanguage language)
        {
            return GetLangInfo(achievement, language)?.Name ?? $"#ACHIEVEMENT_NAME_{achievement.Id}";
        }

        public static string GetDescription(this Achievement achievement, ILanguage language)
        {
            return GetLangInfo(achievement, language)?.Description ?? $"#ACHIEVEMENT_DESC_{achievement.Id}";
        }

        private static AchievementMessages.AchievementMessage GetLangInfo(this Achievement achievement, ILanguage language)
        {
            if (language.AchievementMessages.TryGetValue(achievement.Id, out var result))
            {
                return result;
            }
            return null;
        }
    }
}
