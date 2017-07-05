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
            Player.EveningVoteActivity = this;
        }

        protected override void OnCancel(InGamePlayerInfo onlyAgainstTarget)
        {
            if (onlyAgainstTarget == null)
            {
                if (Player != null)
                {
                    Player.EveningVoteActivity = null;
                    Vote.Remove(Player);
                }
                base.OnCancel(onlyAgainstTarget);
            }
        }
    }
}
