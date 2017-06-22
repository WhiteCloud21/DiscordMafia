namespace DiscordMafia.Roles
{
    public class Lawyer : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Адвокат";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "адвокат",
                    "адвоката",
                    "адвокату",
                    "адвоката",
                    "адвокатом",
                    "адвокате",
                };
            }
        }

        public override Team Team
        {
            get
            {
                return Team.Mafia;
            }
        }

        public InGamePlayerInfo PlayerToCheck { get; set; }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToCheck == onlyAgainstTarget)
            {
                PlayerToCheck = null;
                base.ClearActivity(cancel);
            }
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.NightInfo(game, currentPlayer);
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/пров");
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToCheck == null)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }

        public override bool HasActivityAgainst(InGamePlayerInfo target)
        {
            return target == PlayerToCheck;
        }
    }
}
