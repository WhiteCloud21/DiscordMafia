using System.Collections.Generic;
using DiscordMafia.Activity;

namespace DiscordMafia.Voting
{
    public class Vote
    {
        protected Dictionary<ulong, VoteActivity> votes = new Dictionary<ulong, VoteActivity>();

        /// <summary>
        /// Был ли хоть один проголосовавший (даже отменивший голос)
        /// </summary>
        public bool HasVotes { get; private set; }

        public Vote()
        {
            HasVotes = false;
        }

        public VoteResult GetResult()
        {
            return new VoteResult(votes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voter"></param>
        /// <param name="forWho"></param>
        /// <exception cref="ArgumentException">Если голосовавший уже голосовал</exception>
        public void Add(InGamePlayerInfo voter, InGamePlayerInfo forWho)
        {
            HasVotes = true;
            var activity = new VoteActivity(voter, this, forWho);
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
