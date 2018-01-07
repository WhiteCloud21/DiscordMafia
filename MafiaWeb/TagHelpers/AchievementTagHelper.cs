using DiscordMafia.Achievement;
using DiscordMafia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Threading.Tasks;

namespace MafiaWeb.TagHelpers
{
    public class AchievementTagHelper: TagHelper
    {
        private ILanguage language;

        public AchievementTagHelper(ILanguage lang)
        {
            language = lang;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";
            var achievedAt = context.AllAttributes["data-achieved-at"]?.Value as DateTime?;
            var content = await output.GetChildContentAsync();
            var achievementId = content.GetContent();
            var achievement = AchievementManager.GetAchievementInfo(achievementId);
            if (achievement != null)
            {
                output.TagName = "a";
                output.Attributes.SetAttribute("class", output.Attributes["class"]?.Value + " achievement");
                output.Attributes.SetAttribute("title", achievement.GetName(language));
                output.Attributes.SetAttribute("href", $"/Achievement?id={achievement.Id}");
                output.Content.SetContent(achievement.Icon);
            }
        }
    }
}
