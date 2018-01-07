using DiscordMafia.Roles;
using DiscordMafia.Services;
using MafiaWeb.HtmlHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace MafiaWeb.TagHelpers
{
    public class RoleTagHelper: TagHelper
    {
        private ILanguage language;

        public RoleTagHelper(ILanguage lang)
        {
            language = lang;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            var content = await output.GetChildContentAsync();
            var roleId = content.GetContent();
            var role = RoleHtmlHelper.GetRoleInstance(roleId);
            if (role != null)
            {
                output.Attributes.SetAttribute("class", output.Attributes["class"]?.Value + " role-info");
                output.Attributes.SetAttribute("title", role.GetName(language));
                output.Content.SetContent(role.GetName(language));
            }
        }
    }
}
