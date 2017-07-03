using System;
using DiscordMafia.Voting;

namespace DiscordMafia.Activity
{
    public class BooleanVoteActivity : BaseActivity
    {
        public BooleanVote Vote { get; protected set; }
        public bool Value { get; protected set; }

        public BooleanVoteActivity(InGamePlayerInfo player, BooleanVote vote, bool value)
            : base(player)
        {
            Vote = vote;
            Value = value;
            Player.eveningVoteActivity = this;
        }

        protected override void OnCancel(InGamePlayerInfo onlyAgainstTarget)
        {
            if (onlyAgainstTarget == null)
            {
                if (Player != null)
                {
                    Player.eveningVoteActivity = null;
                    Vote.Remove(Player);
                }
                base.OnCancel(onlyAgainstTarget);
            }
        }
    }
}
