using System;

namespace DiscordMafia.Roles
{
    public abstract class BaseRole
    {
        public abstract string Name { get; }
        public abstract string[] NameCases { get; }
        public abstract Team Team { get; }

        public InGamePlayerInfo Player { get; internal set; }

        public virtual void ClearActivity(bool cancel = false, InGamePlayerInfo onlyAgainstTarget = null)
        {

        }

        public virtual void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            game.messageBuilder.PrepareText("NightInfo_" + this.GetType().Name).SendPrivate(currentPlayer);
        }

        public virtual void DayInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            game.messageBuilder.PrepareText("DayInfo_" + this.GetType().Name).SendPrivate(currentPlayer);
        }

        public virtual bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    return true;
                case GameState.Day:
                    if (Player.voteFor == null)
                    {
                        return false;
                    }
                    return true;
                case GameState.Evening:
                    return false;
                default:
                    return false;
            }
        }
    }

    public enum Team
    {
        None = 0,
        Civil = 1,
        Neutral = 2,
        Mafia = 200,
        Yakuza = 201,
    }
}
