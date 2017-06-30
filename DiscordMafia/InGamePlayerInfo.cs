using System;
using System.Collections.Generic;
using DiscordMafia.Activity;
using DiscordMafia.Client;
using DiscordMafia.Items;
using DiscordMafia.Roles;
using DiscordMafia.Roles.Places;

namespace DiscordMafia
{
    public class InGamePlayerInfo
    {
        public UserWrapper user { get; private set; }
        public DB.User dbUser { get; private set; }
        public BaseRole role { get; set; }
        public BaseRole startRole { get; set; }
        public bool isAlive { get; set; }
        public bool isBot { get; set; }
        public long currentGamePoints { get; set; }
        public int? delayedDeath { get; set; }
        protected Game game { get; set; }
        protected List<BaseActivity> activityList { get; set; }
        public VoteActivity voteFor { get; set; }
        public BooleanVoteActivity eveningVoteActivity { get; set; }
        public HealActivity healedBy { get; set; }
        public JustifyActivity justifiedBy { get; set; }
        public Place placeToGo { get; set; }
        public List<BaseItem> ownedItems { get; set; }
        protected bool isTurnSkipped { get; set; }

        public InGamePlayerInfo(UserWrapper user, Game game)
        {
            this.user = user;
            this.isBot = false;
            this.isAlive = true;
            this.currentGamePoints = 0;
            this.game = game;
            this.activityList = new List<BaseActivity>();
            dbUser = DB.User.findById(user.Id);
            placeToGo = Place.AvailablePlaces[0];
            ownedItems = new List<BaseItem>();
        }

        public void AddPoints(string strategy)
        {
            var howMany = game.settings.Points.GetPoints(strategy);
            currentGamePoints += howMany;
        }

        public void AddPoints(long howMany)
        {
            currentGamePoints += howMany;
        }

        public void ActualizeDBUser()
        {
            dbUser.username = user.Username ?? "";
            dbUser.firstName = user.FirstName ?? "";
            dbUser.lastName = user.LastName ?? "";
            dbUser.RecalculateStats();
            dbUser.Save();
        }

        public string GetName()
        {
            return user.FirstName + " " + user.LastName;
        }

        public void AddActivity(BaseActivity activity)
        {
            activityList.Add(activity);
        }

        public void ClearActivity()
        {
            isTurnSkipped = false;
            role?.ClearActivity();
            foreach (var item in activityList)
            {
                item.Cancel();
            }
            activityList.Clear();
        }

        public void CancelActivity(InGamePlayerInfo onlyAgainstTarget = null)
        {
            role?.ClearActivity(true, onlyAgainstTarget);
            if (onlyAgainstTarget != null)
            {
                var itemsToRemove = new List<BaseActivity>();
                foreach (var item in activityList)
                {
                    item.Cancel(onlyAgainstTarget);
                    itemsToRemove.Add(item);
                }
                foreach (var item in itemsToRemove)
                {
                    activityList.Remove(item);
                }
            }
            else
            {
                isTurnSkipped = false;
                var itemsToRemove = new List<BaseActivity>();
                foreach (var item in activityList)
                {
                    // Дневное и вечернее голосование отменяется через CancelVote
                    if (game.currentState == GameState.Day && item == voteFor || game.currentState == GameState.Evening && item == eveningVoteActivity)
                    {
                        continue;
                    }
                    item.Cancel();
                    itemsToRemove.Add(item);
                }
                foreach (var item in itemsToRemove)
                {
                    activityList.Remove(item);
                }
            }
        }

        public bool CancelVote()
        {
            isTurnSkipped = false;
            BaseActivity voteActivity;
            switch (game.currentState)
            {
                case GameState.Day:
                    voteActivity = voteFor;
                    break;
                case GameState.Evening:
                    voteActivity = eveningVoteActivity;
                    break;
                default:
                    return false;
            }
            if (voteActivity != null)
            {
                voteActivity.Cancel();
                activityList.Remove(voteActivity);
                return true;
            }
            return false;
        }

        public bool HasActivityAgainst(InGamePlayerInfo target)
        {
            foreach (var item in activityList)
            {
                if (item.HasActivityAgainst(target))
                {
                    return true;
                }
            }
            return role != null ? role.HasActivityAgainst(target) : false;
        }

        public override bool Equals(object obj)
        {
            return this == obj as InGamePlayerInfo;
        }

        public override int GetHashCode()
        {
            return user.Id.GetHashCode();
        }

        public BaseItem GetItem(BaseItem item, bool onlyActive = false)
        {
            var result = ownedItems.Find(i => item.GetType() == i.GetType() && (i.IsActive || !onlyActive));
            return result;
        }

        public static bool operator ==(InGamePlayerInfo x, InGamePlayerInfo y)
        {
            if (x is InGamePlayerInfo && y is InGamePlayerInfo)
            {
                return x.user.Id == y.user.Id;
            }
            else if (x is InGamePlayerInfo || y is InGamePlayerInfo)
            {
                return false;
            }
            return true;
        }

        public static bool operator !=(InGamePlayerInfo x, InGamePlayerInfo y)
        {
            return !(x == y);
        }

        public void Buy(BaseItem itemToBuy)
        {
            if (GetItem(itemToBuy) != null)
            {
                throw new InvalidOperationException(itemToBuy.NameCases[3] + " можно покупать только один раз за игру.");
            }
            if (dbUser.totalPoints < itemToBuy.Cost)
            {
                throw new InvalidOperationException("Недостаточно очков для покупки " + itemToBuy.NameCases[1]);
            }
            ownedItems.Add(itemToBuy);
            AddPoints(-itemToBuy.Cost);
        }

        public bool IsReady(GameState currentState)
        {
            return isTurnSkipped || (role?.IsReady(currentState) ?? false);
        }

        public bool SkipTurn(bool value = true)
        {
            if (isTurnSkipped == value)
            {
                return false;
            }
            isTurnSkipped = true;
            return true;
        }
    }
}
