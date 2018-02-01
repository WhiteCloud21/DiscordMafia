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

        public static string GetDescription(this BaseRole role, ILanguage language)
        {
            string description = language.Messages.get($"RoleHelp_{role.GetType().Name}");
            return !string.IsNullOrEmpty(description) ? description : $"#ROLE_DESC_{role.GetType().Name}";
        }

        private static RoleMessages.RoleMessage GetItemLangInfo(this BaseRole role, ILanguage language)
        {
            if (language.RoleMessages.TryGetValue(role.GetType().Name, out var result))
            {
                return result;
            }
            return null;
        }
    }
}
