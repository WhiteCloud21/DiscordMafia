namespace DiscordMafia.Roles
{
    public class Elder : UniqueRole
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
        public InGamePlayerInfo PlayerToKill
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

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToKill == onlyAgainstTarget)
            {
                PlayerToKill = null;
                base.ClearActivity(cancel);
            }
        }

        public override void DayInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.DayInfo(game, currentPlayer);
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/посадить");
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Evening:
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
