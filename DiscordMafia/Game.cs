using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordMafia.Client;
using DiscordMafia.Items;
using DiscordMafia.Roles;
using DiscordMafia.Roles.Places;
using DiscordMafia.Voting;
using DiscordMafia.DB;
using DiscordMafia.Lib;

namespace DiscordMafia
{
    public class Game
    {
        protected System.Threading.SynchronizationContext syncContext;
        protected Random randomGenerator = new Random();
        protected Timer timer;
        private DiscordSocketClient client;
        public Config.GameSettings settings { get; protected set; }

        public GameState currentState { get; protected set; }
        public Dictionary<ulong, InGamePlayerInfo> currentPlayers { get; protected set; }
        public List<InGamePlayerInfo> playersList { get; protected set; }
        public ulong gameChannel { get; protected set; }
        protected RoleAssigner roleAssigner { get; private set; }
        protected Vote currentDayVote { get; set; }
        protected BooleanVote currentEveningVote { get; set; }
        protected IMessage currentDayVoteMessage { get; set; }
        protected Vote currentMafiaVote { get; set; }
        protected Vote currentYakuzaVote { get; set; }
        public Config.MessageBuilder messageBuilder { get; set; }
        protected KillManager killManager { get; set; }
        public Achievement.AchievementManager achievementManager { get; private set; }
        public Achievement.AchievementAssigner achievementAssigner { get; private set; }
        protected int PlayerCollectingRemainingTime = 0;

        public Game(System.Threading.SynchronizationContext syncContext, DiscordSocketClient client, Config.MainSettings mainSettings)
        {
            gameChannel = mainSettings.GameChannel;
            achievementManager = new Achievement.AchievementManager(this);
            achievementAssigner = new Achievement.AchievementAssigner(this);

            this.syncContext = syncContext;
            this.client = client;

            roleAssigner = new RoleAssigner();
            timer = new Timer();
            timer.Elapsed += OnTimer;
            currentState = GameState.Stopped;
            currentPlayers = new Dictionary<ulong, InGamePlayerInfo>();
            playersList = new List<InGamePlayerInfo>();
            LoadSettings();
            killManager = new KillManager(this);
        }

        private void LoadSettings(string gametype = null)
        {
            settings = new Config.GameSettings(gametype);
            messageBuilder = new Config.MessageBuilder(settings, client, playersList);
            Console.WriteLine("Settings loaded");
        }

        public void OnPublicMessage(SocketMessage message)
        {
            string text = message.Content;
            var channel = message.Channel;
            UserWrapper user = message.Author;
            if (channel.Id != gameChannel)
            {
                return;
            }
            var currentPlayer = GetPlayerInfo(user.Id);
            if (text.StartsWith("/"))
            {
                var parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0].ToLower())
                {
                    case "/help":
                    case "/хелп":
                        Help(user);
                        return;
                    case "/top":
                    case "/топ":
                        var topCount = 20;
                        if (parts.Length > 1)
                        {
                            int.TryParse(parts[1], out topCount);
                        }
                        messageBuilder.Text(Stat.GetTopAsString(messageBuilder, topCount), false).SendPublic(channel);
                        return;
                    case "/start":
                    case "/старт":
                        StartGame();
                        return;
                    case "/погнали":
                        StopPlayerCollecting();
                        return;
                    case "/stop":
                    case "/стоп":
                        if (user.IsAdmin())
                        {
                            StopGame();
                        }
                        return;
                    case "/я":
                    case "/z":
                    case "/join":
                        RegisterPlayer(user, fromPrivate: false);
                        return;
                    case "/отмена":
                    case "/cancel":
                        if (currentState == GameState.PlayerCollecting)
                        {
                            UnRegisterPlayer(user);
                            return;
                        }
                        if (currentPlayer != null && currentState == GameState.Day)
                        {
                            if (currentPlayer.CancelVote())
                            {
                                messageBuilder.Text(currentPlayer.GetName() + " отменил свой голос").SendPublic(gameChannel);
                            }
                        }
                        return;
                    case "/пропустить":
                    case "/skip":
                        if (currentPlayer != null)
                        {
                            if (currentPlayer.SkipTurn())
                            {
                                messageBuilder.Text(currentPlayer.GetName() + " пропустил ход").SendPublic(gameChannel);
                                CheckNextCheckpoint();
                            }
                        }
                        return;
                    case "/посадить":
                    case "/imprison":
                        if (parts.Length > 1)
                        {
                            DayVote(currentPlayer, parts[1]);
                            CheckNextCheckpoint();
                        }
                        return;
                    case "/да":
                    case "/yes":
                        if (currentState == GameState.Evening)
                        {
                            EveningVote(currentPlayer, true);
                            CheckNextCheckpoint();
                        }
                        return;
                    case "/нет":
                    case "/no":
                        if (currentState == GameState.Evening)
                        {
                            EveningVote(currentPlayer, false);
                            CheckNextCheckpoint();
                        }
                        return;
                    case "/gametype":
                    case "/gamemode":
                    case "/режим":
                        if (user.IsAdmin() && parts.Length > 1)
                        {
                            if (currentState == GameState.Stopped)
                            {
                                if (settings.IsValidGametype(parts[1]))
                                {
                                    LoadSettings(parts[1]);
                                    messageBuilder.Text("Режим игры успешно изменен.").SendPublic(gameChannel);
                                }
                                else
                                {
                                    messageBuilder.Text("Неизвестный режим игры.").SendPublic(gameChannel);
                                }
                            }
                            else
                            {
                                messageBuilder.Text("Менять режим игры нельзя, пока игра не завершена.").SendPublic(gameChannel);
                            }
                        }
                        else
                        {
                            messageBuilder.Text($"Текущий режим игры: {settings.GameType}").SendPublic(gameChannel);
                        }
                        return;
                }
            }
        }

        public void OnPrivateMessage(SocketMessage message)
        {
            string text = message.Content;
            UserWrapper user = message.Author;
            var currentPlayer = GetPlayerInfo(user.Id);
            if (text.StartsWith("/"))
            {
                var parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0].ToLower())
                {
                    case "/хелп":
                    case "/help":
                        Help(user);
                        return;
                    case "/top":
                    case "/топ":
                        var topCount = 20;
                        if (parts.Length > 1)
                        {
                            int.TryParse(parts[1], out topCount);
                        }
                        messageBuilder.Text(Stat.GetTopAsString(messageBuilder, topCount), false).SendPrivate(user);
                        return;
                    case "/mystat":
                    case "/мойстат":
                        messageBuilder.Text(Stat.GetStatAsString(user)).SendPrivate(user);
                        messageBuilder.Text(achievementManager.GetAchievementsAsString(user), false).SendPrivate(user);
                        return;
                    case "/recalculate":
                        if (user.IsAdmin())
                        {
                            Stat.RecalculateAll();
                            messageBuilder.Text("OK").SendPrivate(user);
                        }
                        return;
                    case "/я":
                    case "/z":
                    case "/join":
                        RegisterPlayer(user, fromPrivate: true);
                        return;
                    case "/купить":
                    case "/buy":
                        if (currentPlayer == null)
                        {
                            return;
                        }
                        if (parts.Length > 1)
                        {
                            var itemToBuy = GetItemInfo(parts[1]);
                            if (itemToBuy != null)
                            {
                                try
                                {
                                    currentPlayer.Buy(itemToBuy);
                                    messageBuilder.Text("Вы купили " + itemToBuy.Name).SendPrivate(currentPlayer);
                                }
                                catch (Exception ex)
                                {
                                    messageBuilder.Text(ex.Message, false).SendPrivate(currentPlayer);
                                }
                            }
                            return;
                        }
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
                            messageBuilder.Text(response, false).SendPrivate(currentPlayer);
                        }
                        else
                        {
                            messageBuilder.PrepareText("ShopDisabled").SendPrivate(currentPlayer);
                        }
                        return;
                    case "/отмена":
                    case "/cancel":
                        if (currentPlayer != null)
                        {
                            currentPlayer.CancelActivity();
                            messageBuilder.Text("Ваш голос отменен").SendPrivate(currentPlayer);
                        }
                        return;
                    case "/пропустить":
                    case "/skip":
                        if (currentPlayer != null)
                        {
                            if (currentPlayer.SkipTurn())
                            {
                                messageBuilder.Text("Вы пропустили ход.").SendPrivate(currentPlayer);
                                CheckNextCheckpoint();
                            }
                        }
                        return;
                    case "/убить":
                    case "/kill":
                        if (currentPlayer == null)
                        {
                            return;
                        }
                        if (currentPlayer.Role is Highlander && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var highlander = (currentPlayer.Role as Highlander);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && highlander.PlayerToKill == null)
                            {
                                try
                                {
                                    highlander.PlayerToKill = playerToKill;
                                    NightAction(currentPlayer.Role);
                                    messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                    CheckNextCheckpoint();
                                }
                                catch (Exception ex)
                                {
                                    messageBuilder.Text(ex.Message, false).SendPrivate(currentPlayer);
                                }
                            }
                            return;
                        }
                        if (currentPlayer.Role is Sheriff && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var sheriff = (currentPlayer.Role as Sheriff);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && sheriff.PlayerToKill == null)
                            {
                                sheriff.PlayerToKill = playerToKill;
                                NightAction(currentPlayer.Role);
                                messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                CheckNextCheckpoint();
                            }
                            return;
                        }
                        if (currentPlayer.Role is Killer && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var killer = (currentPlayer.Role as Killer);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && killer.PlayerToKill == null)
                            {
                                killer.PlayerToKill = playerToKill;
                                NightAction(currentPlayer.Role);
                                var response = String.Format("Киллер {0} выбрал в качестве жертвы {1}!", currentPlayer.GetName(), playerToKill.GetName());
                                messageBuilder.Text(response).SendToTeam(Team.Mafia);
                                CheckNextCheckpoint();
                            }
                            return;
                        }
                        if (currentPlayer.Role is NeutralKiller && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var maniac = (currentPlayer.Role as NeutralKiller);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && maniac.PlayerToKill == null)
                            {
                                maniac.PlayerToKill = playerToKill;
                                NightAction(currentPlayer.Role);
                                messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                CheckNextCheckpoint();
                            }
                            return;
                        }
                        if (parts.Length > 1)
                        {
                            NightVote(currentPlayer, parts[1]);
                            CheckNextCheckpoint();
                        }
                        return;
                    case "/проклясть":
                    case "/curse":
                        if (currentPlayer == null)
                        {
                            return;
                        }
                        if (currentPlayer.Role is Warlock && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var warlock = (currentPlayer.Role as Warlock);
                            var playerToCurse = GetPlayerInfo(parts[1]);
                            if (playerToCurse != null && warlock.PlayerToCurse == null)
                            {
                                try
                                {
                                    warlock.PlayerToCurse = playerToCurse;
                                    NightAction(currentPlayer.Role);
                                    messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                    CheckNextCheckpoint();
                                }
                                catch (Exception ex)
                                {
                                    messageBuilder.Text(ex.Message, false).SendPrivate(currentPlayer);
                                }
                            }
                        }
                        return;
                    case "/посадить":
                    case "/imprison":
                        if (currentPlayer == null)
                        {
                            return;
                        }
                        if (currentPlayer.Role is Elder && parts.Length > 1 && currentState == GameState.Day)
                        {
                            var elder = (currentPlayer.Role as Elder);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && elder.PlayerToKill == null)
                            {
                                try
                                {
                                    elder.PlayerToKill = playerToKill;
                                    messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                    CheckNextCheckpoint();
                                }
                                catch (Exception ex)
                                {
                                    messageBuilder.Text(ex.Message, false).SendPrivate(currentPlayer);
                                }
                            }
                            return;
                        }
                        return;
                    case "/пров":
                    case "/check":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.Role is Commissioner && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var commissioner = (currentPlayer.Role as Commissioner);
                                var playerToCheck = GetPlayerInfo(parts[1]);
                                if (playerToCheck != null && commissioner.PlayerToCheck == null)
                                {
                                    commissioner.PlayerToCheck = playerToCheck;
                                    NightAction(currentPlayer.Role);
                                    messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                    CheckNextCheckpoint();
                                }
                            }
                            else if (currentPlayer.Role is Homeless && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var homeless = (currentPlayer.Role as Homeless);
                                var playerToCheck = GetPlayerInfo(parts[1]);
                                if (playerToCheck != null && homeless.PlayerToCheck == null)
                                {
                                    homeless.PlayerToCheck = playerToCheck;
                                    NightAction(currentPlayer.Role);
                                    messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                    CheckNextCheckpoint();
                                }
                            }
                            else if (currentPlayer.Role is Lawyer && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var lawyer = (currentPlayer.Role as Lawyer);
                                var playerToCheck = GetPlayerInfo(parts[1]);
                                if (playerToCheck != null && lawyer.PlayerToCheck == null)
                                {
                                    lawyer.PlayerToCheck = playerToCheck;
                                    NightAction(currentPlayer.Role);
                                    var response = String.Format("Адвокат {0} выбрал {1} для проверки!", currentPlayer.GetName(), lawyer.PlayerToCheck.GetName());
                                    messageBuilder.Text(response).SendToTeam(Team.Mafia);
                                    CheckNextCheckpoint();
                                }
                            }
                        }
                        return;
                    case "/спать":
                    case "/sleep":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.Role is Wench && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var wench = (currentPlayer.Role as Wench);
                                var playerToCheck = GetPlayerInfo(parts[1]);
                                if (playerToCheck != null && wench.PlayerToCheck == null)
                                {
                                    try
                                    {
                                        wench.PlayerToCheck = playerToCheck;
                                        NightAction(currentPlayer.Role);
                                        messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                        CheckNextCheckpoint();
                                    }
                                    catch (Exception ex)
                                    {
                                        messageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                                    }
                                }
                            }
                        }
                        return;
                    case "/блок":
                    case "/block":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.Role is Hoodlum && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var hoodlum = (currentPlayer.Role as Hoodlum);
                                var playerToBlock = GetPlayerInfo(parts[1]);
                                if (playerToBlock != null && hoodlum.PlayerToBlock == null)
                                {
                                    try
                                    {
                                        hoodlum.PlayerToBlock = playerToBlock;
                                        NightAction(currentPlayer.Role);
                                        var response = String.Format("Громила {0} выбрал {1} для блокировки!", currentPlayer.GetName(), hoodlum.PlayerToBlock.GetName());
                                        messageBuilder.Text(response).SendToTeam(Team.Yakuza);
                                        CheckNextCheckpoint();
                                    }
                                    catch (Exception ex)
                                    {
                                        messageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                                    }
                                }
                            }
                        }
                        return;
                    case "/лечить":
                    case "/heal":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.Role is Doctor && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var doctor = (currentPlayer.Role as Doctor);
                                var playerToHeal = GetPlayerInfo(parts[1]);
                                if (playerToHeal != null && doctor.PlayerToHeal == null)
                                {
                                    try
                                    {
                                        doctor.PlayerToHeal = playerToHeal;
                                        NightAction(currentPlayer.Role);
                                        messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                        CheckNextCheckpoint();
                                    }
                                    catch (Exception ex)
                                    {
                                        messageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                                    }
                                }
                            }
                        }
                        return;
                    case "/оправдать":
                    case "/justify":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.Role is Judge && parts.Length > 1 && currentState == GameState.Day)
                            {
                                var judge = (currentPlayer.Role as Judge);
                                var playerToJustify = GetPlayerInfo(parts[1]);
                                if (playerToJustify != null && judge.PlayerToJustufy == null)
                                {
                                    try
                                    {
                                        judge.PlayerToJustufy = playerToJustify;
                                        messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                        CheckNextCheckpoint();
                                    }
                                    catch (Exception ex)
                                    {
                                        messageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                                    }
                                }
                            }
                        }
                        return;
                    case "/подорвать":
                    case "/destroy":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.Role is Demoman && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var demoman = (currentPlayer.Role as Demoman);
                                var placeToDestroy = GetPlaceInfo(parts[1]);
                                if (placeToDestroy != null && demoman.PlaceToDestroy == null)
                                {
                                    try
                                    {
                                        demoman.PlaceToDestroy = placeToDestroy;
                                        messageBuilder.Text("Сегодня взорвем " + placeToDestroy.Name).SendPrivate(currentPlayer);
                                        CheckNextCheckpoint();
                                    }
                                    catch (Exception ex)
                                    {
                                        messageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                                    }
                                }
                            }
                        }
                        return;
                    case "/пойти":
                    case "/go":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.Role.Team != Team.Mafia && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var placeToGo = GetPlaceInfo(parts[1]);
                                if (placeToGo != null)
                                {
                                    try
                                    {
                                        currentPlayer.PlaceToGo = placeToGo;
                                        messageBuilder.Text("Сегодня пойдем в " + placeToGo.Name).SendPrivate(currentPlayer);
                                        CheckNextCheckpoint();
                                    }
                                    catch (Exception ex)
                                    {
                                        messageBuilder.Text(ex.Message).SendPrivate(currentPlayer);
                                    }
                                }
                            }
                        }
                        return;
                }
            }
            // Передача "командных" сообщений
            if (currentPlayer?.Role != null)
            {
                if (currentPlayer.Role.Team == Team.Mafia || currentPlayer.Role.Team == Team.Yakuza)
                {
                    foreach (var player in playersList)
                    {
                        if (player.Role.Team == currentPlayer.Role.Team && !player.IsBot && player.User.Id != message.Author.Id)
                        {
                            messageBuilder.Text($"{currentPlayer.GetName()}: {text}").SendPrivate(player);
                        }
                    }
                }
            }
        }

        private void Help(UserWrapper user)
        {
            var currentPlayer = currentPlayers.ContainsKey(user.Id) ? currentPlayers[user.Id] : null;
            var message = "<b>==========Игровые команды==========</b>" + Environment.NewLine;
            message += "/help - вывод этой справки (в приват боту);" + Environment.NewLine;
            message += "/join, /я - регистрация в игре (во время набора игроков);" + Environment.NewLine;
            message += "/cancel, /отмена - выход из игры (во время набора игроков);" + Environment.NewLine;
            message += "/mystat, /мойстат - ваша статистика(в приват боту);" + Environment.NewLine;
            message += "/top, /топ - лучшие игроки;" + Environment.NewLine;
            message += "/buy, /купить - посмотреть доступные вещи для покупки(только во время игры, в приват боту);" + Environment.NewLine;
            //message += "/announceon, /предупреждай - сообщать о начале игры(в приват боту);" + Environment.NewLine;
            //message += "/announceoff, /отстань - больше не сообщать о начале игры(в приват боту);" + Environment.NewLine;

            if (currentPlayer != null && currentPlayer.IsAlive && currentPlayer.Role != null)
            {
                message += " " + Environment.NewLine;
                message += "<b>=========== Помощь по статусу===========</b>" + Environment.NewLine;
                message += "Ваш статус - " + messageBuilder.FormatRole(currentPlayer.Role.Name) + Environment.NewLine;
                switch (currentPlayer.Role.Team)
                {
                    case Team.Civil:
                        message += "Вы играете за команду мирных жителей" + Environment.NewLine;
                        break;
                    case Team.Neutral:
                        message += "Вы играете сами за себя" + Environment.NewLine;
                        break;
                    case Team.Mafia:
                        message += "Вы играете за команду мафов" + Environment.NewLine;
                        break;
                    case Team.Yakuza:
                        message += "Вы играете за команду якудз" + Environment.NewLine;
                        break;
                }

                message += messageBuilder.GetText(string.Format("RoleHelp_{0}", currentPlayer.Role.GetType().Name)) + Environment.NewLine;
            }

            message += " " + Environment.NewLine;
            message += "<b>========Помощь по режиму игры========</b>" + Environment.NewLine;
            message += $"Текущий режим игры: {settings.GameType}" + Environment.NewLine;
            message += $"Якудза: {settings.IsYakuzaEnabled}" + Environment.NewLine;
            message += $"Мафов из каждой группировки: {settings.MafPercent}%" + Environment.NewLine;
            message += "<u><b>Доступные роли</b></u>" + Environment.NewLine;
            message += settings.Roles.RolesHelp();

            message += " " + Environment.NewLine;
            message += "<b>======Помощь по начислению очков======</b>" + Environment.NewLine;
            foreach (var pointConfig in settings.Points.Values)
            {
                message += $"{pointConfig.Description}: {pointConfig.Points}" + Environment.NewLine;
            }

            messageBuilder.Text(message, false).SendPrivate(user);
        }

        private void NightAction(BaseRole role)
        {
            if (settings.ShowNightActions)
            {
                messageBuilder.PrepareText("NightAction_" + role.GetType().Name).SendPublic(gameChannel);
            }
        }

        private void RegisterPlayer(UserWrapper player, bool isBot = false, bool fromPrivate = false)
        {
            if (currentState == GameState.PlayerCollecting && !currentPlayers.ContainsKey(player.Id))
            {
                var playerInfo = new InGamePlayerInfo(player, this);
                playerInfo.DbUser.Save();
                playerInfo.IsBot = isBot;
                currentPlayers.Add(player.Id, playerInfo);
                playersList.Add(playerInfo);
                messageBuilder.Text(String.Format("{0} присоединился к игре! ({1}) ", playerInfo.GetName(), currentPlayers.Count)).SendPublic(gameChannel);
            }
        }

        private void UnRegisterPlayer(UserWrapper player)
        {
            if (currentState == GameState.PlayerCollecting && currentPlayers.ContainsKey(player.Id))
            {
                var playerInfo = currentPlayers[player.Id];
                currentPlayers.Remove(player.Id);
                playersList.Remove(playerInfo);
                messageBuilder.Text(String.Format("{0} вышел из игры! ({1}) ", playerInfo.GetName(), currentPlayers.Count)).SendPublic(gameChannel);
            }
        }

        private void DayVote(InGamePlayerInfo player, string voteForRequest)
        {
            if (player == null)
            {
                return;
            }
            if (currentState == GameState.Day)
            {
                var voteFor = GetPlayerInfo(voteForRequest);
                if (voteFor != null)
                {
                    try
                    {
                        currentDayVote.Add(player, voteFor);
                        var voteCount = currentDayVote.GetResult().VoteCountByPlayer[voteFor.User.Id];
                        messageBuilder.Text(String.Format("{0} голосует за {1} ({2})!", player.GetName(), voteFor.GetName(), voteCount)).SendPublic(gameChannel);
                    }
                    catch (ArgumentException)
                    {
                        // Игрок уже голосовал
                    }
                }
            }
        }

        private void EveningVote(InGamePlayerInfo player, bool voteValue)
        {
            if (player == null)
            {
                return;
            }
            if (currentState == GameState.Evening)
            {
                try
                {
                    var result = currentDayVote?.GetResult();
                    if (result != null && result.HasOneLeader)
                    {
                        var leader = currentPlayers[result.Leader.Value];
                        if (leader != player)
                        {
                            currentEveningVote.Add(player, voteValue);
                            if (voteValue)
                            {
                                messageBuilder.Text(String.Format("{0} уверен в виновности {1} ({2})!", player.GetName(), leader.GetName(), currentEveningVote.GetResult().YesCount)).SendPublic(gameChannel);
                            }
                            else
                            {
                                messageBuilder.Text(String.Format("{0} требует оправдания {1} ({2})!", player.GetName(), leader.GetName(), currentEveningVote.GetResult().NoCount)).SendPublic(gameChannel);
                            }
                        }
                        else
                        {
                            messageBuilder.Text(String.Format("{0} - нельзя голосовать за себя!", player.GetName())).SendPublic(gameChannel);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Игрок уже голосовал
                }
            }
        }

        private void NightVote(InGamePlayerInfo player, string voteForRequest)
        {
            if (player == null)
            {
                return;
            }
            if (currentState == GameState.Night)
            {
                var voteFor = GetPlayerInfo(voteForRequest);
                if (voteFor != null)
                {
                    if (player.Role is Mafioso)
                    {
                        try
                        {
                            if (!currentMafiaVote.HasVotes)
                            {
                                NightAction(player.Role);
                            }
                            currentMafiaVote.Add(player, voteFor);
                            var voteCount = currentMafiaVote.GetResult().VoteCountByPlayer[voteFor.User.Id];
                            var message = String.Format("{0} голосует за убийство {1} ({2})!", player.GetName(), voteFor.GetName(), voteCount);
                            messageBuilder.Text(message).SendToTeam(Team.Mafia);
                        }
                        catch (ArgumentException)
                        {
                            // Игрок уже голосовал
                        }
                    }
                    else if (player.Role is Yakuza)
                    {
                        try
                        {
                            if (!currentYakuzaVote.HasVotes)
                            {
                                NightAction(player.Role);
                            }
                            currentYakuzaVote.Add(player, voteFor);
                            var voteCount = currentYakuzaVote.GetResult().VoteCountByPlayer[voteFor.User.Id];
                            var message = String.Format("{0} голосует за убийство {1} ({2})!", player.GetName(), voteFor.GetName(), voteCount);
                            messageBuilder.Text(message).SendToTeam(Team.Yakuza);
                        }
                        catch (ArgumentException)
                        {
                            // Игрок уже голосовал
                        }
                    }
                }
            }
        }

        private void StartGame()
        {
            if (currentState == GameState.Stopped)
            {
                var message = $"Начинаю набор игроков. У вас <b>{settings.PlayerCollectingTime / 1000}</b> секунд.";
                message += Environment.NewLine + "<b>/join</b> (<b>/я</b>) - Присоединиться к игре";
                messageBuilder.Text(message, false).SendPublic(gameChannel);
                currentState = GameState.PlayerCollecting;
                timer.Interval = Math.Min(settings.PlayerCollectingTime, 60000);
                PlayerCollectingRemainingTime = (int)(settings.PlayerCollectingTime - timer.Interval);
                timer.Start();
                client.SetGameAsync("Мафия (ожидание игроков)");
            }
        }

        private void StopGame()
        {
            timer.Stop();

            achievementAssigner.afterGame();
            achievementManager.Apply();

            currentPlayers.Clear();
            playersList.Clear();
            currentState = GameState.Stopped;
            client.SetGameAsync(null);
        }

        private void OnTimer(object sender)
        {
            timer.Stop();
            syncContext.Post(new System.Threading.SendOrPostCallback(
                delegate (object state)
                {
                    switch (currentState)
                    {
                        case GameState.PlayerCollecting:
                            StopPlayerCollecting();
                            break;
                        case GameState.Night:
                            EndNight();
                            break;
                        case GameState.Morning:
                            EndMorning();
                            break;
                        case GameState.Day:
                            EndDay();
                            break;
                        case GameState.Evening:
                            EndEvening();
                            break;
                    }
                }
             ), null);
        }

        private void StopPlayerCollecting()
        {
            if (PlayerCollectingRemainingTime > 1000)
            {
                var message = String.Format("Осталось <b>{0}</b> секунд. Ещё есть шанс поиграть!", PlayerCollectingRemainingTime / 1000);
                message += Environment.NewLine + "<b>/join</b> (<b>/я</b>) - Присоединиться к игре";
                messageBuilder.Text(message, false).SendPublic(gameChannel);

                timer.Interval = Math.Min(settings.PlayerCollectingTime, 60000);
                PlayerCollectingRemainingTime -= (int)timer.Interval;
                timer.Start();
                return;
            }
            if (currentState == GameState.PlayerCollecting && currentPlayers.Count >= settings.MinPlayers)
            {
                messageBuilder.Text(String.Format("Набор игроков окончен. Участвуют: {0}", currentPlayers.Count)).SendPublic(gameChannel);
                roleAssigner.AssignRoles(this.playersList, this.settings);
                // TODO подтверждение ролей

                var mafiaMessage = "Состав мафии" + Environment.NewLine;
                var yakuzaMessage = "Состав японской мафии" + Environment.NewLine;

                foreach (var player in playersList)
                {
                    var roleWelcomeParam = String.Format("GameStart_Role_{0}", player.Role.GetType().Name);
                    var photoName = String.Format("roles/card{0}.png", player.Role.GetType().Name);
                    messageBuilder.PrepareTextReplacePlayer(roleWelcomeParam, player, "GameStart_Role_Default").AddImage(photoName).SendPrivate(player);
                    switch (player.Role.Team)
                    {
                        case Team.Mafia:
                            mafiaMessage += String.Format("{0} - {1} (`{2}`)", messageBuilder.FormatName(player), messageBuilder.FormatRole(player.Role.Name), player.GetName()) + Environment.NewLine;
                            break;
                        case Team.Yakuza:
                            yakuzaMessage += String.Format("{0} - {1} (`{2}`)", messageBuilder.FormatName(player), messageBuilder.FormatRole(player.Role.Name), player.GetName()) + Environment.NewLine;
                            break;
                    }
                }
                mafiaMessage += "Можете обсуждать свои действия в личных сообщениях или через бота.";
                yakuzaMessage += "Можете обсуждать свои действия в личных сообщениях или через бота.";
                Pause();

                // Состав мафий
                messageBuilder.Text(mafiaMessage, false).SendToTeam(Team.Mafia);
                messageBuilder.Text(yakuzaMessage, false).SendToTeam(Team.Yakuza);

                if (settings.StartFromNight)
                {
                    StartNight();
                }
                else
                {
                    StartMorning();
                }
                client.SetGameAsync("Мафия");
            }
            else
            {
                messageBuilder.Text(String.Format("Недостаточно игроков ({0}/{1})", currentPlayers.Count, settings.MinPlayers)).SendPublic(gameChannel);
                StopGame();
            }
        }

        protected void CheckNextCheckpoint()
        {
            if (currentState == GameState.Day || currentState == GameState.Evening || currentState == GameState.Night)
            {
                var isAllReady = true;
                var dayVoteResult = currentDayVote?.GetResult();
                foreach (var player in playersList)
                {
                    if (player.IsAlive)
                    {
                        isAllReady = player.IsReady(currentState);
                        if (player.Role is Demoman && currentState == GameState.Night)
                        {
                            if ((player.Role as Demoman).Counter == 0)
                            {
                                isAllReady = false;
                            }
                        }
                        if (currentState == GameState.Evening && dayVoteResult != null && dayVoteResult.HasOneLeader)
                        {
                            if (currentPlayers[dayVoteResult.Leader.Value] == player)
                            {
                                isAllReady = true;
                            }
                        }
                        if (!isAllReady)
                        {
                            break;
                        }
                    }
                }
                if (isAllReady)
                {
                    timer.Stop();
                    timer.Interval = 1;
                    timer.Start();
                }
            }
        }

        /// <summary>
        /// Возвращает игрока по его номеру в игре
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected InGamePlayerInfo GetPlayerInfo(string request, bool onlyAlive = true)
        {
            int playerNum = 0;
            if (int.TryParse(request, out playerNum) && playerNum > 0 && playerNum <= playersList.Count)
            {
                var player = playersList[playerNum - 1];
                if (!onlyAlive || player.IsAlive)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Возвращает место по его номеру в игре
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected Place GetPlaceInfo(string request)
        {
            int placeNum = 0;
            if (int.TryParse(request, out placeNum) && placeNum >= 0 && placeNum < Place.AvailablePlaces.Length)
            {
                return Place.AvailablePlaces[placeNum];
            }
            return null;
        }

        /// <summary>
        /// Возвращает предмет по его номеру в игре
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected BaseItem GetItemInfo(string request)
        {
            int itemNum = 0;
            if (int.TryParse(request, out itemNum) && itemNum > 0 && itemNum <= BaseItem.AvailableItems.Length)
            {
                return BaseItem.AvailableItems[itemNum - 1].GetType().GetTypeInfo().GetConstructor(Type.EmptyTypes).Invoke(new object[0]) as BaseItem;
            }
            return null;
        }

        /// <summary>
        /// Возвращает игрока по его ID Telegram
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyAlive"></param>
        /// <returns></returns>
        protected InGamePlayerInfo GetPlayerInfo(ulong userId, bool onlyAlive = true)
        {
            if (currentPlayers.ContainsKey(userId))
            {
                var player = currentPlayers[userId];
                if (!onlyAlive || player.IsAlive)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Сообщение со списком живых игроков
        /// </summary>
        /// <param name="echo">Если true, сообщение будет отправлено в игровой канал</param>
        /// <returns>Сообщение</returns>
        public string GetAlivePlayersMesssage(bool showTitle = true, bool echo = true, InGamePlayerInfo sendTo = null, string keyboardCommand = null)
        {
            var message = "";
            var buttons = new List<string[]>();
            if (showTitle)
            {
                message += "Оставшиеся игроки: " + Environment.NewLine;
            }
            int i = 1;
            foreach (var player in this.playersList)
            {
                if (player.IsAlive)
                {
                    message += i + " - " + messageBuilder.FormatName(player) + $" (`{player.GetName()}`)" + Environment.NewLine;
                }
                i++;
            }
            if (echo)
            {
                if (sendTo != null)
                {
                    messageBuilder.Text(message, false).SendPrivate(sendTo);
                }
                else
                {
                    messageBuilder.Text(message, false).SendPublic(gameChannel);
                }
            }
            return message;
        }

        private void StartMorning()
        {
            foreach (var player in playersList)
            {
                player.ClearActivity();
            }
            timer.Interval = settings.MorningTime;
            messageBuilder.PrepareText("StartMorning").SendPublic(gameChannel);
            currentState = GameState.Morning;
            timer.Start();
        }

        private void StartDay()
        {
            GetAlivePlayersMesssage(keyboardCommand: "/посадить");
            messageBuilder.PrepareText("StartDay").SendPublic(gameChannel);

            foreach (var player in playersList)
            {
                if (player.IsAlive)
                {
                    player.Role.DayInfo(this, player);
                }
            }

            timer.Interval = settings.DayTime;
            currentState = GameState.Day;
            currentDayVote = new Vote();
            timer.Start();
        }

        private void StartEvening()
        {
            messageBuilder.PrepareText("StartEvening").SendPublic(gameChannel);

            var dayVoteResult = currentDayVote?.GetResult();
            if (dayVoteResult != null && dayVoteResult.HasOneLeader)
            {
                messageBuilder.PrepareText("EveningVoteInfo").SendPublic(gameChannel);
                timer.Interval = settings.EveningTime;
                foreach (var player in playersList)
                {
                    player.SkipTurn(false);
                }
            }
            else
            {
                timer.Interval = 1;
            }
            currentState = GameState.Evening;
            currentEveningVote = new BooleanVote();
            timer.Start();
        }

        private void StartNight()
        {
            messageBuilder.PrepareText("StartNight").SendPublic(gameChannel);

            foreach (var player in playersList)
            {
                if (player.IsAlive)
                {
                    player.Role.NightInfo(this, player);
                }
            }

            timer.Interval = settings.NightTime;
            currentState = GameState.Night;
            currentMafiaVote = new Vote();
            currentYakuzaVote = new Vote();
            timer.Start();
        }

        private void EndEvening()
        {
            Console.WriteLine("EndEvening");
            var result = currentDayVote?.GetResult();
            var eveningResult = currentEveningVote?.GetResult();

            foreach (var player in PlayerSorter.SortForActivityCheck(playersList, GameState.Day))
            {
                #region Судья
                // Основная логика и начисление очков размазано по "спасению" в других событиях
                if (player.Role is Judge)
                {
                    var role = player.Role as Judge;
                    if (role.PlayerToJustufy != null)
                    {
                        role.LastPlayerToJustufy = role.PlayerToJustufy;
                    }
                }
                #endregion
            }

            if (result != null && !result.IsEmpty)
            {
                if (result.HasOneLeader && (settings.EveningTime == 0 || (eveningResult != null && eveningResult.Result.HasValue && eveningResult.Result.Value)))
                {
                    var leader = currentPlayers[result.Leader.Value];
                    if (leader.JustifiedBy != null)
                    {
                        messageBuilder.PrepareTextReplacePlayer("JudgeJustify", leader).SendPublic(gameChannel);
                        if (leader.Role is Commissioner)
                        {
                            leader.JustifiedBy.Player.AddPoints("JudgeJustifyCom");
                        }
                        switch (leader.Role.Team)
                        {
                            case Team.Civil:
                                leader.JustifiedBy.Player.AddPoints("JudgeJustifyCivil");
                                break;
                            case Team.Mafia:
                            case Team.Yakuza:
                                leader.JustifiedBy.Player.AddPoints("JudgeJustifyMaf");
                                break;
                        }
                        Pause();
                    }
                    else if (leader.Role is Elder)
                    {
                        messageBuilder.PrepareTextReplacePlayer("DayKillElder", leader).SendPublic(gameChannel);
                        var elder = leader.Role as Elder;
                        if (elder.PlayerToKill != null)
                        {
                            if (elder.PlayerToKill.Role is Commissioner)
                            {
                                leader.AddPoints("CivilDayKillCom");
                            }
                            switch (elder.PlayerToKill.Role.Team)
                            {
                                case Team.Civil:
                                    leader.AddPoints("CivilKillCivil");
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    leader.AddPoints("CivilKillMaf");
                                    break;
                            }
                            Pause();
                            killManager.Kill(elder.PlayerToKill);
                            messageBuilder.PrepareTextReplacePlayer("ElderKill", elder.PlayerToKill).SendPublic(gameChannel);
                        }
                    }
                    else
                    {
                        var pointsByTeam = new Dictionary<Team, long>()
            {
                { Team.Civil, 0 },
                { Team.Mafia, 0 },
                { Team.Yakuza, 0 },
                { Team.Neutral, 0 }
            };
                        if (leader.Role is Commissioner)
                        {
                            pointsByTeam[Team.Civil] += settings.Points.GetPoints("CivilDayKillCom");
                            pointsByTeam[Team.Mafia] += settings.Points.GetPoints("MafKillCom");
                            pointsByTeam[Team.Yakuza] += settings.Points.GetPoints("MafKillCom");
                        }
                        switch (leader.Role.Team)
                        {
                            case Team.Civil:
                                pointsByTeam[Team.Civil] += settings.Points.GetPoints("CivilKillCivil");
                                pointsByTeam[Team.Mafia] += settings.Points.GetPoints("MafKill");
                                pointsByTeam[Team.Yakuza] += settings.Points.GetPoints("MafKill");
                                break;
                            case Team.Mafia:
                                pointsByTeam[Team.Civil] += settings.Points.GetPoints("CivilKillMaf");
                                pointsByTeam[Team.Yakuza] += settings.Points.GetPoints("MafKillOpposite");
                                break;
                            case Team.Yakuza:
                                pointsByTeam[Team.Civil] += settings.Points.GetPoints("CivilKillMaf");
                                pointsByTeam[Team.Mafia] += settings.Points.GetPoints("MafKillOpposite");
                                break;
                        }

                        foreach (var player in playersList)
                        {
                            if (result.IsVotedForLeader(player))
                            {
                                player.AddPoints(pointsByTeam[player.Role.Team]);
                            }
                        }
                        messageBuilder.PrepareTextReplacePlayer("DayKill", leader).SendPublic(gameChannel);
                        killManager.Kill(leader);
                    }
                }
                else
                {
                    // Не решили, кого посадить
                    messageBuilder.PrepareText("DayKillNoChoice").SendPublic(gameChannel);
                }
            }
            else
            {
                // Нет активности
                messageBuilder.PrepareText("DayKillNoActive").SendPublic(gameChannel);
            }
            killManager.Apply();
            ClearActivity();
            if (!CheckWinConditions())
            {
                Pause();
                StartNight();
            }
        }

        private void EndDay()
        {
            Console.WriteLine("EndDay");
            currentDayVoteMessage = null;
            Pause();
            StartEvening();
        }

        private void EndMorning()
        {
            Console.WriteLine("EndMorning");
            StartDay();
        }

        private void EndNight()
        {
            Console.WriteLine("EndNight");
            messageBuilder.PrepareText("EndNight").SendPublic(gameChannel);
            Pause(2);

            // Предметы
            foreach (var player in playersList)
            {
                foreach (var item in player.OwnedItems)
                {
                    if (item.IsActive)
                    {
                        item.Use(player, playersList);
                    }
                }
            }

            foreach (var player in PlayerSorter.SortForActivityCheck(playersList, GameState.Night))
            {
                #region Ниндзя
                if (player.Role is Ninja)
                {
                    foreach (var playerToCancelActivity in playersList)
                    {
                        if (player != playerToCancelActivity)
                        {
                            playerToCancelActivity.CancelActivity(player);
                        }
                    }
                }
                #endregion

                #region Громила
                if (player.Role is Hoodlum)
                {
                    var role = player.Role as Hoodlum;
                    if (role.PlayerToBlock != null)
                    {
                        role.LastPlayerToBlock = role.PlayerToBlock;
                        if (role.PlayerToBlock != player)
                        {
                            // Блокируем проверяемого
                            role.PlayerToBlock.CancelActivity();
                        }
                        if (role.PlayerToBlock.Role is Commissioner)
                        {
                            player.AddPoints("HoodlumBlockCom");
                        }
                        else if (role.PlayerToBlock.Role.Team == Team.Mafia)
                        {
                            player.AddPoints("HoodlumBlockMaf");
                        }
                        messageBuilder.PrepareTextReplacePlayer("HoodlumBlock", role.PlayerToBlock).SendPublic(gameChannel);
                        Pause();
                    }
                }
                #endregion

                #region Путана
                if (player.Role is Wench)
                {
                    var role = player.Role as Wench;
                    if (role.PlayerToCheck != null)
                    {
                        role.LastPlayerToCheck = role.PlayerToCheck;
                        if (role.PlayerToCheck != player)
                        {
                            // Блокируем проверяемого
                            role.PlayerToCheck.CancelActivity();
                        }
                        if (role.PlayerToCheck.Role is Commissioner)
                        {
                            player.AddPoints("WenchBlockCom");
                        }
                        else if (role.PlayerToCheck.Role.Team == Team.Mafia || role.PlayerToCheck.Role.Team == Team.Yakuza)
                        {
                            player.AddPoints("WenchBlockMaf");
                        }
                        if (randomGenerator.Next(0, 100) < settings.InfectionChancePercent && role.PlayerToCheck.DelayedDeath == null)
                        {
                            role.PlayerToCheck.DelayedDeath = 1;
                        }
                        messageBuilder.PrepareTextReplacePlayer("WenchBlock", role.PlayerToCheck).SendPublic(gameChannel);
                        Pause();
                    }
                }
                #endregion

                #region Бомж
                if (player.Role is Homeless)
                {
                    var role = player.Role as Homeless;
                    if (role.PlayerToCheck != null)
                    {
                        var message = String.Format("Статус {0} - {1}", messageBuilder.FormatName(role.PlayerToCheck), messageBuilder.FormatRole(role.PlayerToCheck.Role.Name));
                        messageBuilder.Text(message, false).SendPrivate(player);
                        messageBuilder.PrepareTextReplacePlayer("HomelessCheck", role.PlayerToCheck).SendPublic(gameChannel);
                        if (role.PlayerToCheck.Role.Team == Team.Mafia || role.PlayerToCheck.Role.Team == Team.Yakuza)
                        {
                            player.AddPoints("ComKillMaf");
                        }
                        Pause();
                    }
                }
                #endregion

                #region Комиссар
                if (player.Role is Commissioner)
                {
                    var role = player.Role as Commissioner;
                    if (role.PlayerToCheck != null)
                    {
                        var message = String.Format("Статус {0} - {1}", messageBuilder.FormatName(role.PlayerToCheck), messageBuilder.FormatRole(role.PlayerToCheck.Role.Name));
                        messageBuilder.Text(message, false).SendPrivate(player);
                        switch (role.PlayerToCheck.Role.Team)
                        {
                            case Team.Civil:
                                // Проверил мирного
                                messageBuilder.PrepareText("ComCheckCivil").SendPublic(gameChannel);
                                break;
                            case Team.Mafia:
                            case Team.Yakuza:
                                if (role.PlayerToCheck.HealedBy?.Player != null)
                                {
                                    role.PlayerToCheck.HealedBy.Player.AddPoints("DocHealMaf");
                                    messageBuilder.PrepareTextReplacePlayer("ComKillMafHelpDoc", role.PlayerToCheck).SendPublic(gameChannel);
                                }
                                else
                                {
                                    player.AddPoints("ComKillMaf");
                                    messageBuilder.PrepareTextReplacePlayer("ComKillMaf", role.PlayerToCheck).SendPublic(gameChannel);
                                    messageBuilder.PrepareTextReplacePlayer("ComKillMafPrivate", role.PlayerToCheck).SendPrivate(player);
                                    killManager.Kill(role.PlayerToCheck);
                                }
                                break;
                            case Team.Neutral:
                                if (role.PlayerToCheck.Role is RobinHood)
                                {
                                    messageBuilder.PrepareText("ComCheckCivil").SendPublic(gameChannel);
                                }
                                else
                                {
                                    if (role.PlayerToCheck.HealedBy?.Player != null)
                                    {
                                        role.PlayerToCheck.HealedBy.Player.AddPoints("DocHealMaf");
                                        messageBuilder.PrepareTextReplacePlayer("ComKillManiacHelpDoc", role.PlayerToCheck).SendPublic(gameChannel);
                                    }
                                    else
                                    {
                                        player.AddPoints("ComKillMaf");
                                        messageBuilder.PrepareTextReplacePlayer("ComKillManiac", role.PlayerToCheck).SendPublic(gameChannel);
                                        killManager.Kill(role.PlayerToCheck);
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        // Нет активности
                        messageBuilder.PrepareText("ComNoActive").SendPublic(gameChannel);
                    }
                    Pause();
                }
                #endregion

                #region Шериф
                if (player.Role is Sheriff)
                {
                    var role = player.Role as Sheriff;
                    if (role.PlayerToKill != null)
                    {
                        if (role.PlayerToKill.Role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("SheriffKillHighlander", role.PlayerToKill).SendPublic(gameChannel);
                            (role.PlayerToKill.Role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToKill.HealedBy?.Player != null)
                        {
                            switch (role.PlayerToKill.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToKill.HealedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToKill.Role is Commissioner)
                                    {
                                        role.PlayerToKill.HealedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToKill.HealedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            messageBuilder.PrepareTextReplacePlayer("SheriffKillHelpDoc", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        else
                        {
                            switch (role.PlayerToKill.Role.Team)
                            {
                                case Team.Civil:
                                    player.AddPoints("ComKillCivil");
                                    if (role.PlayerToKill.Role is Commissioner)
                                    {
                                        player.AddPoints("SheriffKillCom");
                                        achievementManager.Push(player.User, Achievement.Achievement.IdCivilKillCom);
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    player.AddPoints("ComKillMaf");
                                    break;
                            }
                            killManager.Kill(role.PlayerToKill);
                            messageBuilder.PrepareTextReplacePlayer("SheriffKill", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        Pause();
                    }
                }
                #endregion

                #region Доктор
                // Основная логика и начисление очков размазано по "спасению" в других событиях
                if (player.Role is Doctor)
                {
                    var role = player.Role as Doctor;
                    if (role.PlayerToHeal != null)
                    {
                        role.LastPlayerToHeal = role.PlayerToHeal;
                    }
                }
                #endregion

                #region Киллер
                if (player.Role is Killer)
                {
                    var role = player.Role as Killer;
                    if (role.PlayerToKill != null)
                    {
                        if (role.PlayerToKill.Role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("KillerKillHighlander", role.PlayerToKill).SendPublic(gameChannel);
                            (role.PlayerToKill.Role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToKill.HealedBy?.Player != null)
                        {
                            switch (role.PlayerToKill.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToKill.HealedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToKill.Role is Commissioner)
                                    {
                                        role.PlayerToKill.HealedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToKill.HealedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            messageBuilder.PrepareTextReplacePlayer("KillerKillHelpDoc", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        else
                        {
                            player.AddPoints("MafKill");
                            if (role.PlayerToKill.Role is Commissioner)
                            {
                                player.AddPoints("MafKillCom");
                            }
                            killManager.Kill(role.PlayerToKill);
                            messageBuilder.PrepareTextReplacePlayer("KillerKill", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        Pause();
                    }
                }
                #endregion

                #region Адвокат
                if (player.Role is Lawyer)
                {
                    var role = player.Role as Lawyer;
                    if (role.PlayerToCheck != null)
                    {
                        messageBuilder.PrepareTextReplacePlayer("LawyerCheck", role.PlayerToCheck).SendToTeam(Team.Mafia);
                        if (role.PlayerToCheck.Role is Commissioner)
                        {
                            player.AddPoints("LawyerCheckCom");
                        }
                        Pause();
                    }
                }
                #endregion

                #region Подрывник
                if (player.Role is Demoman)
                {
                    var role = player.Role as Demoman;
                    if (role.Counter == 0 && role.PlaceToDestroy != null)
                    {
                        var killedPlayersMessage = "💣 Сегодня был взорван " + role.PlaceToDestroy.Name + ". ";
                        var killedPlayers = new List<InGamePlayerInfo>();
                        foreach (var target in playersList)
                        {
                            if (target.IsAlive && target.Role.Team != Team.Mafia && target.PlaceToGo == role.PlaceToDestroy)
                            {
                                killedPlayers.Add(target);
                                killManager.Kill(target);
                                player.AddPoints("MaffKill");
                                if (target.Role is Commissioner)
                                {
                                    player.AddPoints("MaffKillCom");
                                }
                                killedPlayersMessage += messageBuilder.FormatRole(target.Role.NameCases[3]) + " " + messageBuilder.FormatName(target) + ", ";
                            }
                        }

                        if (killedPlayers.Count > 0)
                        {
                            killedPlayersMessage += "к сожалению, убило взрывом :(";
                        }
                        else
                        {
                            killedPlayersMessage += "К счастью, никто не погиб.";
                        }
                        messageBuilder.Text(killedPlayersMessage, false).SendPublic(gameChannel);

                        Pause();
                    }
                }
                #endregion

                #region Маньяк
                if (player.Role is Maniac)
                {
                    var role = player.Role as Maniac;
                    if (role.PlayerToKill != null)
                    {
                        if (role.PlayerToKill.Role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("ManiacKillHighlander", role.PlayerToKill).SendPublic(gameChannel);
                            (role.PlayerToKill.Role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToKill.HealedBy?.Player != null)
                        {
                            switch (role.PlayerToKill.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToKill.HealedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToKill.Role is Commissioner)
                                    {
                                        role.PlayerToKill.HealedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToKill.HealedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            messageBuilder.PrepareTextReplacePlayer("ManiacKillHelpDoc", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        else
                        {
                            player.AddPoints("NeutralKill");
                            killManager.Kill(role.PlayerToKill);
                            messageBuilder.PrepareTextReplacePlayer("ManiacKill", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        Pause();
                    }
                }
                #endregion
                
                #region Робин Гуд
                if (player.Role is RobinHood)
                {
                    var role = player.Role as RobinHood;
                    if (role.PlayerToKill != null)
                    {
                        if (role.PlayerToKill.Role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("RobinHoodKillHighlander", role.PlayerToKill).SendPublic(gameChannel);
                            (role.PlayerToKill.Role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToKill.Role is Citizen)
                        {
                            messageBuilder.PrepareTextReplacePlayer("RobinHoodKillCitizen", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        else if (role.PlayerToKill.HealedBy?.Player != null)
                        {
                            switch (role.PlayerToKill.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToKill.HealedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToKill.Role is Commissioner)
                                    {
                                        role.PlayerToKill.HealedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToKill.HealedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            messageBuilder.PrepareTextReplacePlayer("RobinHoodKillHelpDoc", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        else
                        {
                            player.AddPoints("NeutralKill");
                            killManager.Kill(role.PlayerToKill);
                            messageBuilder.PrepareTextReplacePlayer("RobinHoodKill", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        Pause();
                    }
                }
                #endregion
            }

            #region Мафия
            {
                var result = currentMafiaVote?.GetResult();
                if (result != null && !result.IsEmpty)
                {
                    if (result.HasOneLeader)
                    {
                        var leader = currentPlayers[result.Leader.Value];
                        string pointsStrategy = null;
                        if (leader.Role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("MafKillHighlander", leader).SendPublic(gameChannel);
                            (leader.Role as Highlander).WasAttacked = true;
                        }
                        else if (leader.HealedBy?.Player != null)
                        {
                            leader.HealedBy.Player.AddPoints("DocHealCivil");
                            if (leader.Role is Commissioner)
                            {
                                leader.HealedBy.Player.AddPoints("DocHealCom");
                            }
                            messageBuilder.PrepareTextReplacePlayer("MafKillHelpDoc", leader).SendPublic(gameChannel);
                        }
                        else
                        {
                            if (leader.Role is Commissioner)
                            {
                                pointsStrategy = "MafKillCom";
                            }
                            else if (leader.Role.Team == Team.Yakuza)
                            {
                                pointsStrategy = "MafKillOpposite";
                            }
                            foreach (var player in playersList)
                            {
                                if (player.Role.Team == Team.Mafia && result.IsVotedForLeader(player))
                                {
                                    if (pointsStrategy != null)
                                    {
                                        player.AddPoints(pointsStrategy);
                                    }
                                    player.AddPoints("MafKill");
                                }
                            }
                            messageBuilder.PrepareTextReplacePlayer("MafKill", leader).SendPublic(gameChannel);
                            killManager.Kill(leader);
                        }
                    }
                    else
                    {
                        // Не решили, кого убивать
                        messageBuilder.PrepareText("MafKillNoChoice").SendPublic(gameChannel);
                    }
                }
                else if (playersList.Any(delegate (InGamePlayerInfo value) { return value.Role is Mafioso && value.IsAlive; }))
                {
                    // Нет активности
                    messageBuilder.PrepareText("MafKillNoActive").SendPublic(gameChannel);
                }
                Pause();
            }
            #endregion

            #region Якудза
            {
                var result = currentYakuzaVote?.GetResult();
                if (result != null && !result.IsEmpty)
                {
                    if (result.HasOneLeader)
                    {
                        var leader = currentPlayers[result.Leader.Value];
                        string pointsStrategy = null;
                        if (leader.Role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("YakuzaKillHighlander", leader).SendPublic(gameChannel);
                            (leader.Role as Highlander).WasAttacked = true;
                        }
                        else if (leader.HealedBy?.Player != null)
                        {
                            leader.HealedBy.Player.AddPoints("DocHealCivil");
                            if (leader.Role is Commissioner)
                            {
                                leader.HealedBy.Player.AddPoints("DocHealCom");
                            }
                            messageBuilder.PrepareTextReplacePlayer("YakuzaKillHelpDoc", leader).SendPublic(gameChannel);
                        }
                        else
                        {
                            if (leader.Role is Commissioner)
                            {
                                pointsStrategy = "MafKillCom";
                            }
                            else if (leader.Role.Team == Team.Mafia)
                            {
                                pointsStrategy = "MafKillOpposite";
                            }
                            foreach (var player in playersList)
                            {
                                if (player.Role.Team == Team.Yakuza && result.IsVotedForLeader(player))
                                {
                                    if (pointsStrategy != null)
                                    {
                                        player.AddPoints(pointsStrategy);
                                    }
                                    player.AddPoints("MafKill");
                                }
                            }
                            messageBuilder.PrepareTextReplacePlayer("YakuzaKill", leader).SendPublic(gameChannel);
                            killManager.Kill(leader);
                        }
                    }
                    else
                    {
                        // Не решили, кого убивать
                        messageBuilder.PrepareText("YakuzaKillNoChoice").SendPublic(gameChannel);
                    }
                }
                else if (playersList.Any(delegate (InGamePlayerInfo value) { return value.Role is Yakuza && value.IsAlive; }))
                {
                    // Нет активности
                    messageBuilder.PrepareText("YakuzaKillNoActive").SendPublic(gameChannel);
                }
                Pause();
            }
            #endregion

            foreach (var player in playersList)
            {
                if (player.Role is Highlander)
                {
                    var highlander = player.Role as Highlander;
                    if (highlander.WasAttacked && highlander.PlayerToKill != null)
                    {
                        switch (highlander.PlayerToKill.Role.Team)
                        {
                            case Team.Civil:
                                player.AddPoints("ComKillCivil");
                                if (highlander.PlayerToKill.Role is Commissioner)
                                {
                                    player.AddPoints("SheriffKillCom");
                                    achievementManager.Push(player.User, Achievement.Achievement.IdCivilKillCom);
                                }
                                break;
                            case Team.Mafia:
                            case Team.Yakuza:
                                player.AddPoints("ComKillMaf");
                                break;
                        }
                        messageBuilder.PrepareTextReplacePlayer("HighlanderKill", highlander.PlayerToKill).SendPublic(gameChannel);
                        killManager.Kill(highlander.PlayerToKill);
                    }
                }
                
                #region Чернокнижник
                if (player.Role is Warlock)
                {
                    var role = player.Role as Warlock;
                    if (role.PlayerToCurse != null)
                    {
                        role.AvailableCursesCount--;
                        var killedPlayersMessage = "㊙ Неудачно сегодня закончилась ночь для ";
                        var killedPlayers = new List<InGamePlayerInfo>();
                        var mafiosoList = new List<InGamePlayerInfo>();
                        var yakuzaList = new List<InGamePlayerInfo>();
                        foreach (var target in playersList)
                        {
                            if (target.IsAlive && target != player && target.HasActivityAgainst(role.PlayerToCurse))
                            {
                                if (target.Role is Mafioso)
                                {
                                    mafiosoList.Add(target);
                                    continue;
                                }
                                if (target.Role is Yakuza)
                                {
                                    yakuzaList.Add(target);
                                    continue;
                                }
                                if (target.Role is Highlander && !(target.Role as Highlander).WasAttacked)
                                {
                                    continue;
                                }
                                killedPlayers.Add(target);
                                killManager.Kill(target);
                                player.AddPoints("NeutralKill");
                                killedPlayersMessage += messageBuilder.FormatRole(target.Role.NameCases[1]) + " " + messageBuilder.FormatName(target) + ", ";
                            }
                        }

                        // TODO Переделать, вынести в функцию, хоть что-то сделать :(
                        if (mafiosoList.Count > 0)
                        {
                            var playerToKillIdx = randomGenerator.Next(mafiosoList.Count);
                            var target = mafiosoList[playerToKillIdx];
                            killedPlayers.Add(target);
                            killManager.Kill(target);
                            player.AddPoints("NeutralKill");
                            killedPlayersMessage += messageBuilder.FormatRole(target.Role.NameCases[1]) + " " + messageBuilder.FormatName(target) + ", ";
                        }

                        // TODO Переделать, вынести в функцию, хоть что-то сделать :(
                        if (yakuzaList.Count > 0)
                        {
                            var playerToKillIdx = randomGenerator.Next(yakuzaList.Count);
                            var target = yakuzaList[playerToKillIdx];
                            killedPlayers.Add(target);
                            killManager.Kill(target);
                            player.AddPoints("NeutralKill");
                            killedPlayersMessage += messageBuilder.FormatRole(target.Role.NameCases[1]) + " " + messageBuilder.FormatName(target) + ", ";
                        }

                        if (killedPlayers.Count > 0)
                        {
                            killedPlayersMessage += "не надо было трогать проклятого " + messageBuilder.FormatTextReplacePlayer("{role4}", player) + " игрока...";
                            messageBuilder.Text(killedPlayersMessage, false).SendPublic(gameChannel);
                            Pause();
                        }
                    }
                }
                #endregion

                if (player.IsAlive && player.DelayedDeath != null)
                {
                    if (player.DelayedDeath-- == 0)
                    {
                        player.DelayedDeath = null;
                        messageBuilder.PrepareTextReplacePlayer("AIDSKill", player).SendPublic(gameChannel);
                        killManager.Kill(player);
                    }
                }
            }

            killManager.Apply();
            ClearActivity();
            if (!CheckWinConditions())
            {
                Pause();
                StartMorning();
            }
        }

        protected void ClearActivity()
        {
            foreach (var player in playersList)
            {
                player.ClearActivity();
            }
        }

        protected void Pause(int multipler = 1)
        {
            Task.Delay(settings.PauseTime * multipler).Wait();
        }

        /// <summary>
        /// Проверяет условия победы и объявляет победу
        /// </summary>
        /// <returns>true, если игра окончена</returns>
        protected bool CheckWinConditions()
        {
            // Победа команд или ничья (в живых никого)
            foreach (var team in new[] { Team.None, Team.Neutral, Team.Mafia, Team.Yakuza, Team.Civil })
            {
                if (isTeamWin(team))
                {
                    Win(team);
                    return true;
                }
            }

            // Победа команды + нейтрального персонажа
            foreach (var team in new[] { Team.Mafia, Team.Yakuza, Team.Civil })
            {
                if (!playersList.Any(delegate (InGamePlayerInfo value)
                    {
                        return value.Role.Team != team && value.Role.Team != Team.Neutral && value.IsAlive;
                    }))
                {
                    Win(team);
                    return true;
                }
            }

            // В живых 2 игрока
            if (playersList.Count(delegate (InGamePlayerInfo player) { return player.IsAlive; }) == 2)
            {
                // Ничья (любые 2 игрока из разных команд (НОЧЬ))
                if (currentState == GameState.Night)
                {
                    Win(Team.None);
                    return true;
                }
                // Ничья (маф + ком, як + ком (ДЕНЬ))
                else if (playersList.Exists(delegate (InGamePlayerInfo player) { return player.IsAlive && player.Role is Commissioner; }))
                {
                    Win(Team.None);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Объявляет победу и завершает игру
        /// </summary>
        /// <param name="team">Победившая команда</param>
        private void Win(Team team)
        {
            Console.WriteLine("Победила команда {0}", team);
            Pause();

            foreach (var player in playersList)
            {
                if (player.IsAlive)
                {
                    if (team == player.Role.Team)
                    {
                        player.AddPoints("WinAndSurvive");
                    }
                    else if (team == Team.None)
                    {
                        player.AddPoints("Draw");
                        player.DbUser.Draws++;
                    }
                    else if (player.Role.Team == Team.Neutral)
                    {
                        // К победе присоединяется нейтральный персонаж как к ничьей
                        player.AddPoints("Draw");
                        player.DbUser.Draws++;
                    }
                    player.AddPoints("Survive");
                    player.DbUser.Survivals++;
                }
                if (team == player.StartRole.Team)
                {
                    player.AddPoints("Win");
                    player.DbUser.Wins++;
                }
                player.DbUser.GamesPlayed++;
                player.DbUser.TotalPoints += player.CurrentGamePoints;
                player.ActualizeDbUser();
            }

            messageBuilder.PrepareText(String.Format("Win_{0}", team)).SendPublic(gameChannel);
            Pause();
            var message = "";
            foreach (var player in playersList)
            {
                message += String.Format("{0} {1} {3} ({4}) - {2}", Environment.NewLine, messageBuilder.FormatName(player), messageBuilder.FormatRole(player.StartRole.Name), player.CurrentGamePoints, player.DbUser.TotalPoints);
                if (!player.IsAlive)
                {
                    message += " (труп)";
                }
            }
            messageBuilder.Text(message, false).SendPublic(gameChannel);

            StopGame();
        }

        private bool isTeamWin(Team team)
        {
            return !playersList.Any(delegate (InGamePlayerInfo value) { return value.Role.Team != team && value.IsAlive; });
        }


        protected bool isTeamHavePlayers(Team team)
        {
            return playersList.Any(delegate (InGamePlayerInfo value) { return value.Role.Team == team && value.IsAlive; });
        }
    }
}
