using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiscordMafia.DB;
using cloudscribe.Web.Navigation;

namespace MafiaWeb.Controllers
{
    public class UserController : Controller
    {
        private GameContext dbContext;
        public UserController(GameContext context)
        {
            dbContext = context;
        }
        
        public IActionResult Index(ulong id)
        {
            var user = dbContext.Users.FirstOrDefault(model => model.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            dbContext.Entry(user).Collection(u => u.Achievements).Load();
            SelectUser(user);
            return View(user);
        }
        public IActionResult Rating(ulong id)
        {
            var user = dbContext.Users.FirstOrDefault(model => model.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            var gameUsers = dbContext.GameUsers.Include(gu => gu.Game).Where(gu => gu.UserId == id).OrderByDescending(gu => gu.GameId).Take(100).ToList().Reverse<GameUser>();
            ViewData["Title"] = user.Username;
            SelectUser(user);
            return View(gameUsers);
        }

        protected void SelectUser(User user)
        {
            var currentCrumbAdjuster = new NavigationNodeAdjuster(Request.HttpContext)
            {
                KeyToAdjust = "UserView",
                AdjustedText = user.Username
            };
            currentCrumbAdjuster.AddToContext();
        }
    }
}
