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
        internal Timer timer;
        private DiscordSocketClient client;
        public Config.GameSettings settings { get; protected set; }

        public GameState currentState { get; internal set; }
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
        internal int PlayerCollectingRemainingTime = 0;

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

        internal void LoadSettings(string gametype = null)
        {
            settings = new Config.GameSettings(gametype);
            messageBuilder = new Config.MessageBuilder(settings, client, playersList);
            Console.WriteLine("Settings loaded");
        }

        public void OnPrivateMessage(SocketMessage message)
        {
            string text = message.Content;
            UserWrapper user = message.Author;
            var currentPlayer = GetPlayerInfo(user.Id);
            if (!text.StartsWith("/"))
            {
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
        }

        internal void NightAction(BaseRole role)
        {
            if (settings.ShowNightActions)
            {
                messageBuilder.PrepareText("NightAction_" + role.GetType().Name).SendPublic(gameChannel);
            }
        }

        internal void DayVote(InGamePlayerInfo player, int choosenPlayer)
        {
            if (player == null)
            {
                return;
            }
            if (currentState == GameState.Day)
            {
                var voteFor = GetPlayerInfo(choosenPlayer);
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

        internal void EveningVote(InGamePlayerInfo player, bool voteValue)
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

        internal void NightVote(InGamePlayerInfo player, int choosenPlayer)
        {
            if (player == null)
            {
                return;
            }
            if (currentState == GameState.Night)
            {
                var voteFor = GetPlayerInfo(choosenPlayer);
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

        internal void StopGame()
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

        internal void StopPlayerCollecting()
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

        internal void CheckNextCheckpoint()
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
        internal InGamePlayerInfo GetPlayerInfo(int playerNum, bool onlyAlive = true)
        {
            if (playerNum > 0 && playerNum <= playersList.Count)
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
        internal Place GetPlaceInfo(int placeNum)
        {
            if (placeNum >= 0 && placeNum < Place.AvailablePlaces.Length)
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
        internal BaseItem GetItemInfo(string request)
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
