using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using DiscordMafia.Client;

namespace DiscordMafia.DB
{
    public static class Stat
    {
        private static HashSet<string> allowedFields = new HashSet<string>()
        {
            "total_points", "rate", "games", "wins"
        };

        public static IEnumerable<User> GetTop(string field, int howMany = 20)
        {
            if (!allowedFields.Contains(field))
            {
                field = allowedFields.First();
            }
            howMany = Math.Min(Math.Max(howMany, 1), 300);
            var parameters = new SQLiteParameter[] { new SQLiteParameter(":limit", howMany) };
            return User.findAllByCondition($"ORDER BY {field} DESC LIMIT :limit", parameters);
        }

        public static void RecalculateAll()
        {
            var users = User.findAllByCondition($"", new SQLiteParameter[0]);
            foreach (var user in users)
            {
                user.RecalculateStats();
                user.Save();
            }
        }

        public static string GetTopAsString(Config.MessageBuilder messageBuilder, int howMany = 20)
        {
            var message = "Лучшие игроки по очкам: " + Environment.NewLine;
            var index = 0;
            foreach (var user in GetTop("total_points", howMany))
            {
                message += String.Format("{0}. {1} - {2}" + Environment.NewLine, ++index, messageBuilder.FormatName(user.firstName, user.lastName, user.username), user.totalPoints);
            }

            message += Environment.NewLine + "Лучшие игроки по рейтингу: " + Environment.NewLine;
            index = 0;
            foreach (var user in GetTop("rate", howMany))
            {
                message += String.Format("{0}. {1} - {2}" + Environment.NewLine, ++index, messageBuilder.FormatName(user.firstName, user.lastName, user.username), user.rate.ToString("0.00"));
            }
            return message;
        }

        public static string GetStatAsString(UserWrapper user)
        {
            var dbUser = User.findById(user.Id);
            var message = "Ваша статистика:" + Environment.NewLine;
            message += $"Всего игр: {dbUser.gamesPlayed}{Environment.NewLine}";
            message += $"Пережил игр: {dbUser.survivals}{Environment.NewLine}";
            message += $"Побед: {dbUser.wins}{Environment.NewLine}";
            message += $"Очков: {dbUser.totalPoints}{Environment.NewLine}";
            message += $"Рейтинг: {dbUser.rate.ToString("0.00")}{Environment.NewLine}";
            return message;
        }
    }
}
