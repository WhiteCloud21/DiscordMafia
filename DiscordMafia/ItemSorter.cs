using DiscordMafia.Base;
using DiscordMafia.Items;
using System;
using System.Collections.Generic;

namespace DiscordMafia
{
    public static class ItemListExtensions
    {
        private static string[] _nightSortPositions;
        private static string[] _daySortPositions;

        static ItemListExtensions()
        {
            _nightSortPositions = new string[]
            {
                typeof(Mask).Name,
                typeof(AntiMask).Name,
            };
            _daySortPositions = new string[] {
            };
        }

        public static List<BaseItem> SortForUse(this List<BaseItem> items, GameState state)
        {
            // Копируем список
            items = new List<BaseItem>(items);

            Comparison<BaseItem> comparer = null;
            items.RemoveAll(delegate (BaseItem item) { return !item.IsActive; });
            switch (state)
            {
                case GameState.Night:
                    comparer = NightComparer;
                    break;
                case GameState.Day:
                    comparer = DayComparer;
                    break;
            }
            if (comparer != null)
            {
                items.Sort(comparer);
            }
            return items;
        }

        private static int NightComparer(BaseItem item1, BaseItem item2)
        {
            var idx1 = Array.IndexOf(_nightSortPositions, item1.GetType().Name);
            if (idx1 < 0)
            {
                idx1 = int.MaxValue;
            }
            var idx2 = Array.IndexOf(_nightSortPositions, item2.GetType().Name);
            if (idx2 < 0)
            {
                idx2 = int.MaxValue;
            }
            return idx1 - idx2;
        }

        private static int DayComparer(BaseItem item1, BaseItem item2)
        {
            var idx1 = Array.IndexOf(_daySortPositions, item1.GetType().Name);
            if (idx1 < 0)
            {
                idx1 = int.MaxValue;
            }
            var idx2 = Array.IndexOf(_daySortPositions, item2.GetType().Name);
            if (idx2 < 0)
            {
                idx2 = int.MaxValue;
            }
            return idx1 - idx2;
        }
    }
}
