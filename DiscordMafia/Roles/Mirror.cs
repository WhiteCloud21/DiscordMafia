namespace DiscordMafia.Roles
{
    public class Mirror : UniqueRole
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
