namespace DiscordMafia.Roles
{
    public class Spy : UniqueRole, ITargetedRole
    {
        public override string Name
        {
            get
            {
                return "Шпион";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "шпион",
                    "шпиона",
                    "шпиону",
                    "шпиона",
                    "шпионом",
                    "шпионе",
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
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/пров");
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
