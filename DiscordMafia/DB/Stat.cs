using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using DiscordMafia.Client;
using Microsoft.EntityFrameworkCore;
using DiscordMafia.Config;

namespace DiscordMafia.DB
{
    public static class Stat
    {
        private static readonly HashSet<string> allowedFields = new HashSet<string>()
        {
            "total_points", "rate", "games", "wins"
        };

        public static IEnumerable<User> GetTop(string field, int countPerPage = 20, int fromPage = 1)
        {
            if (!allowedFields.Contains(field))
            {
                field = allowedFields.First();
            }
            var limit = Math.Min(Math.Max(countPerPage, 1), 300);
            var offset = (Math.Max(fromPage, 1) - 1) * countPerPage;

            using (var context = new GameContext())
            {
                var dbUsers = context.Users.AsNoTracking().Skip(offset).Take(limit);
                switch (field)
                {
                    case "total_points":
                        dbUsers = dbUsers.OrderByDescending(u => u.TotalPoints);
                        break;
                    case "rate":
                        dbUsers = dbUsers.OrderByDescending(u => u.Rate);
                        break;
                    case "games":
                        dbUsers = dbUsers.OrderByDescending(u => u.GamesPlayed);
                        break;
                    case "wins":
                        dbUsers = dbUsers.OrderByDescending(u => u.Wins);
                        break;
                }

                return dbUsers.ToList();
            }
        }

        public static void RecalculateAll()
        {
            using (var context = new GameContext())
            {
                var dbUsers = context.Users.ToList();
                foreach (var user in dbUsers)
                {
                    user.RecalculateStats();
                }
                context.SaveChanges();
            }
        }

        public static string GetStatAsString(MessageBuilder messageBuilder, UserWrapper user)
        {
            var dbUser = User.FindById(user.Id);
            var winsPercent = dbUser.GamesPlayed > 0 ? 100.0 * dbUser.Wins / dbUser.GamesPlayed : 0.0;
            var survivalsPercent = dbUser.GamesPlayed > 0 ? 100.0 * dbUser.Survivals / dbUser.GamesPlayed : 0.0;
            var pointsAverage = dbUser.GamesPlayed > 0 ? 1.0 * dbUser.TotalPoints / dbUser.GamesPlayed : 0.0;
            return messageBuilder.GetTextSimple("UserStatTemplate", new Dictionary<string, object> {
                ["name"] = MessageBuilder.Encode(user.Username),
                ["gamesPlayed"] = dbUser.GamesPlayed,
                ["survivabilityTotal"] = dbUser.Survivals,
                ["survivabilityPercent"] = survivalsPercent.ToString("0.00"),
                ["wins"] = dbUser.Wins,
                ["winsPercent"] = winsPercent.ToString("0.00"),
                ["winStreak"] = dbUser.WinStreak,
                ["loseStreak"] = dbUser.LoseStreak,
                ["totalPoints"] = dbUser.TotalPoints,
                ["averagePoints"] = pointsAverage.ToString("0.00"),
                ["rate"] = dbUser.Rate.ToString("0.00"),
            });
        }
    }
}
