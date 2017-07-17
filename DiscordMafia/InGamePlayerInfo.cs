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
        public UserWrapper User { get; private set; }
        public DB.User DbUser { get; private set; }
        public BaseRole Role { get; set; }
        public BaseRole StartRole { get; set; }
        public bool IsAlive { get; set; }
        public bool IsBot { get; set; }
        public long CurrentGamePoints { get; set; }
        public int? DelayedDeath { get; set; }
        public Game Game { get; protected set; }
        protected List<BaseActivity> ActivityList { get; set; }
        public VoteActivity VoteFor { get; set; }
        public BooleanVoteActivity EveningVoteActivity { get; set; }
        public HealActivity HealedBy { get; set; }
        public JustifyActivity JustifiedBy { get; set; }
        public Place PlaceToGo { get; set; }
        public List<BaseItem> OwnedItems { get; set; }
        protected bool IsTurnSkipped { get; set; }

        public InGamePlayerInfo(UserWrapper user, Game game)
        {
            this.User = user;
            this.IsBot = false;
            this.IsAlive = true;
            this.CurrentGamePoints = 0;
            this.Game = game;
            this.ActivityList = new List<BaseActivity>();
            DbUser = DB.User.FindById(user.Id);
            PlaceToGo = Place.AvailablePlaces[0];
            OwnedItems = new List<BaseItem>();
        }

        public void AddPoints(string strategy)
        {
            var howMany = Game.Settings.Points.GetPoints(strategy);
            CurrentGamePoints += howMany;
        }

        public void AddPoints(long howMany)
        {
            CurrentGamePoints += howMany;
        }

        public void ActualizeDbUser()
        {
            DbUser.Username = User.Username ?? "";
            DbUser.FirstName = User.FirstName ?? "";
            DbUser.LastName = User.LastName ?? "";
            DbUser.RecalculateStats();
        }

        public string GetName()
        {
            return User.FirstName + " " + User.LastName;
        }

        public void AddActivity(BaseActivity activity)
        {
            ActivityList.Add(activity);
        }

        public void ClearActivity()
        {
            IsTurnSkipped = false;
            Role?.ClearActivity();
            foreach (var item in ActivityList)
            {
                item.Cancel();
            }
            ActivityList.Clear();
        }

        public void CancelActivity(InGamePlayerInfo onlyAgainstTarget = null)
        {
            Role?.ClearActivity(true, onlyAgainstTarget);
            if (onlyAgainstTarget != null)
            {
                var itemsToRemove = new List<BaseActivity>();
                foreach (var item in ActivityList)
                {
                    item.Cancel(onlyAgainstTarget);
                    if (item.IsCanceled)
                    {
                        itemsToRemove.Add(item);
                    }
                }
                foreach (var item in itemsToRemove)
                {
                    ActivityList.Remove(item);
                }
            }
            else
            {
                IsTurnSkipped = false;
                var itemsToRemove = new List<BaseActivity>();
                foreach (var item in ActivityList)
                {
                    // Дневное и вечернее голосование отменяется через CancelVote
                    if (Game.CurrentState == GameState.Day && item == VoteFor || Game.CurrentState == GameState.Evening && item == EveningVoteActivity)
                    {
                        continue;
                    }
                    item.Cancel();
                    itemsToRemove.Add(item);
                }
                foreach (var item in itemsToRemove)
                {
                    ActivityList.Remove(item);
                }
            }
        }

        public bool CancelVote()
        {
            IsTurnSkipped = false;
            BaseActivity voteActivity;
            switch (Game.CurrentState)
            {
                case GameState.Day:
                    voteActivity = VoteFor;
                    break;
                case GameState.Evening:
                    voteActivity = EveningVoteActivity;
                    break;
                default:
                    return false;
            }
            if (voteActivity != null)
            {
                voteActivity.Cancel();
                ActivityList.Remove(voteActivity);
                return true;
            }
            return false;
        }

        public bool HasActivityAgainst(InGamePlayerInfo target)
        {
            foreach (var item in ActivityList)
            {
                if (item.HasActivityAgainst(target))
                {
                    return true;
                }
            }
            return Role != null ? Role.HasActivityAgainst(target) : false;
        }

        public override bool Equals(object obj)
        {
            return this == obj as InGamePlayerInfo;
        }

        public override int GetHashCode()
        {
            return User.Id.GetHashCode();
        }

        public BaseItem GetItem(BaseItem item, bool onlyActive = false)
        {
            var result = OwnedItems.Find(i => item.GetType() == i.GetType() && (i.IsActive || !onlyActive));
            return result;
        }

        public static bool operator ==(InGamePlayerInfo x, InGamePlayerInfo y)
        {
            if (x is InGamePlayerInfo && y is InGamePlayerInfo)
            {
                return x.User.Id == y.User.Id;
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
            if (DbUser.TotalPoints < itemToBuy.Cost)
            {
                throw new InvalidOperationException("Недостаточно очков для покупки " + itemToBuy.NameCases[1]);
            }
            OwnedItems.Add(itemToBuy);
            AddPoints(-itemToBuy.Cost);
        }

        public bool IsReady(GameState currentState)
        {
            return IsTurnSkipped || (Role?.IsReady(currentState) ?? false);
        }

        public bool SkipTurn(bool value = true)
        {
            if (IsTurnSkipped == value)
            {
                return false;
            }
            IsTurnSkipped = true;
            return true;
        }
    }
}
