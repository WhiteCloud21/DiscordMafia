using DiscordMafia.Activity;

namespace DiscordMafia.Roles
{
    public class Hammerer : UniqueRole
    {
        public override Team Team
        {
            get
            {
                return Team.Mafia;
            }
        }

        public override int EveningVoteWeight => 2;
        public override EWeightType EveningVoteWeightType => EWeightType.Positive;
    }
}
