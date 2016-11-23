using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DiscordMafia.Client;
using DiscordMafia.Items;
using DiscordMafia.Roles;
using DiscordMafia.Roles.Places;
using DiscordMafia.Voting;
using DiscordMafia.DB;

namespace DiscordMafia
{
    public class Game
    {
        protected System.Threading.SynchronizationContext syncContext;
        protected Random randomGenerator = new Random();
        protected Timer timer;
        private DiscordClient client;
        public Config.GameSettings settings { get; protected set; }

        public GameState currentState { get; protected set; }
        public Dictionary<ulong, InGamePlayerInfo> currentPlayers { get; protected set; }
        public List<InGamePlayerInfo> playersList { get; protected set; }
        public ulong gameChannel { get; protected set; }
        protected RoleAssigner roleAssigner { get; private set; }
        protected Vote currentDayVote { get; set; }
        protected Message currentDayVoteMessage { get; set; }
        protected Vote currentMafiaVote { get; set; }
        protected Vote currentYakuzaVote { get; set; }
        public Config.MessageBuilder messageBuilder { get; set; }
        protected KillManager killManager { get; set; }
        protected int PlayerCollectingRemainingTime = 0;

        public Game(System.Threading.SynchronizationContext syncContext, DiscordClient client, Config.MainSettings mainSettings)
        {
            gameChannel = mainSettings.GameChannel;

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

        private void LoadSettings()
        {
            settings = new Config.GameSettings();
            messageBuilder = new Config.MessageBuilder(settings, client, playersList);
            Console.WriteLine("Settings loaded");
        }

        public void OnPublicMessage(MessageEventArgs update)
        {
            string text = update.Message.Text;
            Channel channel = update.Channel;
            UserWrapper user = update.User;
            if (channel.Id != gameChannel)
            {
                return;
            }
            var currentPlayer = GetPlayerInfo(user.Id);
            if (text.StartsWith("/"))
            {
                var parts = text.Split(' ');
                switch (parts[0])
                {
                    case "/test":
                        messageBuilder.Text("Test is OK").SendPublic(channel, tts: true);
                        return;
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
                    case "/cancel": // TODO Разделить и писать про отмену в публичный канал
                        if (currentState == GameState.PlayerCollecting)
                        {
                            UnRegisterPlayer(user);
                            return;
                        }
                        if (currentPlayer != null && currentPlayer.role != null)
                        {
                            currentPlayer.CancelActivity();
                            messageBuilder.Text("Ваш голос отменен").SendPublic(gameChannel);
                        }
                        return;
                    case "/посадить":
                    case "/imprison":
                        if (parts.Length > 1)
                        {
                            Vote(currentPlayer, parts[1]);
                            CheckNextCheckpoint();
                        }
                        return;
                }
            }
        }

        public void OnPrivateMessage(MessageEventArgs update)
        {
            string text = update.Message.Text;
            UserWrapper user = update.User;
            var currentPlayer = GetPlayerInfo(user.Id);
            if (text.StartsWith("/"))
            {
                var parts = text.Split(' ');
                switch (parts[0])
                {
                    case "/test":
                        client.SetGame("Test");
                        return;
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
                        messageBuilder.Text(Stat.GetTopAsString(messageBuilder, topCount), false).SendPrivate(user.Id);
                        return;
                    case "/mystat":
                    case "/мойстат":
                        messageBuilder.Text(Stat.GetStatAsString(user)).SendPrivate(user.Id);
                        return;
                    case "/recalculate":
                        if (user.IsAdmin())
                        {
                            Stat.RecalculateAll();
                            messageBuilder.Text("OK").SendPrivate(user.Id);
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
                            var message = "Предметы, доступные для покупки: " + Environment.NewLine;
                            for (var i = 0; i < BaseItem.AvailableItems.Length; i++)
                            {
                                var item = BaseItem.AvailableItems[i];
                                var itemInPlayer = currentPlayer.GetItem(item);
                                message += item.Name + " - предмет ";
                                if (itemInPlayer != null)
                                {
                                    if (itemInPlayer.IsActive)
                                    {
                                        message += "будет использован этой ночью";
                                    }
                                    else
                                    {
                                        message += "уже использован";
                                    }
                                }
                                else
                                {
                                    message += "доступен для покупки";
                                }
                                message += ". Цена: " + item.Cost + Environment.NewLine;
                                message += "<i>" + item.Description + "</i>";
                                message += Environment.NewLine;
                                message += Environment.NewLine;
                            }
                            messageBuilder.Text(message, false).SendPrivate(currentPlayer);
                        }
                        else
                        {
                            messageBuilder.PrepareText("ShopDisabled").SendPrivate(currentPlayer);
                        }
                        return;
                    case "/отмена":
                    case "/cancel": // TODO Разделить и писать про отмену в публичный канал
                        if (currentPlayer != null)
                        {
                            currentPlayer.CancelActivity();
                            messageBuilder.Text("Ваш голос отменен").SendPrivate(currentPlayer);
                        }
                        return;
                    case "/убить":
                    case "/kill":
                        if (currentPlayer == null)
                        {
                            return;
                        }
                        if (currentPlayer.role is Highlander && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var highlander = (currentPlayer.role as Highlander);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && highlander.PlayerToKill == null)
                            {
                                highlander.PlayerToKill = playerToKill;
                                NightAction(currentPlayer.role);
                                messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                CheckNextCheckpoint();
                            }
                            return;
                        }
                        if (currentPlayer.role is Sheriff && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var sheriff = (currentPlayer.role as Sheriff);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && sheriff.PlayerToKill == null)
                            {
                                sheriff.PlayerToKill = playerToKill;
                                NightAction(currentPlayer.role);
                                messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                CheckNextCheckpoint();
                            }
                            return;
                        }
                        if (currentPlayer.role is Killer && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var killer = (currentPlayer.role as Killer);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && killer.PlayerToKill == null)
                            {
                                killer.PlayerToKill = playerToKill;
                                NightAction(currentPlayer.role);
                                var message = String.Format("Киллер {0} выбрал в качестве жертвы {1}!", currentPlayer.GetName(), playerToKill.GetName());
                                messageBuilder.Text(message).SendToTeam(Team.Mafia);
                                CheckNextCheckpoint();
                            }
                            return;
                        }
                        if (currentPlayer.role is Maniac && parts.Length > 1 && currentState == GameState.Night)
                        {
                            var maniac = (currentPlayer.role as Maniac);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && maniac.PlayerToKill == null)
                            {
                                maniac.PlayerToKill = playerToKill;
                                NightAction(currentPlayer.role);
                                messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                CheckNextCheckpoint();
                            }
                            return;
                        }
                        if (parts.Length > 1)
                        {
                            Vote(currentPlayer, parts[1]);
                            CheckNextCheckpoint();
                        }
                        return;
                    case "/посадить":
                    case "/imprison":
                        if (currentPlayer == null)
                        {
                            return;
                        }
                        if (currentPlayer.role is Elder && parts.Length > 1 && currentState == GameState.Day)
                        {
                            var elder = (currentPlayer.role as Elder);
                            var playerToKill = GetPlayerInfo(parts[1]);
                            if (playerToKill != null && elder.PlayerToKill == null)
                            {
                                elder.PlayerToKill = playerToKill;
                                messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                CheckNextCheckpoint();
                            }
                            return;
                        }
                        if (parts.Length > 1)
                        {
                            Vote(currentPlayer, parts[1]);
                        }
                        return;
                    case "/пров":
                    case "/check":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.role is Commissioner && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var commissioner = (currentPlayer.role as Commissioner);
                                var playerToCheck = GetPlayerInfo(parts[1]);
                                if (playerToCheck != null && commissioner.PlayerToCheck == null)
                                {
                                    commissioner.PlayerToCheck = playerToCheck;
                                    NightAction(currentPlayer.role);
                                    messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                    CheckNextCheckpoint();
                                }
                            }
                            else if (currentPlayer.role is Homeless && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var homeless = (currentPlayer.role as Homeless);
                                var playerToCheck = GetPlayerInfo(parts[1]);
                                if (playerToCheck != null && homeless.PlayerToCheck == null)
                                {
                                    homeless.PlayerToCheck = playerToCheck;
                                    NightAction(currentPlayer.role);
                                    messageBuilder.Text("Голос принят.").SendPrivate(currentPlayer);
                                    CheckNextCheckpoint();
                                }
                            }
                            else if (currentPlayer.role is Lawyer && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var lawyer = (currentPlayer.role as Lawyer);
                                var playerToCheck = GetPlayerInfo(parts[1]);
                                if (playerToCheck != null && lawyer.PlayerToCheck == null)
                                {
                                    lawyer.PlayerToCheck = playerToCheck;
                                    NightAction(currentPlayer.role);
                                    var message = String.Format("Адвокат {0} выбрал {1} для проверки!", currentPlayer.GetName(), lawyer.PlayerToCheck.GetName());
                                    messageBuilder.Text(message).SendToTeam(Team.Mafia);
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
                            if (currentPlayer.role is Wench && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var wench = (currentPlayer.role as Wench);
                                var playerToCheck = GetPlayerInfo(parts[1]);
                                if (playerToCheck != null && wench.PlayerToCheck == null)
                                {
                                    try
                                    {
                                        wench.PlayerToCheck = playerToCheck;
                                        NightAction(currentPlayer.role);
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
                    case "/лечить":
                    case "/heal":
                        {
                            if (currentPlayer == null)
                            {
                                return;
                            }
                            if (currentPlayer.role is Doctor && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var doctor = (currentPlayer.role as Doctor);
                                var playerToHeal = GetPlayerInfo(parts[1]);
                                if (playerToHeal != null && doctor.PlayerToHeal == null)
                                {
                                    try
                                    {
                                        doctor.PlayerToHeal = playerToHeal;
                                        NightAction(currentPlayer.role);
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
                            if (currentPlayer.role is Judge && parts.Length > 1 && currentState == GameState.Day)
                            {
                                var judge = (currentPlayer.role as Judge);
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
                            if (currentPlayer.role is Demoman && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var demoman = (currentPlayer.role as Demoman);
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
                            if (currentPlayer.role.Team != Team.Mafia && parts.Length > 1 && currentState == GameState.Night)
                            {
                                var placeToGo = GetPlaceInfo(parts[1]);
                                if (placeToGo != null)
                                {
                                    try
                                    {
                                        currentPlayer.placeToGo = placeToGo;
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
            if (currentPlayer?.role != null)
            {
                if (currentPlayer.role.Team == Team.Mafia || currentPlayer.role.Team == Team.Yakuza)
                {
                    foreach (var player in playersList)
                    {
                        if (player.role.Team == currentPlayer.role.Team && !player.isBot && player.user.Id != update.User.Id)
                        {
                            update.Channel.SendMessage($"{currentPlayer.GetName()}: {text}");
                        }
                    }
                }
            }
        }

        private void Help(UserWrapper user)
        {
            var currentPlayer = currentPlayers.ContainsKey(user.Id) ? currentPlayers[user.Id] : null;
            var message = "============Игровые команды============" + Environment.NewLine;
            message += "/help - вывод этой справки (в приват боту);" + Environment.NewLine;
            message += "/join, /я - регистрация в игре (во время набора игроков);" + Environment.NewLine;
            message += "/cancel, /отмена - выход из игры (во время набора игроков);" + Environment.NewLine;
            message += "/mystat, /мойстат - ваша статистика(в приват боту);" + Environment.NewLine;
            message += "/top, /топ - лучшие игроки;" + Environment.NewLine;
            message += "/buy, /купить - посмотреть доступные вещи для покупки(только во время игры, в приват боту);" + Environment.NewLine;
            //message += "/announceon, /предупреждай - сообщать о начале игры(в приват боту);" + Environment.NewLine;
            //message += "/announceoff, /отстань - больше не сообщать о начале игры(в приват боту);" + Environment.NewLine;

            if (currentPlayer != null && currentPlayer.isAlive && currentPlayer.role != null)
            {
                message += Environment.NewLine;
                message += "=========== Помощь по статусу===========" + Environment.NewLine;
                message += "Ваш статус - " + messageBuilder.FormatRole(currentPlayer.role.Name) + Environment.NewLine;
                switch (currentPlayer.role.Team)
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

                message += messageBuilder.GetText(string.Format("RoleHelp_{0}", currentPlayer.role.GetType().Name)) + Environment.NewLine;
            }

            message += Environment.NewLine;
            message += "======Помощь по начислению очков======" + Environment.NewLine;
            foreach (var pointConfig in settings.Points.Values)
            {
                message += String.Format("{0}: {1}", pointConfig.Description, pointConfig.Points) + Environment.NewLine;
            }

            messageBuilder.Text(message, false).SendPrivate(user.Id);
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
                var playerInfo = new InGamePlayerInfo(player, settings);
                playerInfo.dbUser.Save();
                playerInfo.isBot = isBot;
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

        private void Vote(InGamePlayerInfo player, string voteForRequest)
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
                        var voteCount = currentDayVote.GetResult().VoteCountByPlayer[voteFor.user.Id];
                        messageBuilder.Text(String.Format("{0} голосует за {1} ({2})!", player.GetName(), voteFor.GetName(), voteCount)).SendPublic(gameChannel);
                    }
                    catch (ArgumentException)
                    {
                        // Игрок уже голосовал
                    }
                }
            }
            else if (currentState == GameState.Night)
            {
                var voteFor = GetPlayerInfo(voteForRequest);
                if (voteFor != null)
                {
                    if (player.role is Mafioso)
                    {
                        try
                        {
                            if (!currentMafiaVote.HasVotes)
                            {
                                NightAction(player.role);
                            }
                            currentMafiaVote.Add(player, voteFor);
                            var voteCount = currentMafiaVote.GetResult().VoteCountByPlayer[voteFor.user.Id];
                            var message = String.Format("{0} голосует за убийство {1} ({2})!", player.GetName(), voteFor.GetName(), voteCount);
                            messageBuilder.Text(message).SendToTeam(Team.Mafia);
                        }
                        catch (ArgumentException)
                        {
                            // Игрок уже голосовал
                        }
                    }
                    else if (player.role is Yakuza)
                    {
                        try
                        {
                            if (!currentYakuzaVote.HasVotes)
                            {
                                NightAction(player.role);
                            }
                            currentYakuzaVote.Add(player, voteFor);
                            var voteCount = currentYakuzaVote.GetResult().VoteCountByPlayer[voteFor.user.Id];
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
                var message = String.Format("Начинаю набор игроков. У вас <b>{0}</b> секунд.", settings.PlayerCollectingTime / 1000);
                message += Environment.NewLine + "<b>/join</b> (<b>/я</b>) - Присоединиться к игре";
                messageBuilder.Text(message, false).SendPublic(gameChannel);
                currentState = GameState.PlayerCollecting;
                timer.Interval = Math.Min(settings.PlayerCollectingTime, 60000);
                PlayerCollectingRemainingTime = (int)(settings.PlayerCollectingTime - timer.Interval);
                timer.Start();
                client.SetGame("Мафия (ожидание игроков)");
            }
        }

        private void StopGame()
        {
            timer.Stop();
            currentPlayers.Clear();
            playersList.Clear();
            currentState = GameState.Stopped;
            client.SetGame(null);
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
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
                    var roleWelcomeParam = String.Format("GameStart_Role_{0}", player.role.GetType().Name);
                    var photoName = String.Format("roles/card{0}.png", player.role.GetType().Name);
                    messageBuilder.PrepareTextReplacePlayer(roleWelcomeParam, player, "GameStart_Role_Default").AddImage(photoName).SendPrivate(player);
                    switch (player.role.Team)
                    {
                        case Team.Mafia:
                            mafiaMessage += String.Format("{0} - {1}", messageBuilder.FormatName(player), messageBuilder.FormatRole(player.role.Name)) + Environment.NewLine;
                            break;
                        case Team.Yakuza:
                            yakuzaMessage += String.Format("{0} - {1}", messageBuilder.FormatName(player), messageBuilder.FormatRole(player.role.Name)) + Environment.NewLine;
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
                client.SetGame("Мафия");
            }
            else
            {
                messageBuilder.Text(String.Format("Недостаточно игроков ({0}/{1})", currentPlayers.Count, settings.MinPlayers)).SendPublic(gameChannel);
                StopGame();
            }
        }

        protected void CheckNextCheckpoint()
        {
            if (currentState == GameState.Day || currentState == GameState.Night)
            {
                var isAllReady = true;
                foreach (var player in playersList)
                {
                    if (player.isAlive)
                    {
                        isAllReady = player.IsReady(currentState);
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
                if (!onlyAlive || player.isAlive)
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
            if (int.TryParse(request, out itemNum) && itemNum >= 0 && itemNum < BaseItem.AvailableItems.Length)
            {
                return BaseItem.AvailableItems[itemNum].GetType().GetConstructor(Type.EmptyTypes).Invoke(new object[0]) as BaseItem;
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
                if (!onlyAlive || player.isAlive)
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
                if (player.isAlive)
                {
                    message += i + " - " + messageBuilder.FormatName(player) + Environment.NewLine;
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
                player.CancelActivity();
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
                if (player.isAlive)
                {
                    player.role.DayInfo(this, player);
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

            timer.Interval = settings.EveningTime;
            currentState = GameState.Evening;
            timer.Start();
        }

        private void StartNight()
        {
            messageBuilder.PrepareText("StartNight").SendPublic(gameChannel);

            foreach (var player in playersList)
            {
                if (player.isAlive)
                {
                    player.role.NightInfo(this, player);
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

            foreach (var player in PlayerSorter.SortForActivityCheck(playersList, GameState.Day))
            {
                #region Судья
                // Основная логика и начисление очков размазано по "спасению" в других событиях
                if (player.role is Judge)
                {
                    var role = player.role as Judge;
                    if (role.PlayerToJustufy != null)
                    {
                        role.LastPlayerToJustufy = role.PlayerToJustufy;
                    }
                }
                #endregion
            }

            if (result != null && !result.IsEmpty)
            {
                if (result.HasOneLeader)
                {
                    var leader = currentPlayers[result.Leader.Value];
                    if (leader.justifiedBy != null)
                    {
                        messageBuilder.PrepareTextReplacePlayer("JudgeJustify", leader).SendPublic(gameChannel);
                        if (leader.role is Commissioner)
                        {
                            leader.justifiedBy.Player.AddPoints("JudgeJustifyCom");
                        }
                        switch (leader.role.Team)
                        {
                            case Team.Civil:
                                leader.justifiedBy.Player.AddPoints("JudgeJustifyCivil");
                                break;
                            case Team.Mafia:
                            case Team.Yakuza:
                                leader.justifiedBy.Player.AddPoints("JudgeJustifyMaf");
                                break;
                        }
                        Pause();
                    }
                    else if (leader.role is Elder)
                    {
                        messageBuilder.PrepareTextReplacePlayer("DayKillElder", leader).SendPublic(gameChannel);
                        var elder = leader.role as Elder;
                        if (elder.PlayerToKill != null)
                        {
                            if (elder.PlayerToKill.role is Commissioner)
                            {
                                leader.AddPoints("CivilDayKillCom");
                            }
                            switch (leader.role.Team)
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
                        if (leader.role is Commissioner)
                        {
                            pointsByTeam[Team.Civil] += settings.Points.GetPoints("CivilDayKillCom");
                            pointsByTeam[Team.Mafia] += settings.Points.GetPoints("MafKillCom");
                            pointsByTeam[Team.Yakuza] += settings.Points.GetPoints("MafKillCom");
                        }
                        switch (leader.role.Team)
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
                                player.AddPoints(pointsByTeam[player.role.Team]);
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
                foreach (var item in player.ownedItems)
                {
                    if (item.IsActive)
                    {
                        item.Use(player, playersList);
                    }
                }
            }

            foreach (var player in PlayerSorter.SortForActivityCheck(playersList, GameState.Night))
            {
                #region Путана
                if (player.role is Wench)
                {
                    var role = player.role as Wench;
                    if (role.PlayerToCheck != null)
                    {
                        role.LastPlayerToCheck = role.PlayerToCheck;
                        if (role.PlayerToCheck != player)
                        {
                            // Блокируем проверяемого
                            role.PlayerToCheck.CancelActivity();
                        }
                        if (role.PlayerToCheck.role is Commissioner)
                        {
                            player.AddPoints("WenchBlockCom");
                        }
                        else if (role.PlayerToCheck.role.Team == Team.Mafia || role.PlayerToCheck.role.Team == Team.Yakuza)
                        {
                            player.AddPoints("WenchBlockMaf");
                        }
                        if (randomGenerator.Next(0, 100) < settings.InfectionChancePercent && role.PlayerToCheck.delayedDeath == null)
                        {
                            role.PlayerToCheck.delayedDeath = 1;
                        }
                        messageBuilder.PrepareTextReplacePlayer("WenchBlock", role.PlayerToCheck).SendPublic(gameChannel);
                        Pause();
                    }
                }
                #endregion

                #region Бомж
                if (player.role is Homeless)
                {
                    var role = player.role as Homeless;
                    if (role.PlayerToCheck != null)
                    {
                        var message = String.Format("Статус {0} - {1}", messageBuilder.FormatName(role.PlayerToCheck), messageBuilder.FormatRole(role.PlayerToCheck.role.Name));
                        messageBuilder.Text(message, false).SendPrivate(player);
                        messageBuilder.PrepareTextReplacePlayer("HomelessCheck", role.PlayerToCheck).SendPublic(gameChannel);
                        if (role.PlayerToCheck.role.Team == Team.Mafia || role.PlayerToCheck.role.Team == Team.Yakuza)
                        {
                            player.AddPoints("ComKillMaf");
                        }
                        Pause();
                    }
                }
                #endregion

                #region Комиссар
                if (player.role is Commissioner)
                {
                    var role = player.role as Commissioner;
                    if (role.PlayerToCheck != null)
                    {
                        var message = String.Format("Статус {0} - {1}", messageBuilder.FormatName(role.PlayerToCheck), messageBuilder.FormatRole(role.PlayerToCheck.role.Name));
                        messageBuilder.Text(message, false).SendPrivate(player);
                        switch (role.PlayerToCheck.role.Team)
                        {
                            case Team.Civil:
                                // Проверил мирного
                                messageBuilder.PrepareText("ComCheckCivil").SendPublic(gameChannel);
                                break;
                            case Team.Mafia:
                            case Team.Yakuza:
                                if (role.PlayerToCheck.healedBy?.Player != null)
                                {
                                    role.PlayerToCheck.healedBy.Player.AddPoints("DocHealMaf");
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
                                if (role.PlayerToCheck.healedBy?.Player != null)
                                {
                                    role.PlayerToCheck.healedBy.Player.AddPoints("DocHealMaf");
                                    messageBuilder.PrepareTextReplacePlayer("ComKillManiacHelpDoc", role.PlayerToCheck).SendPublic(gameChannel);
                                }
                                else
                                {
                                    player.AddPoints("ComKillMaf");
                                    messageBuilder.PrepareTextReplacePlayer("ComKillManiac", role.PlayerToCheck).SendPublic(gameChannel);
                                    killManager.Kill(role.PlayerToCheck);
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
                if (player.role is Sheriff)
                {
                    var role = player.role as Sheriff;
                    if (role.PlayerToKill != null)
                    {
                        if (role.PlayerToKill.role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("SheriffKillHighlander", role.PlayerToKill).SendPublic(gameChannel);
                            (role.PlayerToKill.role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToKill.healedBy?.Player != null)
                        {
                            switch (role.PlayerToKill.role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToKill.healedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToKill.role is Commissioner)
                                    {
                                        role.PlayerToKill.healedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToKill.healedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            messageBuilder.PrepareTextReplacePlayer("SheriffKillHelpDoc", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        else
                        {
                            switch (role.PlayerToKill.role.Team)
                            {
                                case Team.Civil:
                                    player.AddPoints("ComKillCivil");
                                    if (role.PlayerToKill.role is Commissioner)
                                    {
                                        player.AddPoints("SheriffKillCom");
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
                if (player.role is Doctor)
                {
                    var role = player.role as Doctor;
                    if (role.PlayerToHeal != null)
                    {
                        role.LastPlayerToHeal = role.PlayerToHeal;
                    }
                }
                #endregion

                #region Киллер
                if (player.role is Killer)
                {
                    var role = player.role as Killer;
                    if (role.PlayerToKill != null)
                    {
                        if (role.PlayerToKill.role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("KillerKillHighlander", role.PlayerToKill).SendPublic(gameChannel);
                            (role.PlayerToKill.role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToKill.healedBy?.Player != null)
                        {
                            switch (role.PlayerToKill.role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToKill.healedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToKill.role is Commissioner)
                                    {
                                        role.PlayerToKill.healedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToKill.healedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            messageBuilder.PrepareTextReplacePlayer("KillerKillHelpDoc", role.PlayerToKill).SendPublic(gameChannel);
                        }
                        else
                        {
                            player.AddPoints("MafKill");
                            if (role.PlayerToKill.role is Commissioner)
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
                if (player.role is Lawyer)
                {
                    var role = player.role as Lawyer;
                    if (role.PlayerToCheck != null)
                    {
                        messageBuilder.PrepareTextReplacePlayer("LawyerCheck", role.PlayerToCheck).SendToTeam(Team.Mafia);
                        if (role.PlayerToCheck.role is Commissioner)
                        {
                            player.AddPoints("LawyerCheckCom");
                        }
                        Pause();
                    }
                }
                #endregion

                #region Подрывник
                if (player.role is Demoman)
                {
                    var role = player.role as Demoman;
                    if (role.Counter == 0 && role.PlaceToDestroy != null)
                    {
                        var killedPlayersMessage = "Сегодня был взорван " + role.PlaceToDestroy.Name + ". ";
                        var killedPlayers = new List<InGamePlayerInfo>();
                        foreach (var target in playersList)
                        {
                            if (target.isAlive && target.role.Team != Team.Mafia && target.placeToGo == role.PlaceToDestroy)
                            {
                                killedPlayers.Add(target);
                                killManager.Kill(target);
                                player.AddPoints("MaffKill");
                                if (target.role is Commissioner)
                                {
                                    player.AddPoints("MaffKillCom");
                                }
                                killedPlayersMessage += messageBuilder.FormatRole(target.role.NameCases[3]) + " " + messageBuilder.FormatName(target) + ", ";
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
                if (player.role is Maniac)
                {
                    var role = player.role as Maniac;
                    if (role.PlayerToKill != null)
                    {
                        if (role.PlayerToKill.role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("ManiacKillHighlander", role.PlayerToKill).SendPublic(gameChannel);
                            (role.PlayerToKill.role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToKill.healedBy?.Player != null)
                        {
                            switch (role.PlayerToKill.role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToKill.healedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToKill.role is Commissioner)
                                    {
                                        role.PlayerToKill.healedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToKill.healedBy.Player.AddPoints("DocHealMaf");
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
                        if (leader.role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("MafKillHighlander", leader).SendPublic(gameChannel);
                            (leader.role as Highlander).WasAttacked = true;
                        }
                        else if (leader.healedBy?.Player != null)
                        {
                            leader.healedBy.Player.AddPoints("DocHealCivil");
                            if (leader.role is Commissioner)
                            {
                                leader.healedBy.Player.AddPoints("DocHealCom");
                            }
                            messageBuilder.PrepareTextReplacePlayer("MafKillHelpDoc", leader).SendPublic(gameChannel);
                        }
                        else
                        {
                            if (leader.role is Commissioner)
                            {
                                pointsStrategy = "MafKillCom";
                            }
                            else if (leader.role.Team == Team.Yakuza)
                            {
                                pointsStrategy = "MafKillOpposite";
                            }
                            foreach (var player in playersList)
                            {
                                if (player.role.Team == Team.Mafia && result.IsVotedForLeader(player))
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
                else if (playersList.Any(delegate (InGamePlayerInfo value) { return value.role is Mafioso && value.isAlive; }))
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
                        if (leader.role is Highlander)
                        {
                            messageBuilder.PrepareTextReplacePlayer("YakuzaKillHighlander", leader).SendPublic(gameChannel);
                            (leader.role as Highlander).WasAttacked = true;
                        }
                        else if (leader.healedBy?.Player != null)
                        {
                            leader.healedBy.Player.AddPoints("DocHealCivil");
                            if (leader.role is Commissioner)
                            {
                                leader.healedBy.Player.AddPoints("DocHealCom");
                            }
                            messageBuilder.PrepareTextReplacePlayer("YakuzaKillHelpDoc", leader).SendPublic(gameChannel);
                        }
                        else
                        {
                            if (leader.role is Commissioner)
                            {
                                pointsStrategy = "MafKillCom";
                            }
                            else if (leader.role.Team == Team.Mafia)
                            {
                                pointsStrategy = "MafKillOpposite";
                            }
                            foreach (var player in playersList)
                            {
                                if (player.role.Team == Team.Yakuza && result.IsVotedForLeader(player))
                                {
                                    if (pointsStrategy != null)
                                    {
                                        player.AddPoints(pointsStrategy);
                                    }
                                    player.AddPoints(pointsStrategy);
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
                else if (isTeamHavePlayers(Team.Yakuza))
                {
                    // Нет активности
                    messageBuilder.PrepareText("YakuzaKillNoActive").SendPublic(gameChannel);
                }
                Pause();
            }
            #endregion

            foreach (var player in playersList)
            {
                if (player.role is Highlander)
                {
                    var highlander = player.role as Highlander;
                    if (highlander.WasAttacked && highlander.PlayerToKill != null)
                    {
                        messageBuilder.PrepareTextReplacePlayer("HighlanderKill", highlander.PlayerToKill).SendPublic(gameChannel);
                        killManager.Kill(highlander.PlayerToKill);
                    }
                }
                if (player.isAlive && player.delayedDeath != null)
                {
                    if (player.delayedDeath-- == 0)
                    {
                        player.delayedDeath = null;
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
                        return value.role.Team != team && value.role.Team != Team.Neutral && value.isAlive;
                    }))
                {
                    Win(team);
                    return true;
                }
            }

            // В живых 2 игрока
            if (playersList.Count(delegate (InGamePlayerInfo player) { return player.isAlive; }) == 2)
            {
                // Ничья (любые 2 игрока из разных команд (НОЧЬ))
                if (currentState == GameState.Night)
                {
                    Win(Team.None);
                    return true;
                }
                // Ничья (маф + ком, як + ком (ДЕНЬ))
                else if (playersList.Exists(delegate (InGamePlayerInfo player) { return player.isAlive && player.role is Commissioner; }))
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
                if (player.isAlive)
                {
                    if (team == player.role.Team)
                    {
                        player.AddPoints("WinAndSurvive");
                    }
                    else if (team == Team.None)
                    {
                        player.AddPoints("Draw");
                        player.dbUser.draws++;
                    }
                    else if (player.role.Team == Team.Neutral)
                    {
                        // К победе присоединяется нейтральный персонаж как к ничьей
                        player.AddPoints("Draw");
                        player.dbUser.draws++;
                    }
                    player.AddPoints("Survive");
                    player.dbUser.survivals++;
                }
                if (team == player.startRole.Team)
                {
                    player.AddPoints("Win");
                    player.dbUser.wins++;
                }
                player.dbUser.gamesPlayed++;
                player.dbUser.totalPoints += player.currentGamePoints;
                player.ActualizeDBUser();
            }

            messageBuilder.PrepareText(String.Format("Win_{0}", team)).SendPublic(gameChannel);
            Pause();
            var message = "";
            foreach (var player in playersList)
            {
                message += String.Format("{0} {1} {3} ({4}) - {2}", Environment.NewLine, messageBuilder.FormatName(player), messageBuilder.FormatRole(player.startRole.Name), player.currentGamePoints, player.dbUser.totalPoints);
                if (!player.isAlive)
                {
                    message += " (труп)";
                }
            }
            messageBuilder.Text(message, false).SendPublic(gameChannel);

            StopGame();
        }

        private bool isTeamWin(Team team)
        {
            return !playersList.Any(delegate (InGamePlayerInfo value) { return value.role.Team != team && value.isAlive; });
        }


        protected bool isTeamHavePlayers(Team team)
        {
            return playersList.Any(delegate (InGamePlayerInfo value) { return value.role.Team == team && value.isAlive; });
        }
    }
}
