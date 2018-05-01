using DiscordMafia.Roles.Places;

namespace DiscordMafia.Roles
{
    public class Demoman : UniqueRole, IRoleWithCooldown
    {
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

        public int TotalVictims { get; set; } = 0;

        public int Cooldown => 2;

        public int CurrentCooldown { get; set; }

        public Demoman()
        {
            CurrentCooldown = Cooldown;
        }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null)
            {
                PlaceToDestroy = null;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void OnNightStart(Game game, InGamePlayerInfo currentPlayer)
        {
            this.DecreaseCooldown();
            if (!this.IsOnCooldown())
            {
                base.OnNightStart(game, currentPlayer);
                foreach (var player in game.PlayersList)
                {
                    if (player.IsAlive && player.Role.Team != Team.Mafia)
                    {
                        game.MessageBuilder.PrepareText("NightInfo_" + GetType().Name + "_ForPlayers").AddImage("roles/whereToGo.png").SendPrivate(player);
                    }
                }
            }
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlaceToDestroy == null && !this.IsOnCooldown())
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
