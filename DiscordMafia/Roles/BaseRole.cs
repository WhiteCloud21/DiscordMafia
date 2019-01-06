using DiscordMafia.Activity;
using DiscordMafia.Base;
using System.ComponentModel;

namespace DiscordMafia.Roles
{
    public abstract class BaseRole
    {
        public abstract Team Team { get; }

        public InGamePlayerInfo Player { get; internal set; }

        public virtual int EveningVoteWeight { get; } = 1;

        public virtual EWeightType EveningVoteWeightType { get; } = EWeightType.None;

        public BaseRole()
        {
        }

        public virtual void ClearActivity(bool cancel = false, InGamePlayerInfo onlyAgainstTarget = null)
        {

        }

        public virtual void OnNightStart(Game game, InGamePlayerInfo currentPlayer)
        {
            game.MessageBuilder.PrepareText("NightInfo_" + this.GetType().Name).SendPrivate(currentPlayer);
        }

        public virtual void OnDayStart(Game game, InGamePlayerInfo currentPlayer)
        {
            game.MessageBuilder.PrepareText("DayInfo_" + this.GetType().Name).SendPrivate(currentPlayer);
        }

        public virtual bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    return true;
                case GameState.Day:
                    if (Player.VoteFor == null)
                    {
                        return false;
                    }
                    return true;
                case GameState.Evening:
                    if (Player.EveningVoteActivity == null)
                    {
                        return false;
                    }
                    return true;
                default:
                    return false;
            }
        }

        public virtual bool HasActivityAgainst(InGamePlayerInfo target)
        {
            if (this is ITargetedRole directedRole)
            {
                return target == directedRole.PlayerToInteract;
            }
            return false;
        }
    }

    public enum Team
    {
        [Description("Нет")]
        None = 0,
        [Description("Мирные")]
        Civil = 1,
        [Description("Нейтральный")]
        Neutral = 2,
        [Description("Мафия")]
        Mafia = 200,
        [Description("Якудза")]
        Yakuza = 201,
    }
}
