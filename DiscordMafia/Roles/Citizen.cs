namespace DiscordMafia.Roles
{
    public class Citizen : BaseRole
    {
        public override string Name
        {
            get
            {
                return "Чиж";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "чиж",
                    "чижа",
                    "чижу",
                    "чижа",
                    "чижом",
                    "чиже",
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
