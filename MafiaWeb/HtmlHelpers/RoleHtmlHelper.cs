using DiscordMafia.Roles;
using DiscordMafia.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MafiaWeb.HtmlHelpers
{
    public static class RoleHtmlHelper
    {
        private static Dictionary<string, BaseRole> instanceCache = new Dictionary<string, BaseRole>();

        public static HtmlString Role(this IHtmlHelper html, string roleId, ILanguage lang)
        {
            string result = "<span>";
            var role = GetRoleInstance(roleId);
            result += html.Encode(role != null ? role.GetName(lang) : roleId);
            result += "</span>";
            return new HtmlString(result);
        }

        public static BaseRole GetRoleInstance(string roleId)
        {
            if (instanceCache.ContainsKey(roleId))
            {
                return instanceCache[roleId];
            }
            else
            {
                return instanceCache[roleId] = DiscordMafia.Config.Roles.GetRoleInstance(roleId);
            }
        }
    }
}
