using System.Collections.Generic;
using DiscordMafia.Activity;

namespace DiscordMafia.Voting
{
    public class Vote
    {
        protected Dictionary<ulong, VoteActivity> Votes = new Dictionary<ulong, VoteActivity>();

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
            return new VoteResult(Votes);
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
