using System;
using DiscordMafia.Voting;

namespace DiscordMafia.Activity
{
    public class JustifyActivity: BaseActivity
    {
        public InGamePlayerInfo Accused { get; protected set; }

        public JustifyActivity(InGamePlayerInfo judge, InGamePlayerInfo accused)
            : base(judge)
        {
            Accused = accused;
            Accused.justifiedBy = this;
        }

        protected override void OnCancel(InGamePlayerInfo onlyAgainstTarget)
        {
            if (onlyAgainstTarget == null || Accused == onlyAgainstTarget)
            {
                if (Accused != null)
                {
                    Accused.justifiedBy = null;
                }
                base.OnCancel(onlyAgainstTarget);
            }
        }
    }
}
