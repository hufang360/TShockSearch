/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace Search
{
    /// <summary>
    /// Provides a series of methods that give Chinese texts
    /// </summary>
    public static class ChineseLanguage
    {
        private static readonly Dictionary<int, string> ItemNames = new();

        private static readonly Dictionary<int, string> NpcNames = new();

        private static readonly Dictionary<int, string> Prefixs = new();

        private static readonly Dictionary<int, string> Buffs = new();

        private static bool Inited = false;

        internal static void Initialize()
        {
            if (Inited) return;
            Inited = true;

            var culture = Language.ActiveCulture;

            var skip = culture == GameCulture.FromCultureName(GameCulture.CultureName.Chinese);

            try
            {
                if (!skip)
                {
                    LanguageManager.Instance.SetLanguage(GameCulture.FromCultureName(GameCulture.CultureName.Chinese));
                }

                for (var i = -48; i < Terraria.ID.ItemID.Count; i++)
                {
                    ItemNames.Add(i, Lang.GetItemNameValue(i));
                }

                for (var i = -17; i < Terraria.ID.NPCID.Count; i++)
                {
                    NpcNames.Add(i, Lang.GetNPCNameValue(i));
                }

                for (var i = 0; i < Terraria.ID.BuffID.Count; i++)
                {
                    Buffs.Add(i, Lang.GetBuffName(i));
                }

                foreach (var field in typeof(Main).Assembly.GetType("Terraria.ID.PrefixID")
                            .GetFields().Where(f => !f.Name.Equals("Count", StringComparison.Ordinal)))
                {
                    var i = (int)field.GetValue(null);
                    Prefixs.Add(i, Lang.prefix[i].Value);
                }
            }
            finally
            {
                if (!skip)
                {
                    LanguageManager.Instance.SetLanguage(culture);
                }
            }
        }

        /// <summary>
        /// Get the Chinese name of an item
        /// </summary>
        /// <param name="id">Id of the item</param>
        /// <returns>Item name in Chinese</returns>
        public static string GetItemNameById(int id)
        {
            ItemNames.TryGetValue(id, out string itemName);
            return itemName;
        }

        /// <summary>
        /// Get the Chinese name of a npc
        /// </summary>
        /// <param name="id">Id of the npc</param>
        /// <returns>Npc name in Chinese</returns>
        public static string GetNpcNameById(int id)
        {
            NpcNames.TryGetValue(id, out string npcName);
            return npcName;
        }

        /// <summary>
        /// Get prefix in Chinese
        /// </summary>
        /// <param name="id">Prefix Id</param>
        /// <returns>Prefix in Chinese</returns>
        public static string GetPrefixById(int id)
        {
            Prefixs.TryGetValue(id, out string prefix);
            return prefix;
        }

        /// <summary>
        /// Get buff name in Chinese
        /// </summary>
        /// <param name="id">Buff Id</param>
        /// <returns>Buff name in Chinese</returns>
        public static string GetBuffNameById(int id)
        {
            string buff;
            if (Buffs.TryGetValue(id, out buff))
                return buff;

            return null;
        }
    }
}
