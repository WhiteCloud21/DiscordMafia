namespace DiscordMafia.Achievement
{
    public class Achievement
    {
        public const string Id10K = "10k";
        public const string Id25K = "25k";
        public const string Id50K = "50k";
        public const string Id100K = "100k";

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
