using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloudscribe.Web.Navigation;
using DiscordMafia.Services;
using MafiaWeb.ViewModels.Role;
using DiscordMafia.Config;
using System.IO;
using MafiaWeb.ViewModels.Gamemode;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace MafiaWeb.Controllers
{
    public class HelpController : Controller
    {
        protected MainSettings settings;

        public HelpController(MainSettings settings)
        {
            this.settings = settings;
        }

        public IActionResult Gamemodes()
        {
            ViewData["Title"] = "Режимы игры";

            IEnumerable<string> directories = Directory.GetDirectories(Path.Combine(settings.ConfigPath, "Gametypes"));
            directories = directories.Prepend(null);
            return View(from d in directories select new Gamemode { Name = Path.GetFileName(d) });
        }

        public IActionResult Roles(string modeId = null)
        {
            ViewData["Title"] = "Роли";
            var modeRegex = new Regex(@"^[a-zA-Z0-9\-]+$");
            if (modeId != null && !modeRegex.IsMatch(modeId))
            {
                return NotFound();
            }
            
            ViewBag.ModeSettings = new GameSettings(settings, modeId);
            if (modeId != null)
            {
                var currentCrumbAdjuster = new NavigationNodeAdjuster(Request.HttpContext)
                {
                    KeyToAdjust = "HelpGamemodeView",
                    AdjustedText = modeId
                };
                currentCrumbAdjuster.AddToContext();
                ViewData["Title"] = $"Роли режима {modeId}";
            }
            return View(from r in RoleInfo.AvailableRoles group r by r.Role.Team into g orderby g.Key select g);
        }
    }
}
