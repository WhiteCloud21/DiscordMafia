using System;
using System.Collections.Generic;
using DiscordMafia.Activity;

namespace DiscordMafia.Voting
{
    public class BooleanVote
    {
        protected Dictionary<ulong, BooleanVoteActivity> Votes = new Dictionary<ulong, BooleanVoteActivity>();

        /// <summary>
        /// Был ли хоть один проголосовавший (даже отменивший голос)
        /// </summary>
        public bool HasVotes { get; private set; }

        public BooleanVote()
        {
            HasVotes = false;
        }

        public BooleanVoteResult GetResult(bool useWeght = false)
        {
            return new BooleanVoteResult(Votes, useWeght);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voter"></param>
        /// <param name="forWho"></param>
        /// <param name="weight"></param>
        /// <param name="weightType"></param>
        /// <exception cref="ArgumentException">Если голосовавший уже голосовал</exception>
        public void Add(InGamePlayerInfo voter, bool value, int weight, EWeightType weightType)
        {
            HasVotes = true;
            var activity = new BooleanVoteActivity(voter, this, value, weight, weightType);
            Votes.Add(voter.User.Id, activity);
            voter.AddActivity(activity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voter"></param>
        public void Remove(InGamePlayerInfo voter)
        {
            if (Votes.ContainsKey(voter.User.Id))
            {
                var activity = Votes[voter.User.Id];
                Votes.Remove(voter.User.Id);
                activity.Cancel();
            }
        }
    }
}
