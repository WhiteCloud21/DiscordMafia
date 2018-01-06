using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordMafia.Roles
{
    public interface ITargetedRole
    {
        InGamePlayerInfo PlayerToInteract { get; set; }
    }

    public static class ITargetedRoleExtensions
    {
        public static void PerformNightAction<T>(this T role, InGamePlayerInfo target) where T: BaseRole, ITargetedRole
        {
            if (role.PlayerToInteract == null)
            {
                try
                {
                    role.PlayerToInteract = target;
                    role.Player.Game.NightAction(role);
                    role.Player.Game.MessageBuilder.PrepareText("OK").SendPrivate(role.Player);
                    role.Player.Game.CheckNextCheckpoint();
                }
                catch (Exception ex)
                {
                    role.Player.Game.MessageBuilder.Text(ex.Message, false).SendPrivate(role.Player);
                }
            }
        }
    }
}
