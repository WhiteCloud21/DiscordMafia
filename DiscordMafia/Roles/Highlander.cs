namespace DiscordMafia.Roles
{
    public class Highlander : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Горец";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "горец",
                    "горца",
                    "горцу",
                    "горца",
                    "горцем",
                    "горце",
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
                    throw new System.ArgumentException("Нельзя подстрелить себя.");
                }
                playerToKill = value;
            }
        }

        public bool WasAttacked { get; set; }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToKill == onlyAgainstTarget)
            {
                PlayerToKill = null;
                WasAttacked = false;
                base.ClearActivity(cancel);
            }
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.NightInfo(game, currentPlayer);
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/убить");
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
