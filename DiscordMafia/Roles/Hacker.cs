namespace DiscordMafia.Roles
{
    public class Hacker : UniqueRole, ITargetedRole
    {
        public override string Name
        {
            get
            {
                return "Хакер";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "хакер",
                    "хакера",
                    "хакеру",
                    "хакера",
                    "хакером",
                    "хакере",
                };
            }
        }

        public override Team Team
        {
            get
            {
                return Team.Neutral;
            }
        }
        
        public InGamePlayerInfo PlayerToInteract
        {
            get; set;
        }
        
        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget)
            {
                PlayerToInteract = null;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
                base.NightInfo(game, currentPlayer);
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
