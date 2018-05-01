using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordMafia.Roles
{
    public interface IRoleWithCooldown
    {
        int Cooldown { get; }
        int CurrentCooldown { get; set; }
    }
    
    public static class IRoleWithCooldownExtensions
    {
        public static void PutOnCooldown<T>(this T role) where T : IRoleWithCooldown
        {
            role.CurrentCooldown = role.Cooldown;
        }

        public static void DecreaseCooldown<T>(this T role) where T : IRoleWithCooldown
        {
            role.CurrentCooldown = Math.Max(role.CurrentCooldown - 1, 0);
        }

        public static bool IsOnCooldown<T>(this T role) where T : IRoleWithCooldown
        {
            return role.CurrentCooldown != 0;
        }

        public static string GetCooldownText<T>(this T role, Game game) where T : IRoleWithCooldown
        {
            return game.MessageBuilder.GetText("CooldownInfo", new Dictionary<string, object> { ["current"] = role.CurrentCooldown, ["total"] = role.Cooldown });
        }
    }
}
