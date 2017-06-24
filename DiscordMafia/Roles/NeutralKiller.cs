namespace DiscordMafia.Roles
{
    public abstract class NeutralKiller : UniqueRole
    {
        public override Team Team
        {
            get
            {
                return Team.Neutral;
            }
        }

        public InGamePlayerInfo PlayerToKill { get; set; }
        
        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToKill == onlyAgainstTarget)
            {
                PlayerToKill = null;
                base.ClearActivity(cancel);
            }
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.NightInfo(game, currentPlayer);
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/kill");
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToKill == null)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }

        public override bool HasActivityAgainst(InGamePlayerInfo target)
        {
            return target == PlayerToKill;
        }
    }
}
