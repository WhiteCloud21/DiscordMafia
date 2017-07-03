using System;
using System.Collections.Generic;
using DiscordMafia.Activity;

namespace DiscordMafia.Voting
{
    public class VoteResult
    {
        public ulong? Leader
        {
            get;
            private set;
        }

        public int LeaderResult
        {
            get;
            private set;
        }

        public IDictionary<ulong, VoteActivity> DetailedInfo
        {
            get;
            private set;
        }

        public IDictionary<ulong, int> VoteCountByPlayer
        {
            get;
            private set;
        }

        public bool IsEmpty
        {
            get
            {
                return LeaderResult == 0;
            }
        }

        public bool HasOneLeader
        {
            get
            {
                return Leader != null;
            }
        }

        public VoteResult(IDictionary<ulong, VoteActivity> detailedInfo)
        {
            this.DetailedInfo = detailedInfo;
            fillVoteCountByPlayer();
            fillLeader();
        }

        private void fillVoteCountByPlayer()
        {
            VoteCountByPlayer = new Dictionary<ulong, int>();
            foreach (var vote in DetailedInfo.Values)
            {
                if (!VoteCountByPlayer.ContainsKey(vote.ForWho.User.Id))
                {
                    VoteCountByPlayer.Add(vote.ForWho.User.Id, 0);
                }
                VoteCountByPlayer[vote.ForWho.User.Id]++;
            }
        }

        protected void fillLeader()
        {
            foreach (var playerVotes in VoteCountByPlayer)
            {
                if (LeaderResult < playerVotes.Value)
                {
                    Leader = playerVotes.Key;
                }
                else if (LeaderResult == playerVotes.Value)
                {
                    Leader = null;
                }
                LeaderResult = Math.Max(LeaderResult, playerVotes.Value);
            }
        }
        
        public InGamePlayerInfo GetTarget(InGamePlayerInfo voter)
        {
            if (DetailedInfo.ContainsKey(voter.User.Id))
            {
                return DetailedInfo[voter.User.Id].ForWho;
            }
            return null;
        }

        public bool IsVotedForLeader(InGamePlayerInfo voter)
        {
            if (HasOneLeader && DetailedInfo.ContainsKey(voter.User.Id))
            {
                return DetailedInfo[voter.User.Id].ForWho.User.Id == Leader;
            }
            return false;
        }
    }
}
