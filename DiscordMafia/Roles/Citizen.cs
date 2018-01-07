namespace DiscordMafia.Roles
{
    public class Citizen : BaseRole
    {
        public override Team Team
        {
            get
            {
                return Team.Civil;
            }
        }
        
    }
}
