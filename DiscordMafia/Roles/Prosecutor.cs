namespace DiscordMafia.Roles
{
    public class Prosecutor : UniqueRole, ITargetedRole
    {
        public override string Name
        {
            get
            {
                return "Прокурор";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "прокурор",
                    "прокурора",
                    "прокурору",
                    "прокурора",
                    "прокурором",
                    "прокуроре",
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
            if (Player.Game.CurrentState == GameState.Night && (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget))
            {
                PlayerToInteract = null;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
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
