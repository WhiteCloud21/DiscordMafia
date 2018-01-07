namespace DiscordMafia.Roles
{
    public class Sergeant : UniqueRole
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
