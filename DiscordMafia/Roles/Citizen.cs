namespace DiscordMafia.Roles
{
    public class Citizen : BaseRole
    {
        public override string Name
        {
            get
            {
                return "Мирный гражданин";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "мирный гражданин",
                    "мирного гражданина",
                    "мирному гражданину",
                    "мирного гражданина",
                    "мирным гражданином",
                    "мирном гражданине",
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
