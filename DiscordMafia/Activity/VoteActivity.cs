using System;
using DiscordMafia.Voting;

namespace DiscordMafia.Activity
{
    public class VoteActivity: BaseActivity
    {
        public Vote Vote { get; protected set; }
        public InGamePlayerInfo ForWho { get; protected set; }

        public VoteActivity(InGamePlayerInfo player, Vote vote, InGamePlayerInfo forWho)
            : base(player)
        {
            Vote = vote;
            ForWho = forWho;
            Player.voteFor = this;
        }

        protected override void OnCancel(InGamePlayerInfo onlyAgainstTarget)
        {
            if (onlyAgainstTarget == null || ForWho == onlyAgainstTarget)
            {
                if (Player != null)
                {
                    Player.voteFor = null;
                    Vote.Remove(Player);
                }
                IsCanceled = true;
                base.OnCancel(onlyAgainstTarget);
            }
        }

        public override bool HasActivityAgainst(InGamePlayerInfo target)
        {
            return target == ForWho;
        }
    }
}
