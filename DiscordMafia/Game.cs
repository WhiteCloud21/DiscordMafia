using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordMafia.Client;
using DiscordMafia.DB;
using DiscordMafia.Items;
using DiscordMafia.Roles;
using DiscordMafia.Roles.Places;
using DiscordMafia.Voting;
using DiscordMafia.Lib;
using Microsoft.EntityFrameworkCore;
using static DiscordMafia.Config.MessageBuilder;
using DiscordMafia.Extensions;

namespace DiscordMafia
{
    public class Game
    {
        protected System.Threading.SynchronizationContext syncContext;
        protected Random randomGenerator = new Random();
        internal Timer timer;
        private DiscordSocketClient client;
        public Config.GameSettings Settings { get; protected set; }

        public GameState CurrentState { get; internal set; }
        public Dictionary<ulong, InGamePlayerInfo> CurrentPlayers { get; protected set; }
        public List<InGamePlayerInfo> PlayersList { get; protected set; }
        public SocketTextChannel GameChannel { get; protected set; }
        public DateTime StartedAt { get; set; }
        public string GameMode { get; set; } 
        protected RoleAssigner RoleAssigner { get; private set; }
        protected Vote CurrentDayVote { get; set; }
        protected BooleanVote CurrentEveningVote { get; set; }
        protected IMessage CurrentDayVoteMessage { get; set; }
        protected Vote CurrentMafiaVote { get; set; }
        protected Vote CurrentYakuzaVote { get; set; }
        public Config.MessageBuilder MessageBuilder { get; set; }
        protected KillManager KillManager { get; set; }
        public Achievement.AchievementManager AchievementManager { get; private set; }
        public Achievement.AchievementAssigner AchievementAssigner { get; private set; }
        internal int PlayerCollectingRemainingTime = 0;
        internal DateTime LastNotification = new DateTime(0);

        public Game( System.Threading.SynchronizationContext syncContext, DiscordSocketClient client, Config.MainSettings mainSettings)
        {
            GameChannel = client.GetChannel(mainSettings.GameChannel) as SocketTextChannel;
            AchievementManager = new Achievement.AchievementManager(this);
            AchievementAssigner = new Achievement.AchievementAssigner(this);

            this.syncContext = syncContext;
            this.client = client;

            RoleAssigner = new RoleAssigner();
            timer = new Timer();
            timer.Elapsed += OnTimer;
            CurrentState = GameState.Stopped;
            CurrentPlayers = new Dictionary<ulong, InGamePlayerInfo>();
            PlayersList = new List<InGamePlayerInfo>();
            LoadSettings();
            KillManager = new KillManager(this);
        }

        internal void LoadSettings(string gametype = null)
        {
            Settings = new Config.GameSettings(gametype);
            MessageBuilder = new Config.MessageBuilder(Settings, client, PlayersList);
            GameMode = gametype;
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
                        foreach (var player in PlayersList)
                        {
                            if (player.Role.Team == currentPlayer.Role.Team && !player.IsBot && player.User.Id != message.Author.Id)
                            {
                                MessageBuilder.Text($"{currentPlayer.GetName()}: {text}").SendPrivate(player);
                            }
                        }
                    }
                }
            }
        }

        internal void NightAction(BaseRole role)
        {
            if (Settings.ShowNightActions)
            {
                MessageBuilder.PrepareText("NightAction_" + role.GetType().Name).SendPublic(GameChannel);
            }
        }

        internal void DayVote(InGamePlayerInfo player, InGamePlayerInfo choosenPlayer)
        {
            if (player == null)
            {
                return;
            }
            if (CurrentState == GameState.Day)
            {
                if (choosenPlayer != null)
                {
                    try
                    {
                        CurrentDayVote.Add(player, choosenPlayer);
                        var voteCount = CurrentDayVote.GetResult().VoteCountByPlayer[choosenPlayer.User.Id];
                        MessageBuilder.Text(String.Format("{0} голосует за {1} ({2})!", player.GetName(), choosenPlayer.GetName(), voteCount)).SendPublic(GameChannel);
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
            if (CurrentState == GameState.Evening)
            {
                try
                {
                    var result = CurrentDayVote?.GetResult();
                    if (result != null && result.HasOneLeader)
                    {
                        var leader = CurrentPlayers[result.Leader.Value];
                        if (leader != player)
                        {
                            CurrentEveningVote.Add(player, voteValue);
                            MessageBuilder
                                .PrepareTextReplacePlayer(
                                    voteValue ? "EveningVote_Yes" : "EveningVote_No",
                                    player,
                                    additionalReplaceDictionary: new ReplaceDictionary {
                                        ["toHang"] = Encode(leader.GetName()),
                                        ["count"] = voteValue ? CurrentEveningVote.GetResult().YesCount : CurrentEveningVote.GetResult().NoCount
                                    }
                                )
                                .SendPublic(GameChannel);
                        }
                        else
                        {
                            MessageBuilder.Text(String.Format("{0} - нельзя голосовать за себя!", player.GetName())).SendPublic(GameChannel);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Игрок уже голосовал
                }
            }
        }

        internal void NightVote(InGamePlayerInfo player, InGamePlayerInfo choosenPlayer, bool silent = false)
        {
            if (player == null)
            {
                return;
            }
            if (CurrentState == GameState.Night)
            {
                if (choosenPlayer != null)
                {
                    if (player.Role is Mafioso)
                    {
                        try
                        {
                            if (!CurrentMafiaVote.HasVotes && !silent)
                            {
                                NightAction(player.Role);
                            }
                            CurrentMafiaVote.Add(player, choosenPlayer);
                            if (!silent)
                            {
                                var voteCount = CurrentMafiaVote.GetResult().VoteCountByPlayer[choosenPlayer.User.Id];
                                var message = String.Format("{0} голосует за убийство {1} ({2})!", player.GetName(), choosenPlayer.GetName(), voteCount);
                                MessageBuilder.Text(message).SendToTeam(Team.Mafia);
                            }
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
                            if (!CurrentYakuzaVote.HasVotes && !silent)
                            {
                                NightAction(player.Role);
                            }
                            CurrentYakuzaVote.Add(player, choosenPlayer);
                            if (!silent)
                            {
                                var voteCount = CurrentYakuzaVote.GetResult().VoteCountByPlayer[choosenPlayer.User.Id];
                                var message = String.Format("{0} голосует за убийство {1} ({2})!", player.GetName(), choosenPlayer.GetName(), voteCount);
                                MessageBuilder.Text(message).SendToTeam(Team.Yakuza);
                            }
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

            AchievementAssigner.AfterGame();
            AchievementManager.Apply();

            KillManager.Clear();
            CurrentPlayers.Clear();
            PlayersList.Clear();
            CurrentState = GameState.Stopped;
            client.SetGameAsync(null);
        }

        private void OnTimer(object sender)
        {
            timer.Stop();
            syncContext.Post(new System.Threading.SendOrPostCallback(
                delegate (object state)
                {
                    switch (CurrentState)
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
                MessageBuilder.Text(message, false).SendPublic(GameChannel);

                timer.Interval = Math.Min(Settings.PlayerCollectingTime, 60000);
                PlayerCollectingRemainingTime -= (int)timer.Interval;
                timer.Start();
                return;
            }
            if (CurrentState == GameState.PlayerCollecting && CurrentPlayers.Count >= Settings.MinPlayers)
            {
                MessageBuilder.Text(String.Format("Набор игроков окончен. Участвуют: {0}", CurrentPlayers.Count)).SendPublic(GameChannel);
                RoleAssigner.AssignRoles(this.PlayersList, this.Settings);
                // TODO подтверждение ролей

                var mafiaMessage = "Состав мафии" + Environment.NewLine;
                var yakuzaMessage = "Состав японской мафии" + Environment.NewLine;

                foreach (var player in PlayersList)
                {
                    var roleWelcomeParam = String.Format("GameStart_Role_{0}", player.Role.GetType().Name);
                    var photoName = String.Format("roles/card{0}.png", player.Role.GetType().Name);
                    MessageBuilder.PrepareTextReplacePlayer(roleWelcomeParam, player, "GameStart_Role_Default").AddImage(photoName).SendPrivate(player);
                    switch (player.Role.Team)
                    {
                        case Team.Mafia:
                            mafiaMessage += String.Format("{0} - {1} (`{2}`)", MessageBuilder.FormatName(player), MessageBuilder.FormatRole(player.Role.Name), player.GetName()) + Environment.NewLine;
                            break;
                        case Team.Yakuza:
                            yakuzaMessage += String.Format("{0} - {1} (`{2}`)", MessageBuilder.FormatName(player), MessageBuilder.FormatRole(player.Role.Name), player.GetName()) + Environment.NewLine;
                            break;
                    }
                    if (player.Role is Sergeant)
                    {
                        var commissioner = (from p in PlayersList where p.Role is Commissioner select p).FirstOrDefault();
                        if (commissioner != null)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("CheckStatus", commissioner).SendPrivate(player);
                            MessageBuilder.PrepareTextReplacePlayer("CheckStatus", player).SendPrivate(commissioner);
                        }
                    }
                }
                mafiaMessage += "Можете обсуждать свои действия в личных сообщениях или через бота.";
                yakuzaMessage += "Можете обсуждать свои действия в личных сообщениях или через бота.";
                Pause();

                // Состав мафий
                MessageBuilder.Text(mafiaMessage, false).SendToTeam(Team.Mafia);
                MessageBuilder.Text(yakuzaMessage, false).SendToTeam(Team.Yakuza);

                if (Settings.StartFromNight)
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
                MessageBuilder.Text(String.Format("Недостаточно игроков ({0}/{1})", CurrentPlayers.Count, Settings.MinPlayers)).SendPublic(GameChannel);
                StopGame();
            }
        }

        internal void CheckNextCheckpoint()
        {
            if (CurrentState == GameState.Day || CurrentState == GameState.Evening || CurrentState == GameState.Night)
            {
                var isAllReady = true;
                var dayVoteResult = CurrentDayVote?.GetResult();
                foreach (var player in PlayersList)
                {
                    if (player.IsAlive)
                    {
                        isAllReady = player.IsReady(CurrentState);
                        if (player.Role is Demoman && CurrentState == GameState.Night)
                        {
                            if ((player.Role as Demoman).Counter == 0)
                            {
                                isAllReady = false;
                            }
                        }
                        if (CurrentState == GameState.Evening && dayVoteResult != null && dayVoteResult.HasOneLeader)
                        {
                            if (CurrentPlayers[dayVoteResult.Leader.Value] == player)
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
            if (playerNum > 0 && playerNum <= PlayersList.Count)
            {
                var player = PlayersList[playerNum - 1];
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
            if (int.TryParse(request, out int itemNum) && itemNum > 0 && itemNum <= BaseItem.AvailableItems.Length)
            {
                return BaseItem.AvailableItems[itemNum - 1].GetType().GetTypeInfo().GetConstructor(Type.EmptyTypes).Invoke(new object[0]) as BaseItem;
            }
            return null;
        }

        /// <summary>
        /// Возвращает игрока по его ID Discord
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="onlyAlive"></param>
        /// <returns></returns>
        internal InGamePlayerInfo GetPlayerInfo(ulong userId, bool onlyAlive = true)
        {
            if (CurrentPlayers.ContainsKey(userId))
            {
                var player = CurrentPlayers[userId];
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
            foreach (var player in this.PlayersList)
            {
                if (player.IsAlive)
                {
                    message += i + " - " + MessageBuilder.FormatName(player) + $" (`{player.GetName()}`)" + Environment.NewLine;
                }
                i++;
            }
            if (echo)
            {
                if (sendTo != null)
                {
                    MessageBuilder.Text(message, false).SendPrivate(sendTo);
                }
                else
                {
                    MessageBuilder.Text(message, false).SendPublic(GameChannel);
                }
            }
            return message;
        }

        private void StartMorning()
        {
            foreach (var player in PlayersList)
            {
                player.ClearActivity();
            }
            timer.Interval = Settings.MorningTime;
            MessageBuilder.PrepareText("StartMorning").SendPublic(GameChannel);
            CurrentState = GameState.Morning;
            timer.Start();
        }

        private void StartDay()
        {
            GetAlivePlayersMesssage(keyboardCommand: "/посадить");
            MessageBuilder.PrepareText("StartDay").SendPublic(GameChannel);

            foreach (var player in PlayersList)
            {
                if (player.IsAlive)
                {
                    player.Role.DayInfo(this, player);
                }
            }

            timer.Interval = Settings.DayTime;
            CurrentState = GameState.Day;
            CurrentDayVote = new Vote();
            timer.Start();
        }

        private void StartEvening()
        {
            MessageBuilder.PrepareText("StartEvening").SendPublic(GameChannel);

            var dayVoteResult = CurrentDayVote?.GetResult();
            if (dayVoteResult != null && dayVoteResult.HasOneLeader)
            {
                MessageBuilder.PrepareTextReplacePlayer("EveningVoteInfo", CurrentPlayers[dayVoteResult.Leader.Value]).SendPublic(GameChannel);
                timer.Interval = Settings.EveningTime;
                foreach (var player in PlayersList)
                {
                    player.SkipTurn(false);
                }
            }
            else
            {
                timer.Interval = 1;
            }
            CurrentState = GameState.Evening;
            CurrentEveningVote = new BooleanVote();
            timer.Start();
        }

        private void StartNight()
        {
            MessageBuilder.PrepareText("StartNight").SendPublic(GameChannel);

            foreach (var player in PlayersList)
            {
                if (player.IsAlive)
                {
                    player.Role.NightInfo(this, player);
                }
            }

            timer.Interval = Settings.NightTime;
            CurrentState = GameState.Night;
            CurrentMafiaVote = new Vote();
            CurrentYakuzaVote = new Vote();
            timer.Start();
        }

        private void EndEvening()
        {
            Console.WriteLine("EndEvening");
            var result = CurrentDayVote?.GetResult();
            var eveningResult = CurrentEveningVote?.GetResult();

            foreach (var player in PlayerSorter.SortForActivityCheck(PlayersList, GameState.Day))
            {
                #region Судья
                // Основная логика и начисление очков размазано по "спасению" в других событиях
                if (player.Role is Judge)
                {
                    var role = player.Role as Judge;
                    if (role.PlayerToInteract != null)
                    {
                        role.LastPlayerToJustufy = role.PlayerToInteract;
                    }
                }
                #endregion

                #region Прокурор
                if (player.Role is Prosecutor)
                {
                    var role = player.Role as Prosecutor;
                    if (role.PlayerToInteract != null && role.PlayerToInteract.IsAlive)
                    {
                        if (role.PlayerToInteract.Role is Elder)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("ProsecutorKillElder", role.PlayerToInteract).SendPublic(GameChannel);
                            (role.PlayerToInteract.Role as Elder).WasAttacked = true;
                        }
                        else if (role.PlayerToInteract.JustifiedBy?.Player != null)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("ProsecutorKillJudgeJustify", role.PlayerToInteract).SendPublic(GameChannel);
                            if (role.PlayerToInteract.Role is Commissioner)
                            {
                                role.PlayerToInteract.JustifiedBy.Player.AddPoints("JudgeJustifyCom");
                            }
                            switch (role.PlayerToInteract.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToInteract.JustifiedBy.Player.AddPoints("JudgeJustifyCivil");
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToInteract.JustifiedBy.Player.AddPoints("JudgeJustifyMaf");
                                    break;
                            }
                        }
                        else
                        {
                            switch (role.PlayerToInteract.Role.Team)
                            {
                                case Team.Civil:
                                    player.AddPoints("ComKillCivil");
                                    if (role.PlayerToInteract.Role is Commissioner)
                                    {
                                        player.AddPoints("SheriffKillCom");
                                        AchievementManager.Push(player.User, Achievement.Achievement.IdCivilKillCom);
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    player.AddPoints("ComKillMaf");
                                    break;
                            }
                            KillManager.Kill(role.PlayerToInteract);
                            MessageBuilder.PrepareTextReplacePlayer("ProsecutorKill", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        Pause();
                    }
                }
                #endregion
            }

            if (result != null && !result.IsEmpty)
            {
                if (result.HasOneLeader && (Settings.EveningTime == 0 || (eveningResult != null && eveningResult.Result.HasValue && eveningResult.Result.Value)))
                {
                    var leader = CurrentPlayers[result.Leader.Value];
                    if (leader.JustifiedBy != null)
                    {
                        MessageBuilder.PrepareTextReplacePlayer("JudgeJustify", leader).SendPublic(GameChannel);
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
                        MessageBuilder.PrepareTextReplacePlayer("DayKillElder", leader).SendPublic(GameChannel);
                        (leader.Role as Elder).WasAttacked = true;
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
                            pointsByTeam[Team.Civil] += Settings.Points.GetPoints("CivilDayKillCom");
                            pointsByTeam[Team.Mafia] += Settings.Points.GetPoints("MafKillCom");
                            pointsByTeam[Team.Yakuza] += Settings.Points.GetPoints("MafKillCom");
                        }
                        switch (leader.Role.Team)
                        {
                            case Team.Civil:
                                pointsByTeam[Team.Civil] += Settings.Points.GetPoints("CivilKillCivil");
                                pointsByTeam[Team.Mafia] += Settings.Points.GetPoints("MafKill");
                                pointsByTeam[Team.Yakuza] += Settings.Points.GetPoints("MafKill");
                                break;
                            case Team.Mafia:
                                pointsByTeam[Team.Civil] += Settings.Points.GetPoints("CivilKillMaf");
                                pointsByTeam[Team.Yakuza] += Settings.Points.GetPoints("MafKillOpposite");
                                break;
                            case Team.Yakuza:
                                pointsByTeam[Team.Civil] += Settings.Points.GetPoints("CivilKillMaf");
                                pointsByTeam[Team.Mafia] += Settings.Points.GetPoints("MafKillOpposite");
                                break;
                        }

                        foreach (var player in PlayersList)
                        {
                            if (result.IsVotedForLeader(player))
                            {
                                player.AddPoints(pointsByTeam[player.Role.Team]);
                            }
                        }
                        MessageBuilder.PrepareTextReplacePlayer("DayKill", leader).SendPublic(GameChannel);
                        KillManager.Kill(leader);
                    }
                }
                else
                {
                    // Не решили, кого посадить
                    MessageBuilder.PrepareText("DayKillNoChoice").SendPublic(GameChannel);
                }
            }
            else
            {
                // Нет активности
                MessageBuilder.PrepareText("DayKillNoActive").SendPublic(GameChannel);
            }


            foreach (var player in PlayerSorter.SortForActivityCheck(PlayersList, GameState.Day))
            {
                #region Старейшина
                if (player.Role is Elder elder)
                {
                    if (elder.WasAttacked && elder.PlayerToInteract != null)
                    {
                        if (elder.PlayerToInteract.Role is Commissioner)
                        {
                            player.AddPoints("CivilDayKillCom");
                        }
                        switch (elder.PlayerToInteract.Role.Team)
                        {
                            case Team.Civil:
                                player.AddPoints("CivilKillCivil");
                                break;
                            case Team.Mafia:
                            case Team.Yakuza:
                                player.AddPoints("CivilKillMaf");
                                break;
                        }
                        Pause();
                        KillManager.Kill(elder.PlayerToInteract);
                        MessageBuilder.PrepareTextReplacePlayer("ElderKill", elder.PlayerToInteract).SendPublic(GameChannel);
                    }
                }
                #endregion
            }

            KillManager.Apply();
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
            CurrentDayVoteMessage = null;
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
            MessageBuilder.PrepareText("EndNight").SendPublic(GameChannel);
            Pause(2);

            // Предметы
            foreach (var player in PlayersList)
            {
                foreach (var item in player.OwnedItems.SortForUse(CurrentState))
                {
                    item.Use(player, PlayersList);
                }
            }

            foreach (var player in PlayerSorter.SortForActivityCheck(PlayersList, GameState.Night))
            {
                #region Зеркало
                if (player.Role is Mirror)
                {
                    var role = player.Role as Mirror;
                    var mafiosoList = new List<InGamePlayerInfo>();
                    var yakuzaList = new List<InGamePlayerInfo>();
                    foreach (var target in PlayersList)
                    {
                        if (target.IsAlive && target != player && target.HasActivityAgainst(player))
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
                            if (target.Role is ITargetedRole targetedRole)
                            {
                                // Применяем действие игрока на него самого, либо очищаем цель (немного грязи, но что поделать)
                                try
                                {
                                    targetedRole.PlayerToInteract = target;
                                }
                                catch
                                {
                                    try
                                    {
                                        targetedRole.PlayerToInteract = null;
                                    }
                                    catch
                                    {
                                        // Nothing to do
                                    }
                                }
                            }
                        }
                    }

                    // TODO Переделать, вынести в функцию, хоть что-то сделать (ага, еще раз) :(
                    if (mafiosoList.Count > 0)
                    {
                        var playerToKillIdx = randomGenerator.Next(mafiosoList.Count);
                        var target = mafiosoList[playerToKillIdx];
                        foreach (var mafioso in mafiosoList)
                        {
                            mafioso.CancelActivity(player);
                            NightVote(mafioso, target, true);
                        }
                    }
                    if (yakuzaList.Count > 0)
                    {
                        var playerToKillIdx = randomGenerator.Next(yakuzaList.Count);
                        var target = yakuzaList[playerToKillIdx];
                        foreach (var yakuza in yakuzaList)
                        {
                            yakuza.CancelActivity(player);
                            NightVote(yakuza, target, true);
                        }
                    }
                }
                #endregion

                #region Ниндзя
                if (player.Role is Ninja)
                {
                    foreach (var playerToCancelActivity in PlayersList)
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
                    if (role.PlayerToInteract != null)
                    {
                        role.LastPlayerToBlock = role.PlayerToInteract;
                        if (role.PlayerToInteract != player)
                        {
                            // Блокируем проверяемого
                            role.PlayerToInteract.CancelActivity();
                        }
                        if (role.PlayerToInteract.Role is Commissioner)
                        {
                            player.AddPoints("HoodlumBlockCom");
                        }
                        else if (role.PlayerToInteract.Role.Team == Team.Mafia)
                        {
                            player.AddPoints("HoodlumBlockMaf");
                        }
                        MessageBuilder.PrepareTextReplacePlayer("HoodlumBlock", role.PlayerToInteract).SendPublic(GameChannel);
                        Pause();
                    }
                }
                #endregion

                #region Путана
                if (player.Role is Wench)
                {
                    var role = player.Role as Wench;
                    if (role.PlayerToInteract != null)
                    {
                        role.LastPlayerToCheck = role.PlayerToInteract;
                        if (role.PlayerToInteract != player)
                        {
                            // Блокируем проверяемого
                            role.PlayerToInteract.CancelActivity();
                        }
                        if (role.PlayerToInteract.Role is Commissioner)
                        {
                            player.AddPoints("WenchBlockCom");
                        }
                        else if (role.PlayerToInteract.Role.Team == Team.Mafia || role.PlayerToInteract.Role.Team == Team.Yakuza)
                        {
                            player.AddPoints("WenchBlockMaf");
                        }
                        if (randomGenerator.Next(0, 100) < Settings.InfectionChancePercent && role.PlayerToInteract.DelayedDeath == null)
                        {
                            role.PlayerToInteract.DelayedDeath = 1;
                        }
                        MessageBuilder.PrepareTextReplacePlayer("WenchBlock", role.PlayerToInteract).SendPublic(GameChannel);
                        Pause();
                    }
                }
                #endregion

                #region Хакер
                if (player.Role is Hacker)
                {
                    var role = player.Role as Hacker;
                    if (role.PlayerToInteract != null)
                    {
                        MessageBuilder.PrepareTextReplacePlayer("HackerCheck", role.PlayerToInteract).SendPublic(GameChannel);
                        player.AddPoints("HackerHack");
                        if (role.PlayerToInteract.Role is UniqueRole)
                        {
                            player.AddPoints("HackerHackUnique");
                        }
                        Pause();
                    }
                }
                #endregion

                #region Бомж
                if (player.Role is Homeless)
                {
                    var role = player.Role as Homeless;
                    if (role.PlayerToInteract != null)
                    {
                        MessageBuilder.PrepareTextReplacePlayer("CheckStatus", role.PlayerToInteract).SendPrivate(player);
                        MessageBuilder.PrepareTextReplacePlayer("HomelessCheck", role.PlayerToInteract).SendPublic(GameChannel);
                        if (role.PlayerToInteract.Role.Team == Team.Mafia || role.PlayerToInteract.Role.Team == Team.Yakuza)
                        {
                            player.AddPoints("ComKillMaf");
                        }
                        Pause();
                    }
                }
                #endregion

                #region Шпион
                if (player.Role is Spy)
                {
                    var role = player.Role as Spy;
                    if (role.PlayerToInteract != null)
                    {
                        var commissioner = (from p in PlayersList where (p.Role is Commissioner && p.IsAlive) select p).FirstOrDefault();
                        if (commissioner != null)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("CheckStatus", role.PlayerToInteract).SendPrivate(commissioner);
                        }
                        if (role.PlayerToInteract.Role.Team == Team.Mafia || role.PlayerToInteract.Role.Team == Team.Yakuza)
                        {
                            player.AddPoints("ComKillMaf");
                        }
                        Pause();
                    }
                }
                #endregion

                #region Комиссар и сержант
                if (player.Role is Commissioner)
                {
                    var role = player.Role as Commissioner;
                    if (role.PlayerToInteract != null)
                    {
                        MessageBuilder.PrepareTextReplacePlayer("CheckStatus", role.PlayerToInteract).SendPrivate(player);
                        var sergeant = (from p in PlayersList where (p.Role is Sergeant && p.IsAlive) select p).FirstOrDefault();
                        if (sergeant != null)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("CheckStatus", role.PlayerToInteract).SendPrivate(sergeant);
                        }
                        switch (role.PlayerToInteract.Role.Team)
                        {
                            case Team.Civil:
                                // Проверил мирного
                                MessageBuilder.PrepareText("ComCheckCivil").SendPublic(GameChannel);
                                break;
                            case Team.Mafia:
                            case Team.Yakuza:
                                if (role.PlayerToInteract.HealedBy?.Player != null)
                                {
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealMaf");
                                    MessageBuilder.PrepareTextReplacePlayer("ComKillMafHelpDoc", role.PlayerToInteract).SendPublic(GameChannel);
                                }
                                else
                                {
                                    player.AddPoints("ComKillMaf");
                                    MessageBuilder.PrepareTextReplacePlayer("ComKillMaf", role.PlayerToInteract).SendPublic(GameChannel);
                                    MessageBuilder.PrepareTextReplacePlayer("ComKillMafPrivate", role.PlayerToInteract).SendPrivate(player);
                                    KillManager.Kill(role.PlayerToInteract);
                                }
                                break;
                            case Team.Neutral:
                                if (role.PlayerToInteract.Role is RobinHood)
                                {
                                    MessageBuilder.PrepareText("ComCheckCivil").SendPublic(GameChannel);
                                }
                                else
                                {
                                    if (role.PlayerToInteract.HealedBy?.Player != null)
                                    {
                                        role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealMaf");
                                        MessageBuilder.PrepareTextReplacePlayer("ComKillManiacHelpDoc", role.PlayerToInteract).SendPublic(GameChannel);
                                    }
                                    else
                                    {
                                        player.AddPoints("ComKillMaf");
                                        MessageBuilder.PrepareTextReplacePlayer("ComKillManiac", role.PlayerToInteract).SendPublic(GameChannel);
                                        KillManager.Kill(role.PlayerToInteract);
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        // Нет активности
                        MessageBuilder.PrepareText("ComNoActive").SendPublic(GameChannel);
                    }
                    Pause();
                }
                #endregion

                #region Шериф
                if (player.Role is Sheriff)
                {
                    var role = player.Role as Sheriff;
                    if (role.PlayerToInteract != null)
                    {
                        if (role.PlayerToInteract.Role is Highlander)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("SheriffKillHighlander", role.PlayerToInteract).SendPublic(GameChannel);
                            (role.PlayerToInteract.Role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToInteract.HealedBy?.Player != null)
                        {
                            switch (role.PlayerToInteract.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToInteract.Role is Commissioner)
                                    {
                                        role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            MessageBuilder.PrepareTextReplacePlayer("SheriffKillHelpDoc", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        else
                        {
                            switch (role.PlayerToInteract.Role.Team)
                            {
                                case Team.Civil:
                                    player.AddPoints("ComKillCivil");
                                    if (role.PlayerToInteract.Role is Commissioner)
                                    {
                                        player.AddPoints("SheriffKillCom");
                                        AchievementManager.Push(player.User, Achievement.Achievement.IdCivilKillCom);
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    player.AddPoints("ComKillMaf");
                                    break;
                            }
                            KillManager.Kill(role.PlayerToInteract);
                            MessageBuilder.PrepareTextReplacePlayer("SheriffKill", role.PlayerToInteract).SendPublic(GameChannel);
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
                    if (role.PlayerToInteract != null)
                    {
                        role.LastPlayerToHeal = role.PlayerToInteract;
                    }
                }
                #endregion

                #region Киллер
                if (player.Role is Killer)
                {
                    var role = player.Role as Killer;
                    if (role.PlayerToInteract != null)
                    {
                        if (role.PlayerToInteract.Role is Highlander)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("KillerKillHighlander", role.PlayerToInteract).SendPublic(GameChannel);
                            (role.PlayerToInteract.Role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToInteract.HealedBy?.Player != null)
                        {
                            switch (role.PlayerToInteract.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToInteract.Role is Commissioner)
                                    {
                                        role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            MessageBuilder.PrepareTextReplacePlayer("KillerKillHelpDoc", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        else
                        {
                            player.AddPoints("MafKill");
                            if (role.PlayerToInteract.Role is Commissioner)
                            {
                                player.AddPoints("MafKillCom");
                            }
                            KillManager.Kill(role.PlayerToInteract);
                            MessageBuilder.PrepareTextReplacePlayer("KillerKill", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        Pause();
                    }
                }
                #endregion

                #region Адвокат
                if (player.Role is Lawyer)
                {
                    var role = player.Role as Lawyer;
                    if (role.PlayerToInteract != null)
                    {
                        MessageBuilder.PrepareTextReplacePlayer("CheckStatus", role.PlayerToInteract).SendToTeam(Team.Mafia);
                        if (role.PlayerToInteract.Role is Commissioner)
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
                        foreach (var target in PlayersList)
                        {
                            if (target.IsAlive && target.Role.Team != Team.Mafia && target.PlaceToGo == role.PlaceToDestroy)
                            {
                                killedPlayers.Add(target);
                                KillManager.Kill(target);
                                player.AddPoints("MaffKill");
                                if (target.Role is Commissioner)
                                {
                                    player.AddPoints("MaffKillCom");
                                }
                                killedPlayersMessage += MessageBuilder.FormatRole(target.Role.NameCases[3]) + " " + MessageBuilder.FormatName(target) + ", ";
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
                        MessageBuilder.Text(killedPlayersMessage, false).SendPublic(GameChannel);

                        Pause();
                    }
                }
                #endregion

                #region Маньяк
                if (player.Role is Maniac)
                {
                    var role = player.Role as Maniac;
                    if (role.PlayerToInteract != null)
                    {
                        if (role.PlayerToInteract.Role is Highlander)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("ManiacKillHighlander", role.PlayerToInteract).SendPublic(GameChannel);
                            (role.PlayerToInteract.Role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToInteract.HealedBy?.Player != null)
                        {
                            switch (role.PlayerToInteract.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToInteract.Role is Commissioner)
                                    {
                                        role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            MessageBuilder.PrepareTextReplacePlayer("ManiacKillHelpDoc", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        else
                        {
                            player.AddPoints("NeutralKill");
                            KillManager.Kill(role.PlayerToInteract);
                            MessageBuilder.PrepareTextReplacePlayer("ManiacKill", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        Pause();
                    }
                }
                #endregion

                #region Робин Гуд
                if (player.Role is RobinHood)
                {
                    var role = player.Role as RobinHood;
                    if (role.PlayerToInteract != null)
                    {
                        if (role.PlayerToInteract.Role is Highlander)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("RobinHoodKillHighlander", role.PlayerToInteract).SendPublic(GameChannel);
                            (role.PlayerToInteract.Role as Highlander).WasAttacked = true;
                        }
                        else if (role.PlayerToInteract.Role is Citizen)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("RobinHoodKillCitizen", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        else if (role.PlayerToInteract.HealedBy?.Player != null)
                        {
                            switch (role.PlayerToInteract.Role.Team)
                            {
                                case Team.Civil:
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealCivil");
                                    if (role.PlayerToInteract.Role is Commissioner)
                                    {
                                        role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealCom");
                                    }
                                    break;
                                case Team.Mafia:
                                case Team.Yakuza:
                                    role.PlayerToInteract.HealedBy.Player.AddPoints("DocHealMaf");
                                    break;
                            }
                            MessageBuilder.PrepareTextReplacePlayer("RobinHoodKillHelpDoc", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        else
                        {
                            player.AddPoints("NeutralKill");
                            KillManager.Kill(role.PlayerToInteract);
                            MessageBuilder.PrepareTextReplacePlayer("RobinHoodKill", role.PlayerToInteract).SendPublic(GameChannel);
                        }
                        Pause();
                    }
                }
                #endregion
            }

            #region Мафия
            {
                var result = CurrentMafiaVote?.GetResult();
                if (result != null && !result.IsEmpty)
                {
                    if (result.HasOneLeader)
                    {
                        var leader = CurrentPlayers[result.Leader.Value];
                        string pointsStrategy = null;
                        if (leader.Role is Highlander)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("MafKillHighlander", leader).SendPublic(GameChannel);
                            (leader.Role as Highlander).WasAttacked = true;
                        }
                        else if (leader.HealedBy?.Player != null)
                        {
                            leader.HealedBy.Player.AddPoints("DocHealCivil");
                            if (leader.Role is Commissioner)
                            {
                                leader.HealedBy.Player.AddPoints("DocHealCom");
                            }
                            MessageBuilder.PrepareTextReplacePlayer("MafKillHelpDoc", leader).SendPublic(GameChannel);
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
                            foreach (var player in PlayersList)
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
                            MessageBuilder.PrepareTextReplacePlayer("MafKill", leader).SendPublic(GameChannel);
                            KillManager.Kill(leader);
                        }
                    }
                    else
                    {
                        // Не решили, кого убивать
                        MessageBuilder.PrepareText("MafKillNoChoice").SendPublic(GameChannel);
                    }
                }
                else if (PlayersList.Any(delegate (InGamePlayerInfo value) { return value.Role is Mafioso && value.IsAlive; }))
                {
                    // Нет активности
                    MessageBuilder.PrepareText("MafKillNoActive").SendPublic(GameChannel);
                }
                Pause();
            }
            #endregion

            #region Якудза
            {
                var result = CurrentYakuzaVote?.GetResult();
                if (result != null && !result.IsEmpty)
                {
                    if (result.HasOneLeader)
                    {
                        var leader = CurrentPlayers[result.Leader.Value];
                        string pointsStrategy = null;
                        if (leader.Role is Highlander)
                        {
                            MessageBuilder.PrepareTextReplacePlayer("YakuzaKillHighlander", leader).SendPublic(GameChannel);
                            (leader.Role as Highlander).WasAttacked = true;
                        }
                        else if (leader.HealedBy?.Player != null)
                        {
                            leader.HealedBy.Player.AddPoints("DocHealCivil");
                            if (leader.Role is Commissioner)
                            {
                                leader.HealedBy.Player.AddPoints("DocHealCom");
                            }
                            MessageBuilder.PrepareTextReplacePlayer("YakuzaKillHelpDoc", leader).SendPublic(GameChannel);
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
                            foreach (var player in PlayersList)
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
                            MessageBuilder.PrepareTextReplacePlayer("YakuzaKill", leader).SendPublic(GameChannel);
                            KillManager.Kill(leader);
                        }
                    }
                    else
                    {
                        // Не решили, кого убивать
                        MessageBuilder.PrepareText("YakuzaKillNoChoice").SendPublic(GameChannel);
                    }
                }
                else if (PlayersList.Any(delegate (InGamePlayerInfo value) { return value.Role is Yakuza && value.IsAlive; }))
                {
                    // Нет активности
                    MessageBuilder.PrepareText("YakuzaKillNoActive").SendPublic(GameChannel);
                }
                Pause();
            }
            #endregion

            foreach (var player in PlayersList)
            {
                if (player.Role is Highlander)
                {
                    var highlander = player.Role as Highlander;
                    if (highlander.WasAttacked && highlander.PlayerToInteract != null)
                    {
                        switch (highlander.PlayerToInteract.Role.Team)
                        {
                            case Team.Civil:
                                player.AddPoints("ComKillCivil");
                                if (highlander.PlayerToInteract.Role is Commissioner)
                                {
                                    player.AddPoints("SheriffKillCom");
                                    AchievementManager.Push(player.User, Achievement.Achievement.IdCivilKillCom);
                                }
                                break;
                            case Team.Mafia:
                            case Team.Yakuza:
                                player.AddPoints("ComKillMaf");
                                break;
                        }
                        MessageBuilder.PrepareTextReplacePlayer("HighlanderKill", highlander.PlayerToInteract).SendPublic(GameChannel);
                        KillManager.Kill(highlander.PlayerToInteract);
                    }
                }

                #region Чернокнижник
                if (player.Role is Warlock)
                {
                    var role = player.Role as Warlock;
                    if (role.PlayerToInteract != null)
                    {
                        role.AvailableCursesCount--;
                        var killedPlayersMessage = "㊙ Неудачно сегодня закончилась ночь для ";
                        var killedPlayers = new List<InGamePlayerInfo>();
                        var mafiosoList = new List<InGamePlayerInfo>();
                        var yakuzaList = new List<InGamePlayerInfo>();
                        foreach (var target in PlayersList)
                        {
                            if (target.IsAlive && target != player && target.HasActivityAgainst(role.PlayerToInteract))
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
                                KillManager.Kill(target);
                                player.AddPoints("NeutralKill");
                                killedPlayersMessage += MessageBuilder.FormatRole(target.Role.NameCases[1]) + " " + MessageBuilder.FormatName(target) + ", ";
                            }
                        }

                        // TODO Переделать, вынести в функцию, хоть что-то сделать :(
                        if (mafiosoList.Count > 0)
                        {
                            var playerToKillIdx = randomGenerator.Next(mafiosoList.Count);
                            var target = mafiosoList[playerToKillIdx];
                            killedPlayers.Add(target);
                            KillManager.Kill(target);
                            player.AddPoints("NeutralKill");
                            killedPlayersMessage += MessageBuilder.FormatRole(target.Role.NameCases[1]) + " " + MessageBuilder.FormatName(target) + ", ";
                        }

                        // TODO Переделать, вынести в функцию, хоть что-то сделать :(
                        if (yakuzaList.Count > 0)
                        {
                            var playerToKillIdx = randomGenerator.Next(yakuzaList.Count);
                            var target = yakuzaList[playerToKillIdx];
                            killedPlayers.Add(target);
                            KillManager.Kill(target);
                            player.AddPoints("NeutralKill");
                            killedPlayersMessage += MessageBuilder.FormatRole(target.Role.NameCases[1]) + " " + MessageBuilder.FormatName(target) + ", ";
                        }

                        if (killedPlayers.Count > 0)
                        {
                            killedPlayersMessage += "не надо было трогать проклятого " + MessageBuilder.FormatTextReplacePlayer("{role4}", player) + " игрока...";
                            MessageBuilder.Text(killedPlayersMessage, false).SendPublic(GameChannel);
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
                        MessageBuilder.PrepareTextReplacePlayer("AIDSKill", player).SendPublic(GameChannel);
                        KillManager.Kill(player);
                    }
                }
            }

            KillManager.Apply();
            ClearActivity();
            if (!CheckWinConditions())
            {
                Pause();
                StartMorning();
            }
        }

        protected void ClearActivity()
        {
            foreach (var player in PlayersList)
            {
                player.ClearActivity();
            }
        }

        protected void Pause(int multipler = 1)
        {
            Task.Delay(Settings.PauseTime * multipler).Wait();
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
                if (IsTeamWin(team))
                {
                    Win(team);
                    return true;
                }
            }

            // Победа команды + нейтрального персонажа
            foreach (var team in new[] { Team.Mafia, Team.Yakuza, Team.Civil })
            {
                if (!PlayersList.Any(delegate (InGamePlayerInfo value)
                    {
                        return value.Role.Team != team && value.Role.Team != Team.Neutral && value.IsAlive;
                    }))
                {
                    Win(team);
                    return true;
                }
            }

            // В живых 2 игрока
            if (PlayersList.Count(delegate (InGamePlayerInfo player) { return player.IsAlive; }) == 2)
            {
                // Ничья (любые 2 игрока из разных команд (НОЧЬ))
                if (CurrentState == GameState.Night)
                {
                    Win(Team.None);
                    return true;
                }
                // Ничья (маф + ком, як + ком (ДЕНЬ))
                else if (PlayersList.Exists(delegate (InGamePlayerInfo player) { return player.IsAlive && player.Role is Commissioner; }))
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

            using (var gameContext = new GameContext())
            {
                var game = new DB.Game()
                {
                    StartedAt = StartedAt,
                    FinishedAt = DateTime.Now
                };
                foreach (var player in PlayersList)
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
                    gameContext.Entry(player.DbUser).State = EntityState.Modified;

                    var gameUser = new GameUser()
                    {
                        UserId = player.DbUser.Id,
                        StartRole = player.StartRole.GetType().Name,
                        Role = player.Role.GetType().Name,
                        Score = player.CurrentGamePoints,
                        RatingAfterGame = player.DbUser.Rate
                    };
                    gameUser.Result = gameUser.Result.SetFlag(GameUser.ResultFlags.Survive, player.IsAlive);
                    gameUser.Result = gameUser.Result.SetFlag(GameUser.ResultFlags.Win, player.StartRole.Team == team);
                    gameUser.Result = gameUser.Result.SetFlag(GameUser.ResultFlags.Draw, team == Team.None);

                    gameUser.Game = game;
                    gameContext.GameUsers.Add(gameUser);
                }

                game.PlayersCount = PlayersList.Count;
                game.Winner = team;
                game.GameMode = GameMode;

                gameContext.Games.Add(game);
                
                gameContext.SaveChanges();
            }

            MessageBuilder.PrepareText(String.Format("Win_{0}", team)).SendPublic(GameChannel);
            Pause();
            var message = "";
            foreach (var player in PlayersList)
            {
                message += String.Format("{0} {1} {3} ({4}) - {2}", Environment.NewLine, MessageBuilder.FormatName(player), MessageBuilder.FormatRole(player.StartRole.Name), player.CurrentGamePoints, player.DbUser.TotalPoints);
                if (!player.IsAlive)
                {
                    message += " (труп)";
                }
            }
            MessageBuilder.Text(message, false).SendPublic(GameChannel);

            StopGame();
        }

        private bool IsTeamWin(Team team)
        {
            return !PlayersList.Any(delegate (InGamePlayerInfo value) { return value.Role.Team != team && value.IsAlive; });
        }


        protected bool IsTeamHavePlayers(Team team)
        {
            return PlayersList.Any(delegate (InGamePlayerInfo value) { return value.Role.Team == team && value.IsAlive; });
        }
    }
}
