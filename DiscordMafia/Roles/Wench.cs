namespace DiscordMafia.Roles
{
    public class Wench : UniqueRole, ITargetedRole
    {
        public override string Name
        {
            get
            {
                return "Путана";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "путана",
                    "путаны",
                    "путане",
                    "путану",
                    "путаной",
                    "путане",
                };
            }
        }

        public override Team Team
        {
            get
            {
                return Team.Civil;
            }
        }

        private InGamePlayerInfo playerToCheck;
        public InGamePlayerInfo PlayerToInteract
        {
            get { return playerToCheck; }
            set {
                if (value != null && value == LastPlayerToCheck)
                {
                    throw new System.ArgumentException("Нельзя ходить к одному игроку 2 раза подряд.");
                }
                if (value == Player)
                {
                    throw new System.ArgumentException("Нельзя ходить к себе.");
                }
                playerToCheck = value;
            }
        }
        public InGamePlayerInfo LastPlayerToCheck { get; set; }
        
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
