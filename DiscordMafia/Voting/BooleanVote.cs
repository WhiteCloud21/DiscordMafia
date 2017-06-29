using System.Collections.Generic;
using DiscordMafia.Activity;

namespace DiscordMafia.Voting
{
    public class BooleanVote
    {
        protected Dictionary<ulong, BooleanVoteActivity> votes = new Dictionary<ulong, BooleanVoteActivity>();

        /// <summary>
        /// Был ли хоть один проголосовавший (даже отменивший голос)
        /// </summary>
        public bool HasVotes { get; private set; }

        public BooleanVote()
        {
            HasVotes = false;
        }

        public BooleanVoteResult GetResult()
        {
            return new BooleanVoteResult(votes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voter"></param>
        /// <param name="forWho"></param>
        /// <exception cref="ArgumentException">Если голосовавший уже голосовал</exception>
        public void Add(InGamePlayerInfo voter, bool value)
        {
            HasVotes = true;
            var activity = new BooleanVoteActivity(voter, this, value);
            votes.Add(voter.user.Id, activity);
            voter.AddActivity(activity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voter"></param>
        public void Remove(InGamePlayerInfo voter)
        {
            if (votes.ContainsKey(voter.user.Id))
            {
                var activity = votes[voter.user.Id];
                votes.Remove(voter.user.Id);
                activity.Cancel();
            }
        }
    }
}
