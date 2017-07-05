using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMafia.Client;
using DiscordMafia.Config;
using DiscordMafia.DB;
using DiscordMafia.Roles;
using Microsoft.Data.Sqlite;

namespace DiscordMafia.Modules
{
    public class GameActionsModule : ModuleBase
    {
        private DiscordSocketClient _client;
        private Game _game;
        private MainSettings _settings;
        private SqliteConnection _connection;

        public GameActionsModule(Game game, DiscordSocketClient client, MainSettings settings)
        {
            _game = game;
            _settings = settings;
            _client = client;
        }

        [Command("start"), Summary("Выводит статистику."), Alias("старт"), RequireContext(ContextType.Guild)]
        public async Task Start()
        {
            if (_game.currentState == GameState.Stopped)
            {
                var message = $"Начинаю набор игроков. У вас <b>{_game.settings.PlayerCollectingTime / 1000}</b> секунд.";
                message += Environment.NewLine + "<b>/join</b> (<b>/я</b>) - Присоединиться к игре";
                _game.messageBuilder.Text(message, false).SendPublic(_game.gameChannel);
                _game.currentState = GameState.PlayerCollecting;
                _game.timer.Interval = Math.Min(_game.settings.PlayerCollectingTime, 60000);
                _game.PlayerCollectingRemainingTime = (int)(_game.settings.PlayerCollectingTime - _game.timer.Interval);
                _game.timer.Start();
                await _client.SetGameAsync("Мафия (ожидание игроков)");
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        [Command("join"), Summary("Выводит статистику."),
        Alias("я", "z")]
        public async Task Register()
        {
            var user = new UserWrapper(Context.User);
            if (_game.currentState == GameState.PlayerCollecting && !_game.currentPlayers.ContainsKey(Context.User.Id))
            {
                var playerInfo = new InGamePlayerInfo(user, _game);
                playerInfo.DbUser.Save();
                playerInfo.IsBot = Context.User.IsBot;
                _game.currentPlayers.Add(Context.User.Id, playerInfo);
                _game.playersList.Add(playerInfo);
                _game.messageBuilder.Text(String.Format("{0} присоединился к игре! ({1}) ", playerInfo.GetName(), _game.currentPlayers.Count)).SendPublic(_game.gameChannel);
            }
            await Task.CompletedTask;
        }
    }
}