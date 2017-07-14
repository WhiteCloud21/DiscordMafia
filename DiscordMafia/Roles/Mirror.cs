namespace DiscordMafia.Roles
{
    public class Mirror : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Зеркало";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "зеркало",
                    "зеркала",
                    "зеркалу",
                    "зеркало",
                    "зеркалом",
                    "зеркале",
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
