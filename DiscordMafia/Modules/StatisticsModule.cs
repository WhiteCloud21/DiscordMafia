using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordMafia.Client;
using DiscordMafia.Config;
using DiscordMafia.DB;
using DiscordMafia.Roles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace DiscordMafia.Modules
{
    [Summary("Команды статистики")]
    public class StatisticsModule : BaseModule
    {
        private Game _game;
        private MainSettings _settings;

        private SqliteConnection _connection;

        private static Dictionary<ulong, (DateTime actualAt, Dictionary<string, int> rolesInfo)> _roleDataCache = new Dictionary<ulong, (DateTime actualAt, Dictionary<string, int> rolesInfo)>();

        public StatisticsModule(Game game, SqliteConnection connection, MainSettings settings)
        {
            _game = game;
            _settings = settings;
            _connection = connection;
        }

        [Command("mystat"), Summary("Выводит статистику."), Alias("мойстат")]
        [RequireContext(ContextType.DM)]
        public async Task Statistics()
        {
            var user = new UserWrapper(Context.User);
            await ReplyAsync(MessageBuilder.Markup(MessageBuilder.Encode(Stat.GetStatAsString(user))));
            await ReplyAsync(MessageBuilder.Markup(_game.AchievementManager.GetAchievementsAsString(user)));

            var rolesInfo = new Dictionary<string, int>();
            var lastActualAt = DateTime.MinValue;
            using (var context = new GameContext())
            {
                if (_roleDataCache.ContainsKey(user.Id))
                {
                    (lastActualAt, rolesInfo) = _roleDataCache[user.Id];
                }
                var query = context.GameUsers.AsNoTracking().Include(gu => gu.Game).Where(gu => gu.UserId == user.Id && gu.Game.FinishedAt > lastActualAt);
                foreach (var gameUser in query)
                {
                    if (lastActualAt < gameUser.Game.FinishedAt)
                    {
                        lastActualAt = gameUser.Game.FinishedAt;
                    }
                    if (rolesInfo.ContainsKey(gameUser.Role))
                    {
                        rolesInfo[gameUser.Role]++;
                    }
                    else
                    {
                        rolesInfo[gameUser.Role] = 1;
                    }
                }
            }
            _roleDataCache[user.Id] = (lastActualAt, rolesInfo);

            var message = "<b><u>Игр по ролям:</u></b>" + Environment.NewLine;
            message += string.Join(Environment.NewLine, rolesInfo.OrderByDescending(r => r.Value).Select(r => $"<b>{Config.Roles.GetRoleInstance(r.Key)?.Name}</b> — {r.Value}"));
            await ReplyAsync(MessageBuilder.Markup(message));
        }

        [Command("stats"), Summary("Выводит статистику."), Alias("статы")]
        public async Task Statistics([Summary("Пользователь, по которому отобразить статистику")] IUser queryUser = null)
        {
            var user = new UserWrapper(queryUser ?? Context.User);
            await ReplyAsync(MessageBuilder.Markup(MessageBuilder.Encode(Stat.GetStatAsString(user))));
            await ReplyAsync(MessageBuilder.Markup(_game.AchievementManager.GetAchievementsAsString(user)));
        }
    }
}