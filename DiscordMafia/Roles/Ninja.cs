namespace DiscordMafia.Roles
{
    public class Ninja : Yakuza
    {
        public override string Name
        {
            get
            {
                return "Ниндзя";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "ниндзя",
                    "ниндзя",
                    "ниндзя",
                    "ниндзя",
                    "ниндзя",
                    "ниндзя",
                };
            }
        }
    }
}
