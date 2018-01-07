using System.Linq;
using DiscordMafia.Services;
using DiscordMafia.Config.Lang;

namespace DiscordMafia.Items
{
    public static class BaseItemExtensions
    {
        public static string GetName(this BaseItem item, ILanguage language)
        {
            return GetItemLangInfo(item, language)?.Name ?? $"#ITEM_NAME_{item.GetType().Name}";
        }

        public static string[] GetNameCases(this BaseItem item, ILanguage language)
        {
            var info = GetItemLangInfo(item, language);
            if (info?.NameCases != null)
            {
                return info.NameCases;
            }
            return Enumerable.Repeat($"#ITEM_NAME_{item.GetType().Name}", 6).ToArray();
        }

        public static string GetDescription(this BaseItem item, ILanguage language)
        {
            return GetItemLangInfo(item, language)?.Description ?? $"#ITEM_DESC_{item.GetType().Name}";
        }

        private static ItemMessages.ItemMessage GetItemLangInfo(this BaseItem item, ILanguage language)
        {
            return language.ItemMessages[item.GetType().Name] ?? null;
        }
    }
}
