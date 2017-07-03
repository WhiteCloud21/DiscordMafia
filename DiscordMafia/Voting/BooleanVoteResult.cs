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

        public BooleanVoteResult(IDictionary<ulong, BooleanVoteActivity> detailedInfo)
        {
            this.DetailedInfo = detailedInfo;
            YesCount = NoCount = 0;
            fillResult();
        }

        private void fillResult()
        {
            foreach (var vote in DetailedInfo.Values)
            {
                if (vote.Value)
                {
                    YesCount++;
                }
                else
                {
                    NoCount++;
                }
            }
        }

        public bool IsVotedYes(InGamePlayerInfo voter)
        {
            if (Result.HasValue && Result.Value && DetailedInfo.ContainsKey(voter.user.Id))
            {
                return DetailedInfo[voter.user.Id].Value;
            }
            return false;
        }
    }
}
