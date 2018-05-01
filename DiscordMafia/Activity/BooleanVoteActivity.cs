using System;
using DiscordMafia.Voting;

namespace DiscordMafia.Activity
{
    public class BooleanVoteActivity : BaseActivity
    {
        public BooleanVote Vote { get; protected set; }
        public bool Value { get; protected set; }
        public int Weight { get; protected set; }
        public EWeightType WeightType { get; protected set; }

        public BooleanVoteActivity(InGamePlayerInfo player, BooleanVote vote, bool value, int weight, EWeightType weightType)
            : base(player)
        {
            Vote = vote;
            Value = value;
            Weight = weight;
            WeightType = weightType;
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

    [Flags]
    public enum EWeightType
    {
        None = 0,
        Positive = 1,
        Negative = 2,
        Both = 3
    }
}
