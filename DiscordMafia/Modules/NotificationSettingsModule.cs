using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordMafia.Client;
using DiscordMafia.Config;
using DiscordMafia.DB;
using Microsoft.Data.Sqlite;

namespace DiscordMafia.Modules
{
    [Summary("Настройки уведомлений")]
    public class NotificationSettingsModule : BaseModule
    {
        private Game _game;
        private MainSettings _settings;
        private SqliteConnection _connection;

        public NotificationSettingsModule(Game game, SqliteConnection connection, MainSettings settings)
        {
            _game = game;
            _settings = settings;
            _connection = connection;
        }

        [Command("announceon"), Summary("Включает уведомления о начале игры"), Alias("предупреждай"), RequireContext(ContextType.DM)]
        public async Task AnnounceOn()
        {
            await AnnounceToggle(true);
        }

        [Command("announceoff"), Summary("Отключает уведомления о начале игры"), Alias("отстань"), RequireContext(ContextType.DM)]
        public async Task AnnounceOff()
        {
            await AnnounceToggle(false);
        }

        private async Task AnnounceToggle(bool value)
        {
            var user = new UserWrapper(Context.User);
            using (var context = new GameContext())
            {
                var dbUser = context.Users.SingleOrDefault(u => u.Id == user.Id);
                if (dbUser == null)
                {
                    dbUser = new User { Id = user.Id };
                }

                try
                {
                    dbUser.IsNotificationEnabled = value;
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