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

        [Command("start"), Summary("Запускает игру."), Alias("старт"), RequireContext(ContextType.Guild), RequireGameState(GameState.Stopped)]
        public async Task Start([Remainder] string ignored = null)
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

        [Command("join"), Summary("Присоединяет игрока к игре."), RequireGameState(GameState.PlayerCollecting),
         Alias("я", "z")]
        public async Task Register([Remainder] string ignored = null)
        {
            var user = new UserWrapper(Context.User);
            if (!_game.CurrentPlayers.ContainsKey(Context.User.Id))
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

        [Command("cancel"), Summary("Отменяет действие."), Alias("отмена", "нея", "don't")]
        public async Task Cancel([Remainder] string ignored = null)
        {
            if (_game.CurrentState == GameState.PlayerCollecting && _game.CurrentPlayers.ContainsKey(Context.User.Id))
            {
                var playerInfo = _game.CurrentPlayers[Context.User.Id];
                _game.CurrentPlayers.Remove(Context.User.Id);
                _game.PlayersList.Remove(playerInfo);
                _game.MessageBuilder.Text(String.Format("{0} вышел из игры! ({1}) ", playerInfo.GetName(), _game.CurrentPlayers.Count)).SendPublic(_game.GameChannel);
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
                        await ReplyAsync("Ваш голос отменен");
                    }
                    else if (_game.CurrentState == GameState.Day)
                    {
                        if (currentPlayer.CancelVote())
                        {
                            _game.MessageBuilder.Text(currentPlayer.GetName() + " отменил свой голос").SendPublic(_game.GameChannel);
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
                        await ReplyAsync("Вы пропустили ход.");
                        _game.CheckNextCheckpoint();
                    }
                    else
                    {
                        await ReplyAsync("Вы уже пропустили ход.");
                    }
                }
                else
                {
                    if (currentPlayer.SkipTurn())
                    {
                        _game.MessageBuilder.Text(currentPlayer.GetName() + " пропустил ход").SendPublic(_game.GameChannel);
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

        [Command("imprison"), Summary("Осудить указанного игрока."), RequirePlayer, RequireGameState(GameState.Day),
         Alias("посадить", "повесить", "gjcflbnm", "gjdtcbnm")]
        public async Task Vote([Summary("номер игрока")] int player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (Context.Channel is IDMChannel)
                {
                    if (currentPlayer.Role is Elder)
                    {
                        var elder = (currentPlayer.Role as Elder);
                        var playerToKill = _game.GetPlayerInfo(player);
                        if (playerToKill != null && elder.PlayerToKill == null)
                        {
                            try
                            {
                                elder.PlayerToKill = playerToKill;
                                await ReplyAsync("Голос принят.");
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
                else
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

        [Command("buy"), Summary("Выводит список предметов."), Alias("купить"), RequireContext(ContextType.DM), RequirePlayer]
        public async Task BuyItem()
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (BaseItem.AvailableItems.Length > 0)
                {
                    var response = "Предметы, доступные для покупки: " + Environment.NewLine;
                    for (var i = 0; i < BaseItem.AvailableItems.Length; i++)
                    {
                        var item = BaseItem.AvailableItems[i];
                        var itemInPlayer = currentPlayer.GetItem(item);
                        response += String.Format("{0}. <b>{1}</b> - предмет ", i + 1, item.Name);
                        if (itemInPlayer != null)
                        {
                            if (itemInPlayer.IsActive)
                            {
                                response += "будет использован этой ночью";
                            }
                            else
                            {
                                response += "уже использован";
                            }
                        }
                        else
                        {
                            response += "доступен для покупки";
                        }
                        response += ". Цена: " + item.Cost + Environment.NewLine;
                        response += "<i>" + item.Description + "</i>";
                        response += Environment.NewLine;
                        response += Environment.NewLine;
                    }
                    _game.MessageBuilder.Text(response, false).SendPrivate(currentPlayer);
                }
                else
                {
                    _game.MessageBuilder.PrepareText("ShopDisabled").SendPrivate(currentPlayer);
                }
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
                        await ReplyAsync(MessageBuilder.Encode("Вы купили " + itemToBuy.Name));
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
        public async Task Kill([Summary("номер игрока")] int player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (currentPlayer.Role is Highlander)
                {
                    var highlander = (currentPlayer.Role as Highlander);
                    var playerToKill = _game.GetPlayerInfo(player);
                    if (playerToKill != null && highlander.PlayerToKill == null)
                    {
                        try
                        {
                            highlander.PlayerToKill = playerToKill;
                            _game.NightAction(currentPlayer.Role);
                            await ReplyAsync("Голос принят.");
                            _game.CheckNextCheckpoint();
                        }
                        catch (Exception ex)
                        {
                            _game.MessageBuilder.Text(ex.Message, false).SendPrivate(currentPlayer);
                        }
                    }
                    return;
                }
                if (currentPlayer.Role is Sheriff)
                {
                    var sheriff = (currentPlayer.Role as Sheriff);
                    var playerToKill = _game.GetPlayerInfo(player);
                    if (playerToKill != null && sheriff.PlayerToKill == null)
                    {
                        sheriff.PlayerToKill = playerToKill;
                        _game.NightAction(currentPlayer.Role);
                        await ReplyAsync("Голос принят.");
                        _game.CheckNextCheckpoint();
                    }
                    return;
                }
                if (currentPlayer.Role is Killer)
                {
                    var killer = (currentPlayer.Role as Killer);
                    var playerToKill = _game.GetPlayerInfo(player);
                    if (playerToKill != null && killer.PlayerToKill == null)
                    {
                        killer.PlayerToKill = playerToKill;
                        _game.NightAction(currentPlayer.Role);
                        var response = String.Format("Киллер {0} выбрал в качестве жертвы {1}!", currentPlayer.GetName(), playerToKill.GetName());
                        _game.MessageBuilder.Text(response).SendToTeam(Team.Mafia);
                        _game.CheckNextCheckpoint();
                    }
                    return;
                }
                if (currentPlayer.Role is NeutralKiller)
                {
                    var maniac = (currentPlayer.Role as NeutralKiller);
                    var playerToKill = _game.GetPlayerInfo(player);
                    if (playerToKill != null && maniac.PlayerToKill == null)
                    {
                        maniac.PlayerToKill = playerToKill;
                        _game.NightAction(currentPlayer.Role);
                        await ReplyAsync("Голос принят.");
                        _game.CheckNextCheckpoint();
                    }
                    return;
                }
                _game.NightVote(currentPlayer, player);
                _game.CheckNextCheckpoint();
            }
            await Task.CompletedTask;
        }

        [Command("curse"), Summary("Проклясть игрока."), Alias("проклясть"), RequireContext(ContextType.DM), RequirePlayer(typeof(Warlock)), RequireGameState(GameState.Night)]
        public async Task CursePlayer([Summary("номер игрока")] int player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                var warlock = (currentPlayer.Role as Warlock);
                var playerToCurse = _game.GetPlayerInfo(player);
                if (playerToCurse != null && warlock.PlayerToCurse == null)
                {
                    try
                    {
                        warlock.PlayerToCurse = playerToCurse;
                        _game.NightAction(currentPlayer.Role);
                        await ReplyAsync("Голос принят.");
                        _game.CheckNextCheckpoint();
                    }
                    catch (Exception ex)
                    {
                        _game.MessageBuilder.Text(ex.Message, false).SendPrivate(currentPlayer);
                    }
                }
            }
            await Task.CompletedTask;
        }

        [Command("check"), Summary("Проверить игрока."), Alias("пров", "проверить"), RequireContext(ContextType.DM), RequirePlayer(typeof(Commissioner), typeof(Homeless), typeof(Lawyer)), RequireGameState(GameState.Night)]
        public async Task CheckPlayer([Summary("номер игрока")] int player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                if (currentPlayer.Role is Commissioner)
                {
                    var commissioner = (currentPlayer.Role as Commissioner);
                    var playerToCheck = _game.GetPlayerInfo(player);
                    if (playerToCheck != null && commissioner.PlayerToCheck == null)
                    {
                        commissioner.PlayerToCheck = playerToCheck;
                        _game.NightAction(currentPlayer.Role);
                        await ReplyAsync("Голос принят.");
                        _game.CheckNextCheckpoint();
                    }
                }
                else if (currentPlayer.Role is Homeless)
                {
                    var homeless = (currentPlayer.Role as Homeless);
                    var playerToCheck = _game.GetPlayerInfo(player);
                    if (playerToCheck != null && homeless.PlayerToCheck == null)
                    {
                        homeless.PlayerToCheck = playerToCheck;
                        _game.NightAction(currentPlayer.Role);
                        await ReplyAsync("Голос принят.");
                        _game.CheckNextCheckpoint();
                    }
                }
                else if (currentPlayer.Role is Lawyer)
                {
                    var lawyer = (currentPlayer.Role as Lawyer);
                    var playerToCheck = _game.GetPlayerInfo(player);
                    if (playerToCheck != null && lawyer.PlayerToCheck == null)
                    {
                        lawyer.PlayerToCheck = playerToCheck;
                        _game.NightAction(currentPlayer.Role);
                        var response = String.Format("Адвокат {0} выбрал {1} для проверки!", currentPlayer.GetName(), lawyer.PlayerToCheck.GetName());
                        _game.MessageBuilder.Text(response).SendToTeam(Team.Mafia);
                        _game.CheckNextCheckpoint();
                    }
                }
            }
            await Task.CompletedTask;
        }

        [Command("sleep"), Summary("Переспать с игроком."), Alias("спать"), RequireContext(ContextType.DM), RequirePlayer(typeof(Wench)), RequireGameState(GameState.Night)]
        public async Task SleepWithPlayer([Summary("номер игрока")] int player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                var wench = (currentPlayer.Role as Wench);
                var playerToCheck = _game.GetPlayerInfo(player);
                if (playerToCheck != null && wench.PlayerToCheck == null)
                {
                    try
                    {
                        wench.PlayerToCheck = playerToCheck;
                        _game.NightAction(currentPlayer.Role);
                        await ReplyAsync("Голос принят.");
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

        [Command("block"), Summary("Блокировать игрока."), Alias("блок"), RequireContext(ContextType.DM), RequirePlayer(typeof(Hoodlum)), RequireGameState(GameState.Night)]
        public async Task BlockPlayer([Summary("номер игрока")] int player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                var hoodlum = (currentPlayer.Role as Hoodlum);
                var playerToBlock = _game.GetPlayerInfo(player);
                if (playerToBlock != null && hoodlum.PlayerToBlock == null)
                {
                    try
                    {
                        hoodlum.PlayerToBlock = playerToBlock;
                        _game.NightAction(currentPlayer.Role);
                        var response = String.Format("Громила {0} выбрал {1} для блокировки!", currentPlayer.GetName(), hoodlum.PlayerToBlock.GetName());
                        _game.MessageBuilder.Text(response).SendToTeam(Team.Yakuza);
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

        [Command("heal"), Summary("Подлатать игрока."), Alias("лечить"), RequireContext(ContextType.DM), RequirePlayer(typeof(Doctor)), RequireGameState(GameState.Night)]
        public async Task HealPlayer([Summary("номер игрока")] int player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                var doctor = (currentPlayer.Role as Doctor);
                var playerToHeal = _game.GetPlayerInfo(player);
                if (playerToHeal != null && doctor.PlayerToHeal == null)
                {
                    try
                    {
                        doctor.PlayerToHeal = playerToHeal;
                        _game.NightAction(currentPlayer.Role);
                        await ReplyAsync("Голос принят.");
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

        [Command("justify"), Summary("Оправдать игрока."), Alias("оправдать"), RequireContext(ContextType.DM), RequirePlayer(typeof(Judge)), RequireGameState(GameState.Day)]
        public async Task JustifyPlayer([Summary("номер игрока")] int player, [Remainder] string ignored = null)
        {
            if (_game.CurrentPlayers.TryGetValue(Context.User.Id, out InGamePlayerInfo currentPlayer))
            {
                var judge = (currentPlayer.Role as Judge);
                var playerToJustify = _game.GetPlayerInfo(player);
                if (playerToJustify != null && judge.PlayerToJustufy == null)
                {
                    try
                    {
                        judge.PlayerToJustufy = playerToJustify;
                        await ReplyAsync("Голос принят.");
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
                        await ReplyAsync(MessageBuilder.Encode("Сегодня взорвем " + placeToDestroy.Name));
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
                            await ReplyAsync(MessageBuilder.Encode("Сегодня пойдем в " + placeToGo.Name));
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

        [Command("go"), Summary("Форсирует начало игры."), Alias("го", "погнали"), RequireContext(ContextType.Guild), RequireAdmin]
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
                    await ReplyAsync(MessageBuilder.Encode($"Режим игры успешно изменен на {_game.Settings.GameType}."));
                }
                else
                {
                    await ReplyAsync("Неизвестный режим игры.");
                }
            }
            else
            {
                await ReplyAsync("Менять режим игры нельзя, пока игра не завершена.");
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