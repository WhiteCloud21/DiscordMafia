namespace DiscordMafia.Roles
{
    public class Warlock : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Чернокнижник";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "чернокнижник",
                    "чернокнижника",
                    "чернокнижнику",
                    "чернокнижника",
                    "чернокнижником",
                    "чернокнижнике",
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

        public int AvailableCursesCount { get; set; }

        private InGamePlayerInfo playerToCurse = null;
        public InGamePlayerInfo PlayerToCurse
        {
            get
            {
                return playerToCurse;
            }
            set
            {
                if (AvailableCursesCount <= 0 && value != null)
                {
                    throw new System.Exception("Больше проклинать нельзя.");
                }
                playerToCurse = value;
            }
        }
        
        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToCurse == onlyAgainstTarget)
            {
                PlayerToCurse = null;
                base.ClearActivity(cancel);
            }
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            if (AvailableCursesCount > 0)
            {
                base.NightInfo(game, currentPlayer);
                game.GetAlivePlayersMesssage(true, true, currentPlayer, "/curse");
                game.messageBuilder.Text($"Осталось проклятий: {AvailableCursesCount}").SendPrivate(currentPlayer);
            }
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToCurse == null && AvailableCursesCount > 0)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }

        public override bool HasActivityAgainst(InGamePlayerInfo target)
        {
            return target == PlayerToCurse;
        }
    }
}
