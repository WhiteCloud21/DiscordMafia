using System;
using System.Collections.Generic;
using DiscordMafia.Config;
using DiscordMafia.Roles;

namespace DiscordMafia.Config
{
    public class GameSettings
    {
        public int MinPlayers { get; protected set; }
        public Dictionary<Team, int> MaxRandomPlayers { get; protected set; }
        public short MafPercent { get; protected set; }
        public short InfectionChancePercent { get; protected set; }

        public bool StartFromNight { get; protected set; }
        public bool ShowNightActions { get; protected set; }
        public int PlayerCollectingTime { get; protected set; }
        public int PauseTime { get; protected set; }
        public int MorningTime { get; protected set; }
        public int DayTime { get; protected set; }
        public int EveningTime { get; protected set; }
        public int NightTime { get; protected set; }

        public Messages Messages { get; private set; }
        public Points Points { get; private set; }
        public Roles Roles { get; private set; }

        public GameSettings()
        {
            MaxRandomPlayers = new Dictionary<Team, int>()
            {
                { Team.Civil, 2 },
                { Team.Mafia, 1 },
                { Team.Neutral, 1 },
                { Team.Yakuza, 1 },
            };
            MinPlayers = 3;
            MafPercent = 34;
            StartFromNight = true;
            PlayerCollectingTime = 60000;
            PauseTime = 1000;
            MorningTime = 2000;
            DayTime = 60000;
            EveningTime = 10000;
            NightTime = 60000;
            InfectionChancePercent = 33;
            ShowNightActions = true;
            Messages = Messages.getInstance("config/messages.xml");
            Console.WriteLine("Сообщения загружены");
            Points = Points.getInstance("config/points.xml");
            Console.WriteLine("Конфигурация очков загружена");
            Roles = Roles.getInstance("config/roles.xml");
            Console.WriteLine("Конфигурация ролей загружена");
        }
    }
}
