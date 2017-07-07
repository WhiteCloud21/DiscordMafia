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

        public GameActionsModule(Game game, DiscordSocketClient client, MainSettings settings)
        {
            _game = game;
            _settings = settings;
            _client = client;
        }

        [Command("start"), Summary("Запускает игру."), Alias("старт"), RequireContext(ContextType.Guild)]
        public async Task Start([Remainder] string ignored = null)
        {
            if (_game.CurrentState == GameState.Stopped)
            {
                var message = $"Начинаю набор игроков. У вас <b>{_game.Settings.PlayerCollectingTime / 1000}</b> секунд.";
                message += Environment.NewLine + "<b>/join</b> (<b>/я</b>) - Присоединиться к игре";
                _game.MessageBuilder.Text(message, false).SendPublic(_game.GameChannel);
                _game.CurrentState = GameState.PlayerCollecting;
                _game.timer.Interval = Math.Min(_game.Settings.PlayerCollectingTime, 60000);
                _game.PlayerCollectingRemainingTime = (int)(_game.Settings.PlayerCollectingTime - _game.timer.Interval);
                _game.timer.Start();
                await _client.SetGameAsync("Мафия (ожидание игроков)");
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        [Command("join"), Summary("Присоединяет игрока к игре."), Alias("я", "z")]
        public async Task Register([Remainder] string ignored = null)
        {
            var user = new UserWrapper(Context.User);
            if (_game.CurrentState == GameState.PlayerCollecting && !_game.CurrentPlayers.ContainsKey(Context.User.Id))
            {
                var playerInfo = new InGamePlayerInfo(user, _game);
                playerInfo.DbUser.Save();
                playerInfo.IsBot = Context.User.IsBot;
                _game.CurrentPlayers.Add(Context.User.Id, playerInfo);
                _game.PlayersList.Add(playerInfo);
                _game.MessageBuilder.Text(String.Format("{0} присоединился к игре! ({1}) ", playerInfo.GetName(), _game.CurrentPlayers.Count)).SendPublic(_game.GameChannel);
            }
            await Task.CompletedTask;
        }
    }
}