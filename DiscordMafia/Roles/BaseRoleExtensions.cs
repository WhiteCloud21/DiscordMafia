using System.Linq;
using DiscordMafia.Services;
using DiscordMafia.Config.Lang;
using System;
using System.Collections.Generic;

namespace DiscordMafia.Roles
{
    public static class BaseRoleExtensions
    {
        private static Dictionary<BaseRole, RoleMessages.IRoleMessage> variantsCache = new Dictionary<BaseRole, RoleMessages.IRoleMessage>();
        private static Dictionary<RoleMessages.RoleMessage, List<RoleMessages.Variant>> availableVariantsCache = new Dictionary<RoleMessages.RoleMessage, List<RoleMessages.Variant>>();
        private static Random r = new Random();
        public static void ClearCache(Game game)
        {
            var rolesToRemove = variantsCache.Where(kvp => kvp.Key.Player == null || kvp.Key.Player.Game == game).Select(kvp => kvp.Key).ToList();
            foreach (var role in rolesToRemove)
            {
                variantsCache.Remove(role);
            }
            availableVariantsCache.Clear();
        }

        public static string GetName(this BaseRole role, ILanguage language)
        {
            return GetItemLangInfo(role, language)?.Name ?? $"#ROLE_NAME_{role.GetType().Name}";
        }

        public static string GetImage(this BaseRole role, ILanguage language)
        {
            return GetItemLangInfo(role, language)?.ImagePath ?? $"roles/card{role.GetType().Name}.png";
        }

        public static string[] GetNameCases(this BaseRole role, ILanguage language)
        {
            var info = GetItemLangInfo(role, language);
            var result = Enumerable.Repeat($"#ROLE_NAME_{role.GetType().Name}", 6).ToArray();
            if (info?.NameCases != null)
            {
                Array.Copy(info.NameCases, result, info.NameCases.Length);
            }
            return result;
        }

        public static string GetDescription(this BaseRole role, ILanguage language)
        {
            string description = language.Messages.get($"RoleHelp_{role.GetType().Name}");
            return !string.IsNullOrEmpty(description) ? description : $"#ROLE_DESC_{role.GetType().Name}";
        }

        private static RoleMessages.IRoleMessage GetItemLangInfo(this BaseRole role, ILanguage language)
        {
            if (role?.Player != null && variantsCache.ContainsKey(role))
            {
                return variantsCache[role];
            }
            if (language.RoleMessages.TryGetValue(role.GetType().Name, out var result))
            {
                if (role?.Player != null)
                {
                    variantsCache[role] = result;
                    if (!availableVariantsCache.ContainsKey(result))
                    {
                        if (result.Variants != null && result.Variants.Length > 0)
                        {
                            availableVariantsCache[result] = result.Variants.ToList();
                        }
                        else
                        {
                            availableVariantsCache[result] = new List<RoleMessages.Variant>();
                        }
                    }
                    if (availableVariantsCache.ContainsKey(result) && availableVariantsCache[result].Count > 0)
                    {
                        int idx = r.Next(availableVariantsCache[result].Count);
                        variantsCache[role] = availableVariantsCache[result][idx];
                        availableVariantsCache[result].RemoveAt(idx);
                    }
                    return variantsCache[role];
                }
                return result;
            }
            return null;
        }
    }
}
