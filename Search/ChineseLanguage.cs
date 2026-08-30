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
    }
}