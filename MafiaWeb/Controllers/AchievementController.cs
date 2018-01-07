using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiscordMafia.DB;
using cloudscribe.Web.Navigation;
using DiscordMafia.Achievement;
using DiscordMafia.Services;

namespace MafiaWeb.Controllers
{
    public class AchievementController : Controller
    {
        private GameContext dbContext;
        private ILanguage language;
        public AchievementController(GameContext context, ILanguage lang)
        {
            dbContext = context;
            language = lang;
        }
        
        public IActionResult List()
        {
            ViewData["Title"] = "Достижения";
            return View(AchievementManager.GetAllowedAchievements());
        }

        public IActionResult Index(string id)
        {
            var achievement = AchievementManager.GetAchievementInfo(id);
            if (achievement == null)
            {
                return NotFound();
            }
            var dbAchievements = dbContext.Achievements.Include(a => a.User).Where(a => a.AchievementId == achievement.Id).OrderBy(a => a.AchievedAtInt);
            SelectAchievement(achievement);
            return View(new ViewModels.Achievement.View { Achievement = achievement, DbAchievements = dbAchievements });
        }

        protected void SelectAchievement(DiscordMafia.Achievement.Achievement achievement)
        {
            var currentCrumbAdjuster = new NavigationNodeAdjuster(Request.HttpContext)
            {
                KeyToAdjust = "AchievementView",
                AdjustedText = achievement.GetName(language)
            };
            currentCrumbAdjuster.AddToContext();
        }
    }
}
