namespace DiscordMafia.Roles
{
    public class Lawyer : UniqueRole, ITargetedRole
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

        public InGamePlayerInfo PlayerToInteract { get; set; }

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
