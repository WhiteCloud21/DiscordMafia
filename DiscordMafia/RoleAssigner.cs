
using System;
using System.Collections.Generic;
using System.Linq;
using DiscordMafia.Roles;

namespace DiscordMafia
{
    public class RoleAssigner
    {
        private Random random = new Random();
        private int remainingPlayerCount = 0;
        private int remainingMafCount = 0;
        private int remainingYakuzaCount = 0;
        private int totalMafCount = 0;

        public void AssignRoles(IEnumerable<InGamePlayerInfo> players, Config.GameSettings settings)
        {
            var totalPlayersCount = players.Count();
            remainingPlayerCount = totalPlayersCount;
            totalMafCount = (int)Math.Truncate(totalPlayersCount * (double)settings.MafPercent / 100);
            remainingMafCount = settings.IsMafiaEnabled ? totalMafCount : 0;
            remainingYakuzaCount = settings.IsYakuzaEnabled ? totalMafCount : 0;
            Console.WriteLine("Количество мафиози: {0}", remainingMafCount);

            foreach (var player in players)
            {
                player.StartRole = player.Role = new Citizen() { Player = player };
            }

            foreach (var role in settings.Roles.GetTemporaryRoles(totalPlayersCount))
            {
                AssignRoleToRandomPlayer(players, role);
            }

            foreach (var role in settings.Roles.GetRandomRoles(totalPlayersCount, settings.MaxRandomPlayers, random))
            {
                AssignRoleToRandomPlayer(players, role);
            }

            while ((remainingMafCount > 0 || remainingYakuzaCount > 0) && remainingPlayerCount > 0)
            {
                if (remainingMafCount > 0)
                {
                    AssignRoleToRandomPlayer(players, new Mafioso());
                }
                if (remainingYakuzaCount > 0)
                {
                    AssignRoleToRandomPlayer(players, new Yakuza());
                }
            }
        }

        public void ReassignRoles(IEnumerable<InGamePlayerInfo> players)
        {
            remainingPlayerCount = 0;
            remainingYakuzaCount = 0;
            remainingMafCount = 0;
            var rolesToReassign = new List<BaseRole>();
            var playersToReassign = new List<InGamePlayerInfo>();
            foreach (var player in players)
            {
                if (player.IsAlive)
                {
                    if (!(player.Role is Citizen))
                    {
                        rolesToReassign.Add(player.Role);
                    }
                    playersToReassign.Add(player);
                    switch (player.Role.Team)
                    {
                        case Team.Mafia:
                            remainingMafCount++;
                            break;
                        case Team.Yakuza:
                            remainingYakuzaCount++;
                            break;
                    }
                    remainingPlayerCount++;
                    player.Role.Player = null;
                    player.StartRole = player.Role = new Citizen() { Player = player };
                }
            }

            foreach (var role in rolesToReassign)
            {
                AssignRoleToRandomPlayer(playersToReassign, role);
            }
        }

        private void AssignRoleToRandomPlayer(IEnumerable<InGamePlayerInfo> players, BaseRole role)
        {
            if ((remainingPlayerCount <= remainingMafCount || remainingPlayerCount <= remainingYakuzaCount) &&
                role.Team != Team.Mafia && role.Team != Team.Yakuza || remainingPlayerCount == 0)
            {
                Console.WriteLine("Недостаточно игроков для назначения роли {0}.", role.GetType().Name);
                return;
            }
            if (role is Warlock)
            {
                (role as Warlock).AvailableCursesCount = totalMafCount;
            }
            var playerIndex = random.Next(remainingPlayerCount);
            Console.WriteLine("Assined: {0} {1}", playerIndex, role.GetType().Name);
            foreach (var player in players)
            {
                if (player.Role is Citizen)
                {
                    if (playerIndex-- <= 0)
                    {
                        player.StartRole = role;
                        AssignRole(player, role);
                        break;
                    }
                }
            }
            remainingPlayerCount--;
            if (role.Team == Team.Mafia)
            {
                remainingMafCount--;
            }
            if (role.Team == Team.Yakuza)
            {
                remainingYakuzaCount--;
            }
        }

        public void AssignRole(InGamePlayerInfo player, BaseRole role)
        {
            player.Role = role;
            role.Player = player;
        }

        public void AssignStartRole(InGamePlayerInfo player, BaseRole role)
        {
            player.StartRole = role;
            role.Player = player;
        }
    }
}
