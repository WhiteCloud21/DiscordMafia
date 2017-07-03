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
            Accused.JustifiedBy = this;
        }

        protected override void OnCancel(InGamePlayerInfo onlyAgainstTarget)
        {
            if (onlyAgainstTarget == null || Accused == onlyAgainstTarget)
            {
                if (Accused != null)
                {
                    Accused.JustifiedBy = null;
                }
                IsCanceled = true;
                base.OnCancel(onlyAgainstTarget);
            }
        }

        public override bool HasActivityAgainst(InGamePlayerInfo target)
        {
            return target == Accused;
        }
    }
}
