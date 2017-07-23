using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiscordMafia.DB;

namespace MafiaWeb.Controllers
{
    [Route("[controller]")]
    public class ErrorController : Controller
    {
        [Route("404")]
        public IActionResult Error404()
        {
            return View();
        }

        [Route("*")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
