using System.Linq;
using DiscordMafia.Services;
using DiscordMafia.Config.Lang;
using System;

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

            var result = Enumerable.Repeat($"#ITEM_NAME_{item.GetType().Name}", 6).ToArray();
            if (info?.NameCases != null)
            {
                Array.Copy(info.NameCases, result, info.NameCases.Length);
            }
            return result;
        }

        public static string GetDescription(this BaseItem item, ILanguage language)
        {
            return GetItemLangInfo(item, language)?.Description ?? $"#ITEM_DESC_{item.GetType().Name}";
        }

        private static ItemMessages.ItemMessage GetItemLangInfo(this BaseItem item, ILanguage language)
        {
            if (language.ItemMessages.TryGetValue(item.GetType().Name, out var result))
            {
                return result;
            }
            return null;
        }
    }
}
