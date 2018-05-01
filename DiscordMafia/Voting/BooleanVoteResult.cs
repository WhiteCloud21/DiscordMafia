using System;
using System.Collections.Generic;
using DiscordMafia.Activity;

namespace DiscordMafia.Voting
{
    public class BooleanVoteResult
    {
        public IDictionary<ulong, BooleanVoteActivity> DetailedInfo
        {
            get;
            private set;
        }

        public int YesCount
        {
            get;
            private set;
        }

        public int NoCount
        {
            get;
            private set;
        }

        public bool IsEmpty
        {
            get
            {
                return (YesCount + NoCount) == 0;
            }
        }

        public bool? Result
        {
            get
            {
                return (YesCount == NoCount) ? (bool?)null : (YesCount > NoCount);
            }
        }

        public BooleanVoteResult(IDictionary<ulong, BooleanVoteActivity> detailedInfo, bool useWeght = false)
        {
            DetailedInfo = detailedInfo;
            YesCount = NoCount = 0;
            FillResult(useWeght);
        }

        private void FillResult(bool useWeght)
        {
            foreach (var vote in DetailedInfo.Values)
            {
                if (vote.Value)
                {
                    YesCount += vote.WeightType.HasFlag(EWeightType.Positive) && useWeght ? vote.Weight : 1;
                }
                else
                {
                    NoCount += vote.WeightType.HasFlag(EWeightType.Negative) && useWeght ? vote.Weight : 1;
                }
            }
        }

        public bool IsVotedYes(InGamePlayerInfo voter)
        {
            if (Result.HasValue && Result.Value && DetailedInfo.ContainsKey(voter.User.Id))
            {
                return DetailedInfo[voter.User.Id].Value;
            }
            return false;
        }

        public bool IsVoted(InGamePlayerInfo voter)
        {
            return DetailedInfo.ContainsKey(voter.User.Id);
        }
    }
}
