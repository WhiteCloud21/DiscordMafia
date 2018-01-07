using System.Linq;
using DiscordMafia.Services;
using DiscordMafia.Config.Lang;

namespace DiscordMafia.Roles
{
    public static class BaseRoleExtensions
    {
        public static string GetName(this BaseRole role, ILanguage language)
        {
            return GetItemLangInfo(role, language)?.Name ?? $"#ROLE_NAME_{role.GetType().Name}";
        }

        public static string[] GetNameCases(this BaseRole role, ILanguage language)
        {
            var info = GetItemLangInfo(role, language);
            if (info?.NameCases != null)
            {
                return info.NameCases;
            }
            return Enumerable.Repeat($"#ROLE_NAME_{role.GetType().Name}", 6).ToArray();
        }

        private static RoleMessages.RoleMessage GetItemLangInfo(this BaseRole role, ILanguage language)
        {
            return language.RoleMessages[role.GetType().Name] ?? null;
        }
    }
}
