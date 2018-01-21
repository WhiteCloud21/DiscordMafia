using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordMafia.Client;
using DiscordMafia.Config;
using DiscordMafia.DB;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using DiscordMafia.Preconditions;
using Discord;

namespace DiscordMafia.Modules
{
    [Group("mute"), RequireAdmin]
    public class MuteModule : BaseModule
    {
        private Game _game;
        private MainSettings _settings;
        private SqliteConnection _connection;

        public MuteModule(Game game, SqliteConnection connection, MainSettings settings)
        {
            _game = game;
            _settings = settings;
            _connection = connection;
        }

        [Command("blacklist")]
        public async Task MuteBlackList(IUser user)
        {
            await MuteToggle(new UserWrapper(user), true);
        }

        [Command("whitelist")]
        public async Task MuteWhiteList(IUser user)
        {
            await MuteToggle(new UserWrapper(user), false);
        }

        [Command("remove")]
        public async Task MuteRemove(IUser user)
        {
            await MuteToggle(new UserWrapper(user), null);
        }

        [Command("status")]
        public async Task MuteStatus(IUser user)
        {
            using (var context = new GameContext())
            {
                var dbUser = context.Users.SingleOrDefault(u => u.Id == user.Id);
                if (dbUser == null)
                {
                    dbUser = new User { Id = user.Id };
                }

                try
                {
                    await ReplyAsync("Status: " + Convert.ToString(dbUser.IsMuteEnabled));
                }
                catch (Exception ex)
                {
                    await ReplyAsync(ex.Message);
                }
            }
        }

        private async Task MuteToggle(UserWrapper user, bool? value)
        {
            using (var context = new GameContext())
            {
                var dbUser = context.Users.SingleOrDefault(u => u.Id == user.Id);
                if (dbUser == null)
                {
                    dbUser = new User { Id = user.Id };
                }

                try
                {
                    dbUser.IsMuteEnabled = value;
                    context.SaveChanges();
                    await ReplyAsync(_game.MessageBuilder.GetText("OK"));
                }
                catch (Exception ex)
                {
                    await ReplyAsync(ex.Message);
                }
            }
        }
    }
}