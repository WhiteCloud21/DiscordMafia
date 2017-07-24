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
    public class GameController : Controller
    {
        private GameContext dbContext;
        public GameController(GameContext context)
        {
            dbContext = context;
        }
        
        public IActionResult List(ulong? userId = null)
        {
            ViewData["Title"] = "Игры";
            IQueryable<Game> games = dbContext.Games.OrderByDescending(g => g.FinishedAtInt);
            if (userId != null)
            {
                var user = dbContext.Users.SingleOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound();
                }
                ViewData["Title"] += $" {user.Username}";
                games = games.Where(g => g.Users.Any(gu => gu.UserId == userId));
            }
            return View(games);
        }

        public IActionResult Index(ulong id)
        {
            var game = dbContext.Games.FirstOrDefault(model => model.Id == id);
            if (game == null)
            {
                return NotFound();
            }
            dbContext.Entry(game).Collection(g => g.Users).Query().Include(gu => gu.User).Load();
            SelectGame(game);
            return View(game);
        }

        protected void SelectGame(Game game)
        {
            var currentCrumbAdjuster = new NavigationNodeAdjuster(Request.HttpContext)
            {
                KeyToAdjust = "GameView",
                AdjustedText = game.Id.ToString()
            };
            currentCrumbAdjuster.AddToContext();
        }
    }
}
