using DiscordMafia.Base;
using System.Collections.Generic;

namespace DiscordMafia.Roles
{
    public class RabbleRouser : UniqueRole, IRoleWithCooldown
    {
        public override Team Team
        {
            get
            {
                return Team.Civil;
            }
        }

        private bool isCharged = false;
        public bool IsCharged
        {
            get
            {
                return isCharged;
            }
            set
            {
                if (this.IsOnCooldown() && value)
                {
                    throw new System.Exception(this.GetCooldownText(Player.Game));
                }
                isCharged = value;
            }
        }

        public int Cooldown => 4;

        public int CurrentCooldown { get; set; }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null)
            {
                IsCharged = false;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void OnDayStart(Game game, InGamePlayerInfo currentPlayer)
        {
            this.DecreaseCooldown();
            if (!this.IsOnCooldown())
            {
                base.OnDayStart(game, currentPlayer);
                game.MessageBuilder.Text(this.GetCooldownText(game), false).SendPrivate(currentPlayer);
            }
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Day:
                case GameState.Evening:
                    if (!IsCharged && !this.IsOnCooldown())
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
