using DiscordMafia.Base;

namespace DiscordMafia.Roles
{
    public class Kamikaze : UniqueRole, ITargetedRole
    {
        public override Team Team
        {
            get
            {
                return Team.Yakuza;
            }
        }

        public InGamePlayerInfo PlayerToInteract { get; set; }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            var nightCancel = cancel && Player.Game.CurrentState == GameState.Night && (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget);
            var eveningClear = !cancel && Player.Game.CurrentState == GameState.Evening && (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget);
            if (nightCancel || eveningClear)
            {
                PlayerToInteract = null;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void OnNightStart(Game game, InGamePlayerInfo currentPlayer)
        {
            base.OnNightStart(game, currentPlayer);
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
