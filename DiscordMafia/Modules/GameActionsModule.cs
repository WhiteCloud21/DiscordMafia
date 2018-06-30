using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMafia.Client;
using DiscordMafia.Config;
using DiscordMafia.DB;
using DiscordMafia.Items;
using DiscordMafia.Preconditions;
using DiscordMafia.Roles;
using static DiscordMafia.Config.MessageBuilder;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace DiscordMafia.Modules
{
    [Summary("Игровые команды")]
    public class GameActionsModule : BaseModule
    {
        private DiscordSocketClient _client;
        private Game _game;
        private DiscordClientWrapper _clientWrapper;

        public GameActionsModule(Game game, DiscordSocketClient client, DiscordClientWrapper clientWrapper)
        {
            _game = game;
            _client = client;
            _clientWrapper = clientWrapper;
        }

        [Command("startevent"), RequireContext(ContextType.DM), RequireAdmin, RequireGameState(GameState.Stopped)]
        public async Task StartEvent([Remainder] string ignored = null)
        {
            _game.MainSettings.LoadLanguage(true);
            _game.LoadSettings();
            var welcomeMessages = _game.MainSettings.Language.Meta.WelcomeMessages;
            if (welcomeMessages != null && welcomeMessages.Length > 0)
            {
                foreach (var message in welcomeMessages)
                {
                    _game.MessageBuilder.Text(message.Content).SendPublic(_clientWrapper.AnnouncerGameChannel);
                    await _clientWrapper.AnnouncerGameChannel.TriggerTypingAsync();
                    System.Threading.Thread.Sleep(message.Pause);
                }
            }
            await Start();
        }

        [Command("endevent"), RequireContext(ContextType.DM), RequireAdmin, RequireGameState(GameState.Stopped)]
        public async Task EndEvent([Remainder] string ignored = null)
        {
            _game.MainSettings.LoadLanguage(false);
            _game.LoadSettings();
            await ReplyAsync("OK");
        }

        [Command("start"), Summary("Запускает игру."), Alias("старт"), RequireContext(ContextType.Guild), RequireGameState(GameState.Stopped)]
        public async Task Start([Remainder] string ignored = null)
        {
            _game.MessageBuilder.PrepareText("PlayerCollectingStart", new Dictionary<string, object>
            {
                ["seconds"] = _game.Settings.PlayerCollectingTime / 1000,
                ["mode"] = _game.GameMode ?? "default",
            }).SendPublic(_game.GameChannel);
            _game.CurrentState = GameState.PlayerCollecting;
            _game.timer.Interval = Math.Min(_game.Settings.PlayerCollectingTime, 60000);
            _game.PlayerCollectingRemainingTime = (int)(_game.Settings.PlayerCollectingTime - _game.timer.Interval);
            _game.timer.Start();
            _game.StartedAt = DateTime.Now;
            NotifyAboutNewGame();
            await _client.SetGameAsync("Мафия (ожидание игроков)");
            await Register();
        }

        private void NotifyAboutNewGame()
        {
            var diff = DateTime.Now - _game.LastNotification;
            if (_game.Settings.MaxUsersToNotify > 0 && diff.TotalSeconds > _game.Settings.MinNotificationInterval)
            {
                byte remainingUserCount = _game.Settings.MaxUsersToNotify;
                var usersToNotify = new List<IGuildUser>();
                using (var context = new GameContext())
                {
                    var userIds = context.Users.AsNoTracking().Where(u => u.IsNotificationEnabled == true).ToDictionary(u => u.Id, u => u.Id);
                    foreach (var user in _game.GameChannel.Users)
                    {
                        if (user.Status != UserStatus.Offline && user.Status != UserStatus.Invisible && userIds.ContainsKey(user.Id))
                        {
                            remainingUserCount--;
                            usersToNotify.Add(user);
                            if (remainingUserCount == 0)
                            {
                                break;
                            }
                        }
                    }
                }
                if (usersToNotify.Count > 0)
                {
                    string notificationMessage = usersToNotify.Aggregate("", (s, u) => s += u.Mention + " ");
                    _game.MessageBuilder.Text(notificationMessage, false).SendPublic(_clientWrapper.AnnouncerGameChannel);
                    _game.LastNotification = DateTime.Now;
                }
            }
        }

        [Command("join"), Summary("Присоединяет игрока к игре."), RequireGameState(GameState.PlayerCollecting),
         Alias("я", "z")]
        public async Task Register([Remainder] string ignored = null)
        {
            var user = new UserWrapper(Context.User);
            if (!_game.CurrentPlayers.ContainsKey(user.Id))
            {
                var playerInfo = new InGamePlayerInfo(user, _game)
                {
                    IsBot = Context.User.IsBot
                };
                _game.CurrentPlayers.Add(user.Id, playerInfo);
                _game.PlayersList.Add(playerInfo);
                _game.MessageBuilder
                    .PrepareTextReplacePlayer("PlayerRegister", playerInfo, additionalReplaceDictionary: new ReplaceDictionary { ["count"] = _game.CurrentPlayers.Count })
                    .SendPublic(_game.GameChannel);
            }
            await Task.CompletedTask;
        }

        [Command("cancel"), Summary("Отменяет действие."), Alias("отмена", "нея", "don't")]
        public async Task Cancel([Remainder] string ignored = null)
        {
            if (_game.CurrentState == GameState.PlayerCollecting && _game.CurrentPlayers.ContainsKey(Context.User.Id))
            {
                var playerInfo = _game.CurrentPlayers[Context.User.Id];
                _game.CurrentPlayers.Remove(Context.User.Id);
                _game.PlayersList.Remove(playerInfo);
                _game.MessageBuilder
                    .PrepareTextReplacePlayer("PlayerUnRegister", playerInfo, additionalReplaceDictionary: new ReplaceDictionary { ["count"] = _game.CurrentPlayers.Count })
                    .SendPublic(_game.GameChannel);
                await Task.CompletedTask;
            }
            else
            {
                InGamePlayerInfo currentPlayer = _game.GetPlayerInfo(Context.User.Id);
                if (currentPlayer != null)
                {
                    if (Context.Channel is IDMChannel)
                    {
                        currentPlayer.CancelActivity();
                        await ReplyAsync(_game.MessageBuilder.GetText("YouCanceledVote"));
                    }
                    else if (_game.CurrentState == GameState.Day)
                    {
                        if (currentPlayer.CancelVote())
                        {
                            _game.MessageBuilder.PrepareTextReplacePlayer("PlayerCanceledVote", currentPlayer).SendPublic(_game.GameChannel);
                        }
                        await Task.CompletedTask;
                    }
                }
                else
                {
                    await Task.CompletedTask;
                }
            }
        }

        [Command("skip"), Summary("Позволяет пропустить текущее действие."), Alias("пропустить"), RequirePlayer]
        public async Task Skip([Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (Context.Channel is IDMChannel)
                {
                    if (currentPlayer.SkipTurn())
                    {
                        await ReplyAsync(_game.MessageBuilder.GetText("YourTurnSkipped"));
                        _game.CheckNextCheckpoint();
                    }
                    else
                    {
                        await ReplyAsync(_game.MessageBuilder.GetText("YourTurnAlreadySkipped"));
                    }
                }
                else
                {
                    if (currentPlayer.SkipTurn())
                    {
                        _game.MessageBuilder.PrepareTextReplacePlayer("PlayerSkippedTurn", currentPlayer).SendPublic(_game.GameChannel);
                        _game.CheckNextCheckpoint();
                    }
                    else
                    {
                        await Task.CompletedTask;
                    }
                }
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        [Command("imprison"), Summary("Осудить указанного игрока."), RequirePlayer, RequireGameState(GameState.Day, GameState.Evening),
         Alias("посадить", "повесить", "gjcflbnm", "gjdtcbnm")]
        public async Task Vote([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (Context.Channel is IDMChannel)
                {
                    if (currentPlayer.Role is Elder)
                    {
                        var elder = (currentPlayer.Role as Elder);
                        var playerToKill = player;
                        if (playerToKill != null && elder.PlayerToInteract == null)
                        {
                            try
                            {
                                elder.PlayerToInteract = playerToKill;
                                await ReplyAsync(_game.MessageBuilder.GetText("OK"));
                                _game.CheckNextCheckpoint();
                            }
                            catch (Exception ex)
                            {
                                _game.MessageBuilder.Text(ex.Message, false).SendPrivate(currentPlayer);
                            }
                        }
                        return;
                    }
                }
                else if (_game.CurrentState == GameState.Day) // TODO Изменить проверку
                {
                    _game.DayVote(currentPlayer, player);
                    _game.CheckNextCheckpoint();
                }
            }
            await Task.CompletedTask;
        }

        [Command("yes"), Summary("Согласиться с решением суда."), Alias("да"), RequireContext(ContextType.Guild), RequirePlayer, RequireGameState(GameState.Evening)]
        public async Task AcceptVote([Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                _game.EveningVote(currentPlayer, true);
                _game.CheckNextCheckpoint();
            }
            await Task.CompletedTask;
        }

        [Command("no"), Summary("Опротестовать решение суда."), Alias("нет"), RequireContext(ContextType.Guild), RequirePlayer, RequireGameState(GameState.Evening)]
        public async Task DeclineVote([Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                _game.EveningVote(currentPlayer, false);
                _game.CheckNextCheckpoint();
            }
            await Task.CompletedTask;
        }

        [Command("gamemode"), Summary("Возвращает текущий режим игры."), Alias("gametype", "режим"), RequireContext(ContextType.Guild)]
        public async Task GameMode()
        {
            await ReplyAsync(MessageBuilder.Encode($"Текущий режим игры: {_game.Settings.GameType}"));
        }

        [Command("buy"), Summary("Выводит список предметов."), Alias("купить"), RequireContext(ContextType.DM)]
        public async Task BuyItem()
        {
            _game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer);
            if (BaseItem.AvailableItems.Length > 0)
            {
                var response = "";
                for (var i = 0; i < BaseItem.AvailableItems.Length; i++)
                {
                    var item = BaseItem.AvailableItems[i];
                    string itemStatusKey = "ShopItemStatus_";
                    BaseItem itemInPlayer = null;
                    if (currentPlayer != null)
                    {
                        itemInPlayer = currentPlayer.GetItem(item);
                    }
                    if (itemInPlayer != null)
                    {
                        itemStatusKey += itemInPlayer.IsActive ? "IsActive" : "Unavailable";
                    }
                    else
                    {
                        itemStatusKey += "IsAvailable";
                    }
                    response += _game.MessageBuilder.GetTextSimple("ShopItemInfo", new Dictionary<string, object>() {
                        ["idx"] = i + 1,
                        ["name"] = item.GetName(_game.MainSettings.Language),
                        ["status"] = _game.MessageBuilder.GetTextSimple(itemStatusKey),
                        ["cost"] = item.Cost,
                        ["description"] = item.GetDescription(_game.MainSettings.Language)
                    });
                    response += Environment.NewLine;
                    response += Environment.NewLine;
                }
                _game.MessageBuilder.Text(response, false).SendPrivate(Context.User);
            }
            else
            {
                _game.MessageBuilder.PrepareText("ShopDisabled").SendPrivate(Context.User);
            }
            await Task.CompletedTask;
        }

        [Command("buy"), Summary("Купить предмет."), Alias("купить"), RequireContext(ContextType.DM), RequirePlayer]
        public async Task BuyItem([Summary("номер предмета")] int item, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                var itemToBuy = _game.GetItemInfo(item.ToString());
                if (itemToBuy != null)
                {
                    try
                    {
                        currentPlayer.Buy(itemToBuy);
                        await ReplyAsync(MessageBuilder.Markup(_game.MessageBuilder.GetTextSimple("ShopItemBought", new Dictionary<string, object>()
                        {
                            ["name"] = itemToBuy.GetName(_game.MainSettings.Language),
                        })));
                    }
                    catch (Exception ex)
                    {
                        _game.MessageBuilder.Text(ex.Message, false).SendPrivate(currentPlayer);
                    }
                }
            }
            await Task.CompletedTask;
        }

        [Command("kill"), Summary("Посодействовать в убийстве игрока."), Alias("убить"), RequireContext(ContextType.DM), RequirePlayer, RequireGameState(GameState.Night)]
        public async Task Kill([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (currentPlayer.Role is Highlander)
                {
                    (currentPlayer.Role as Highlander).PerformNightAction(player);
                    return;
                }
                if (currentPlayer.Role is Sheriff)
                {
                    (currentPlayer.Role as Sheriff).PerformNightAction(player);
                    return;
                }
                if (currentPlayer.Role is Prosecutor)
                {
                    (currentPlayer.Role as Prosecutor).PerformNightAction(player);
                    return;
                }
                if (currentPlayer.Role is Kamikaze)
                {
                    (currentPlayer.Role as Kamikaze).PerformNightAction(player);
                    return;
                }
                if (currentPlayer.Role is Killer)
                {
                    var killer = (currentPlayer.Role as Killer);
                    if (player != null && killer.PlayerToInteract == null)
                    {
                        killer.PlayerToInteract = player;
                        _game.NightAction(currentPlayer.Role);
                        _game.MessageBuilder.
                            PrepareTextReplacePlayer("NightAction_Killer_ToTeam", currentPlayer, additionalReplaceDictionary: new ReplaceDictionary { ["toKill"] = killer.PlayerToInteract.GetName() }).
                            SendToTeam(Team.Mafia);
                        _game.CheckNextCheckpoint();
                    }
                    return;
                }
                if (currentPlayer.Role is NeutralKiller)
                {
                    (currentPlayer.Role as NeutralKiller).PerformNightAction(player);
                    return;
                }
                _game.NightVote(currentPlayer, player);
                _game.CheckNextCheckpoint();
            }
            await Task.CompletedTask;
        }

        [Command("kill"), RequireContext(ContextType.DM), RequirePlayer(typeof(Poisoner)), RequireGameState(GameState.Day)]
        public async Task KillPoisoner([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                (currentPlayer.Role as Poisoner).PerformNightAction(player);
                _game.CheckNextCheckpoint();
            }
            await Task.CompletedTask;
        }

        [Command("curse"), Summary("Проклясть игрока."), Alias("проклясть"), RequireContext(ContextType.DM), RequirePlayer(typeof(Warlock)), RequireGameState(GameState.Night)]
        public async Task CursePlayer([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                (currentPlayer.Role as Warlock).PerformNightAction(player);
            }
            await Task.CompletedTask;
        }

        [Command("hack"), Summary("Хакнуть компьютер игрока."), Alias("хакнуть"), RequireContext(ContextType.DM), RequirePlayer(typeof(Hacker)), RequireGameState(GameState.Night)]
        public async Task HackPlayer([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                (currentPlayer.Role as Hacker).PerformNightAction(player);
            }
            await Task.CompletedTask;
        }

        [Command("check"), Summary("Проверить игрока."), Alias("пров", "проверить"), RequireContext(ContextType.DM), RequirePlayer(typeof(Commissioner), typeof(Homeless), typeof(Lawyer), typeof(Spy)), RequireGameState(GameState.Night)]
        public async Task CheckPlayer([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (currentPlayer.Role is Commissioner)
                {
                    (currentPlayer.Role as Commissioner).PerformNightAction(player);
                }
                else if (currentPlayer.Role is Homeless)
                {
                    (currentPlayer.Role as Homeless).PerformNightAction(player);
                }
                else if (currentPlayer.Role is Spy)
                {
                    (currentPlayer.Role as Spy).PerformNightAction(player);
                }
                else if (currentPlayer.Role is Lawyer)
                {
                    var lawyer = (currentPlayer.Role as Lawyer);
                    if (player != null && lawyer.PlayerToInteract == null)
                    {
                        lawyer.PlayerToInteract = player;
                        _game.NightAction(currentPlayer.Role);
                        _game.MessageBuilder.
                            PrepareTextReplacePlayer("NightAction_Lawyer_ToTeam", currentPlayer, additionalReplaceDictionary: new ReplaceDictionary { ["toCheck"] = lawyer.PlayerToInteract.GetName() }).
                            SendToTeam(Team.Mafia);
                        _game.CheckNextCheckpoint();
                    }
                }
            }
            await Task.CompletedTask;
        }

        [Command("sleep"), Summary("Переспать с игроком."), Alias("спать"), RequireContext(ContextType.DM), RequirePlayer(typeof(Wench)), RequireGameState(GameState.Night)]
        public async Task SleepWithPlayer([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                (currentPlayer.Role as Wench).PerformNightAction(player);
            }
            await Task.CompletedTask;
        }

        [Command("block"), Summary("Блокировать игрока."), Alias("блок"), RequireContext(ContextType.DM), RequirePlayer(typeof(Hoodlum)), RequireGameState(GameState.Night)]
        public async Task BlockPlayer([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                (currentPlayer.Role as Hoodlum).PerformNightAction(player);
            }
            await Task.CompletedTask;
        }

        [Command("heal"), Summary("Подлатать игрока."), Alias("лечить"), RequireContext(ContextType.DM), RequirePlayer(typeof(Doctor)), RequireGameState(GameState.Night)]
        public async Task HealPlayer([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                (currentPlayer.Role as Doctor).PerformNightAction(player);
            }
            await Task.CompletedTask;
        }

        [Command("justify"), Summary("Оправдать игрока."), Alias("оправдать"), RequireContext(ContextType.DM), RequirePlayer(typeof(Judge)), RequireGameState(GameState.Day, GameState.Evening)]
        public async Task JustifyPlayer([Summary("номер игрока")] InGamePlayerInfo player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                (currentPlayer.Role as Judge).PerformNightAction(player);
            }
            await Task.CompletedTask;
        }

        [Command("talk"), RequireContext(ContextType.DM), RequirePlayer(typeof(RabbleRouser)), RequireGameState(GameState.Day, GameState.Evening)]
        public async Task RepeatDay([Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                var rabbleRouser = (currentPlayer.Role as RabbleRouser);
                try
                {
                    rabbleRouser.IsCharged = true;
                    _game.CheckNextCheckpoint();
                    await ReplyAsync("OK");
                }
                catch (Exception ex)
                {
                    _game.MessageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                }
            }
            await Task.CompletedTask;
        }

        [Command("destroy"), Summary("Взорвать локацию."), Alias("подорвать", "kaboom"), RequireContext(ContextType.DM), RequirePlayer(typeof(Demoman)), RequireGameState(GameState.Night)]
        public async Task Kaboom([Summary("номер локации")] int place, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                var demoman = (currentPlayer.Role as Demoman);
                var placeToDestroy = _game.GetPlaceInfo(place);
                if (placeToDestroy != null && demoman.PlaceToDestroy == null)
                {
                    try
                    {
                        demoman.PlaceToDestroy = placeToDestroy;
                        await ReplyAsync(_game.MessageBuilder.GetText("OK"));
                        _game.CheckNextCheckpoint();
                    }
                    catch (Exception ex)
                    {
                        _game.MessageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                    }
                }
            }
            await Task.CompletedTask;
        }

        [Command("go"), Summary("Посетить локацию."), Alias("пойти"), RequireContext(ContextType.DM), RequirePlayer, RequireGameState(GameState.Night)]
        public async Task WhereToGo([Summary("номер локации")] int place, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (currentPlayer.Role.Team != Team.Mafia)
                {
                    var placeToGo = _game.GetPlaceInfo(place);
                    if (placeToGo != null)
                    {
                        try
                        {
                            currentPlayer.PlaceToGo = placeToGo;
                            await ReplyAsync(_game.MessageBuilder.GetText("OK"));
                            _game.CheckNextCheckpoint();
                        }
                        catch (Exception ex)
                        {
                            _game.MessageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                        }
                    }
                }
            }
            await Task.CompletedTask;
        }

        [Command("forcestart"), Summary("Форсирует начало игры."), Alias("го", "погнали"), RequireContext(ContextType.Guild), RequireAdmin]
        public async Task ForceStartGame([Remainder] string ignored = null)
        {
            _game.StopPlayerCollecting();
            await Task.CompletedTask;
        }

        [Command("stop"), Summary("Форсированно отменяет игру."), Alias("стоп"), RequireContext(ContextType.Guild), RequireAdmin]
        public async Task ForceStopGame([Remainder] string ignored = null)
        {
            _game.StopGame();
            await Task.CompletedTask;
        }

        [Command("gamemode"), Summary("Изменяет режим игры."), Alias("gametype", "режим"), RequireContext(ContextType.Guild), RequireAdmin]
        public async Task SetGameMode([Summary("режим игры"), Remainder] string mode)
        {
            if (_game.CurrentState == GameState.Stopped)
            {
                if (_game.Settings.IsValidGametype(mode))
                {
                    _game.LoadSettings(mode);
                    await ReplyAsync(_game.MessageBuilder.GetTextSimple("GamemodeChangeSuccess"));
                }
                else
                {
                    await ReplyAsync(_game.MessageBuilder.GetTextSimple("GamemodeChangeFail"));
                }
            }
            else
            {
                await ReplyAsync(_game.MessageBuilder.GetTextSimple("GamemodeChangeFailInProgress"));
            }
            await Task.CompletedTask;
        }

        [Command("recalculate"), Summary("Пересчет статистики игроков."), Alias("пересчет"), RequireAdmin]
        public async Task RecalculateStats([Remainder] string ignored = null)
        {
            Stat.RecalculateAll();
            await ReplyAsync("Stats recalculated.");
        }
    }
}