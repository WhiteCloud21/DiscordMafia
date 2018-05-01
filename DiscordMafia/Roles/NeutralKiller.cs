namespace DiscordMafia.Roles
{
    public abstract class NeutralKiller : UniqueRole, ITargetedRole
    {
        public override Team Team
        {
            get
            {
                return Team.Neutral;
            }
        }

        public InGamePlayerInfo PlayerToInteract { get; set; }
        
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
            base.OnNightStart(game, currentPlayer);
            game.SendAlivePlayersMesssage(currentPlayer);
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToInteract == null)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
