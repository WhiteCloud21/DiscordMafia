namespace DiscordMafia.Roles
{
    public class Elder : UniqueRole, ITargetedRole
    {
        public override string Name
        {
            get
            {
                return "Старейшина";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "старейшина",
                    "старейшины",
                    "старейшине",
                    "старейшину",
                    "старейшиной",
                    "старейшине",
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

        private InGamePlayerInfo playerToKill;
        public InGamePlayerInfo PlayerToInteract
        {
            get { return playerToKill; }
            set {
                if (value == Player)
                {
                    throw new System.ArgumentException("Нельзя повесить себя.");
                }
                playerToKill = value;
            }
        }

        public bool WasAttacked { get; set; }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget)
            {
                PlayerToInteract = null;
                WasAttacked = false;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void DayInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.DayInfo(game, currentPlayer);
            game.SendAlivePlayersMesssage(currentPlayer);
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Evening:
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
