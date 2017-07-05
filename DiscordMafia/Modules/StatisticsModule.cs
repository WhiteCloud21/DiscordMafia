using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordMafia.Client;
using DiscordMafia.Config;
using DiscordMafia.DB;
using DiscordMafia.Roles;
using Microsoft.Data.Sqlite;

namespace DiscordMafia.Modules
{
    public class StatisticsModule : ModuleBase
    {
        private Game _game;
        private MainSettings _settings;

        private SqliteConnection _connection;

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
            await ReplyAsync(MessageBuilder.Markup(_game.achievementManager.GetAchievementsAsString(user)));
        }

        [Command("stats"), Summary("Выводит статистику."), Alias("статы")]
        public async Task Statistics([Summary("Пользователь, по которому отобразить статистику")] IUser queryUser = null)
        {
            var user = new UserWrapper(queryUser != null ? queryUser : Context.User);
            await ReplyAsync(MessageBuilder.Markup(MessageBuilder.Encode(Stat.GetStatAsString(user))));
            await ReplyAsync(MessageBuilder.Markup(_game.achievementManager.GetAchievementsAsString(user)));
        }
    }
}