using DiscordMafia.Base;
using System.Collections.Generic;

namespace DiscordMafia.Roles
{
    public class Warlock : UniqueRole, ITargetedRole
    {
        public override Team Team
        {
            get
            {
                return Team.Neutral;
            }
        }

        public int AvailableCursesCount { get; set; }

        private InGamePlayerInfo playerToCurse = null;
        public InGamePlayerInfo PlayerToInteract
        {
            get
            {
                return playerToCurse;
            }
            set
            {
                if (AvailableCursesCount <= 0 && value != null)
                {
                    throw new System.Exception("Больше проклинать нельзя.");
                }
                playerToCurse = value;
            }
        }
        
        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget)
            {
                PlayerToInteract = null;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void OnNightStart(Game game, InGamePlayerInfo currentPlayer)
        {
            if (AvailableCursesCount > 0)
            {
                base.OnNightStart(game, currentPlayer);
                game.SendAlivePlayersMesssage(currentPlayer);
                game.MessageBuilder.PrepareText("ChargesRemaining", new Dictionary<string, object> { ["count"] = AvailableCursesCount }).SendPrivate(currentPlayer);
            }
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToInteract == null && AvailableCursesCount > 0)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
