namespace DiscordMafia.Roles
{
    public class Maniac : NeutralKiller
    {
        public override string Name
        {
            get
            {
                return "Маньяк";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "маньяк",
                    "маньяка",
                    "маньяку",
                    "маньяка",
                    "маньяком",
                    "маньяке",
                };
            }
        }
    }
}
