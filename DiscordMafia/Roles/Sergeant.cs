namespace DiscordMafia.Roles
{
    public class Sergeant : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Сержант";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "сержант",
                    "сержанта",
                    "сержанту",
                    "сержанта",
                    "сержантом",
                    "сержанте",
                };
            }
        }

        public override Team Team
        {
            get
            {
                return Team.Civil;
            }
        }
    }
}
