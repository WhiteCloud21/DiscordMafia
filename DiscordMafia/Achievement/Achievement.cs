namespace DiscordMafia.Achievement
{
    public class Achievement
    {
        public const string Id10k = "10k";
        public const string Id25k = "25k";
        public const string Id50k = "50k";
        public const string Id100k = "100k";

        public const string IdRatingBronze = "ratingBronze";
        public const string IdRatingSilver = "ratingSilver";
        public const string IdRatingGold = "ratingGold";
        public const string IdRatingChampion = "ratingChamp";

        public const string IdCivilKillCom = "civilkillcom";

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

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }
    }
}
