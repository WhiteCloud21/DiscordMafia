using DiscordMafia.Roles.Places;

namespace DiscordMafia.Roles
{
    public class Demoman : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Подрывник";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "подрывник",
                    "подрывника",
                    "подрывнику",
                    "подрывника",
                    "подрывником",
                    "подрывнике",
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

        private Place placeToDestroy = null;
        public Place PlaceToDestroy {
            get
            {
                return placeToDestroy;
            }
            set
            {
                if (Counter != 0 && value != null)
                {
                    throw new System.Exception("Взрывать можно только каждую третью ночь.");
                }
                placeToDestroy = value;
            }
        }
        public int Counter { get; protected set; }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null)
            {
                PlaceToDestroy = null;
                base.ClearActivity(cancel);
            }
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            Counter = (Counter + 1) % 3;
            if (Counter == 0)
            {
                base.NightInfo(game, currentPlayer);
                foreach (var player in game.playersList)
                {
                    if (player.isAlive && player.role.Team != Team.Mafia)
                    {
                        game.messageBuilder.PrepareText("NightInfo_" + GetType().Name + "_ForPlayers").SendPrivate(player);
                    }
                }
            }
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlaceToDestroy == null && Counter == 0)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
