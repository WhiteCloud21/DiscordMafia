using System;
using System.Linq;
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
    [Group("settings"), Alias("настройки"), Summary("Личные настройки")]
    public class PersonalSettingsModule : BaseModule
    {
        private Game _game;
        private MainSettings _settings;
        private SqliteConnection _connection;

        public PersonalSettingsModule(Game game, SqliteConnection connection, MainSettings settings)
        {
            _game = game;
            _settings = settings;
            _connection = connection;
        }

        [Command, Priority(-100), Summary("Выводит настройки.")]
        [RequireContext(ContextType.DM)]
        public async Task SettingsList()
        {
            var user = new UserWrapper(Context.User);
            var dbUser = User.FindById(user.Id);
            var message = "";
            foreach (var param in dbUser.Settings)
            {
                message += $"{param.Key}: {param.Value} {Environment.NewLine}";
            }
            await ReplyAsync(MessageBuilder.Markup(MessageBuilder.Encode(message)));
        }

        [Command("update"), Summary("Обновляет настройки."), Alias("обновить")]
        [RequireContext(ContextType.DM)]
        public async Task SettingsUpdate([Summary("Название параметра")] string name, [Summary("Значение параметра")] string value)
        {
            var user = new UserWrapper(Context.User);
            using (var context = new GameContext())
            {
                var dbUser = context.Users.SingleOrDefault(u => u.Id == user.Id);
                if (dbUser == null)
                {
                    dbUser = new User {Id = user.Id};
                }

                if (!dbUser.Settings.ContainsKey(name))
                {
                    await ReplyAsync($"Неизвестный параметр {name}");
                    return;
                }
                try
                {
                    dbUser.Settings[name] = value;
                    context.SaveChanges();
                    await ReplyAsync("Настройки успешно обновлены");
                }
                catch (Exception ex)
                {
                    await ReplyAsync($"Ошибка при обновлении настроек: {ex.Message}");
                }                
            }
        }
    }
}