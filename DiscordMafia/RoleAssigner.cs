
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
        private int remainingMafCount = 0;
        private int totalMafCount = 0;

        public void AssignRoles(IList<InGamePlayerInfo> players, Config.GameSettings settings)
        {
            this.players = players;
            remainingPlayerCount = players.Count;
            totalMafCount = remainingMafCount = (int)Math.Truncate(players.Count * (double)settings.MafPercent / 100);
            Console.WriteLine("Количество мафиози: {0}", remainingMafCount);

            foreach (var player in players)
            {
                player.startRole = player.role = new Citizen() { Player = player };
            }

            foreach (var role in settings.Roles.getTemporaryRoles(players.Count))
            {
                AssignRoleToRandomPlayer(role);
            }

            foreach (var role in settings.Roles.getRandomRoles(players.Count, settings.MaxRandomPlayers, random))
            {
                AssignRoleToRandomPlayer(role);
            }

            while (remainingMafCount > 0 && remainingPlayerCount > 0)
            {
                AssignRoleToRandomPlayer(new Mafioso());
            }
        }

        private void AssignRoleToRandomPlayer(BaseRole role)
        {
            if (remainingPlayerCount <= remainingMafCount && role.Team != Team.Mafia || remainingPlayerCount == 0)
            {
                Console.WriteLine("Недостаточно игроков для назначения роли {0}.", role.Name);
                return;
            }
            if (role is Warlock)
            {
                (role as Warlock).AvailableCursesCount = totalMafCount;
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
            if (role.Team == Team.Mafia)
            {
                remainingMafCount--;
            }
        }
    }
}
