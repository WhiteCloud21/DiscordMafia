namespace DiscordMafia.Achievement
{
    public class Achievement
    {
        public const string IdPlayWithDeveloper = "playWithDeveloper";

        public const string Id10K = "10k";
        public const string Id25K = "25k";
        public const string Id50K = "50k";
        public const string Id100K = "100k";

        public const string IdPointsPerGame = "pointsPerGame";
        public const string IdPointsPerGameWasted = "pointsPerGameWasted";

        public const string IdRatingBronze = "ratingBronze";
        public const string IdRatingSilver = "ratingSilver";
        public const string IdRatingGold = "ratingGold";
        public const string IdRatingChampion = "ratingChamp";

        public const string IdGamesBronze = "gamesBronze";
        public const string IdGamesSilver = "gamesSilver";
        public const string IdGamesGold = "gamesGold";
        public const string IdGamesChampion = "gamesChamp";
        
        public const string IdWinStreak = "winStreak";
        public const string IdLoseStreak = "loseStreak";
        
        public const string IdPerfectWin = "perfectWin";
        public const string IdNeutralWin = "neutralWin";

        public const string IdCivilKillCom = "civilkillcom";
        public const string IdDocHealCom = "dochealcom";
        public const string IdDocHealMaf = "dochealmaf";
        public const string IdDemomanMaster = "demomanMaster";
        public const string IdWenchBlock = "wenchBlock";

        // Secret achievements
        public const string IdLostInStars = "lostInStars";

        public string Id
        {
            get;
            set;
        }

        public string Icon
        {
            get;
            set;
        }
    }
}
