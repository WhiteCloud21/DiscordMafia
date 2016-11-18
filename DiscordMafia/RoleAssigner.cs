
using System;
using System.Collections.Generic;
using DiscordMafia.Roles;

namespace DiscordMafia
{
    public class RoleAssigner
    {
        private Random random = new Random();
        IList<InGamePlayerInfo> players;
        private int remainingPlayerCount = 0;

        public void AssignRoles(IList<InGamePlayerInfo> players, Config.GameSettings settings)
        {
            this.players = players;
            remainingPlayerCount = players.Count;
            var mafCount = Math.Truncate(players.Count * (double)settings.MafPercent / 100);
            Console.WriteLine("Количество мафиози: {0}", mafCount);

            foreach (var player in players)
            {
                player.startRole = player.role = new Citizen() { Player = player };
            }

            for (int i = 0; i < mafCount; i++)
            {
                AssignRoleToRandomPlayer(new Mafioso());
            }
            foreach (var role in settings.Roles.getTemporaryRoles(players.Count))
            {
                AssignRoleToRandomPlayer(role);
            }
            foreach (var role in settings.Roles.getRandomRoles(players.Count, settings.MaxRandomPlayers, random))
            {
                AssignRoleToRandomPlayer(role);
            }
        }

        private void AssignRoleToRandomPlayer(BaseRole role)
        {
            if (remainingPlayerCount == 0)
            {
                Console.WriteLine("Недостаточно игроков для назначения роли {0}.", role.Name);
                return;
            }
            var playerIndex = random.Next(remainingPlayerCount);
            Console.WriteLine("Assined: {0} {1}", playerIndex, role.Name);
            foreach (var player in players)
            {
                if (player.role is Citizen)
                {
                    if (playerIndex-- <= 0)
                    {
                        player.startRole = player.role = role;
                        role.Player = player;
                        break;
                    }
                }
            }
            remainingPlayerCount--;
        }
    }
}
