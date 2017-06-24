using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordMafia.Roles;

namespace DiscordMafia
{
    public static class PlayerSorter
    {
        private static string[] nightSortPositions;
        private static string[] daySortPositions;

        static PlayerSorter()
        {
            nightSortPositions = new string[]
            {
                typeof(Ninja).Name,
                typeof(Hoodlum).Name,
                typeof(Wench).Name,
                typeof(Maniac).Name,
                typeof(Homeless).Name,
                typeof(Commissioner).Name,
                typeof(Sheriff).Name,
                typeof(Killer).Name,
                typeof(Lawyer).Name,
                typeof(Doctor).Name,
                typeof(Demoman).Name,
                typeof(Warlock).Name,
            };
            daySortPositions = new string[] {
                typeof(Judge).Name,
                typeof(Elder).Name,
            };
        }

        public static List<InGamePlayerInfo> SortForActivityCheck(List<InGamePlayerInfo> players, GameState state)
        {
            // Копируем список
            players = new List<InGamePlayerInfo>(players);

            Comparison<InGamePlayerInfo> comparer = null;
            players.RemoveAll(delegate (InGamePlayerInfo player) { return !player.isAlive || !(player.role is UniqueRole); });
            switch (state)
            {
                case GameState.Night:
                    comparer = ActivityCheckNightComparer;
                    break;
                case GameState.Day:
                    comparer = ActivityCheckDayComparer;
                    break;
            }
            if (comparer != null)
            {
                players.Sort(comparer);
            }
            return players;
        }

        private static int ActivityCheckNightComparer(InGamePlayerInfo player1, InGamePlayerInfo player2)
        {
            var idx1 = Array.IndexOf(nightSortPositions, player1.role.GetType().Name);
            if (idx1 < 0)
            {
                idx1 = int.MaxValue;
            }
            var idx2 = Array.IndexOf(nightSortPositions, player2.role.GetType().Name);
            if (idx2 < 0)
            {
                idx2 = int.MaxValue;
            }
            return idx1 - idx2;
        }

        private static int ActivityCheckDayComparer(InGamePlayerInfo player1, InGamePlayerInfo player2)
        {
            var idx1 = Array.IndexOf(daySortPositions, player1.role.GetType().Name);
            if (idx1 < 0)
            {
                idx1 = int.MaxValue;
            }
            var idx2 = Array.IndexOf(daySortPositions, player2.role.GetType().Name);
            if (idx2 < 0)
            {
                idx2 = int.MaxValue;
            }
            return idx1 - idx2;
        }
    }
}
