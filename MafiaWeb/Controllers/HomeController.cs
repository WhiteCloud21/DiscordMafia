using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiscordMafia.DB;

namespace MafiaWeb.Controllers
{
    public class HomeController : Controller
    {
        private GameContext dbContext;
        public HomeController(GameContext context)
        {
            dbContext = context;
        }

        public IActionResult Index()
        {
            return View(dbContext.Users.OrderByDescending(model => model.Rate));
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
