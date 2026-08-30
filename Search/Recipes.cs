using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Map;
using TShockAPI;

namespace Search
{
    /// <summary>
    /// 查合成
    /// </summary>
    public class Recipes
    {

        /// <summary>
        /// 合成查询指令
        /// </summary>
        public static void Manage(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("请输入物品名或id！");
                return;
            }

            var HL = Utils.Highlight;

            // 初始化图格名称数据
            Mapping.BuildMapAtlas();

            // 记录用户输入的指令
            string rawCMD = Utils.GetInputRawCMD(args, 1);
            string ft = rawCMD + " {{0}}";
            int pageNumber;

            // 匹配目标物品的id
            string itemNameOrId = args.Parameters[0];
            List<int> ids = Utils.GetItemIDByIdOrName(itemNameOrId, false);
            if (ids.Count == 0)
            {
                args.Player.SendInfoMessage($"未找到物品，可输入{HL("/search")} {HL(itemNameOrId)}模糊匹配相关的物品名称和id");
                return;
            }

            // 找到多个
            List<string> lines = new();
            if (ids.Count > 1)
            {
                lines = Utils.WrapItemResult(ids);
                if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber)) return;

                PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
                {
                    HeaderFormat = "匹配到多个物品({0}/{1}):",
                    FooterFormat = $"输入{HL(ft)}查看更多".SFormat(Commands.Specifier),
                    MaxLinesPerPage = Utils.MaxLinesPerPage,
                });
                return;
            }

            // 显示这个物品的配方，以及可以合成哪些物品
            int id = ids[0];
            List<int> items = new();
            List<int> li = new();

            // 配方
            List<Recipe> list = Main.recipe.ToList().FindAll((Recipe r) => r.createItem.type == id);
            for (int i = 0; i < list.Count; i++)
            {
                ShowOneRequire(list[i], ref lines, ref items);
            }

            // 可以合成哪些物品
            ShowCreate(id, ref lines, ref items);

            // 嬗变信息（合并 /shi 的展示）
            List<string> shimmerLines = Shimmer.BuildLines(itemNameOrId, id);

            // 显示结果
            if (!lines.Any() && !shimmerLines.Any())
            {
                string item = Utils.ShowItemByText(itemNameOrId, id);
                args.Player.SendInfoMessage($"{item}，无合成配方，也不是合成材料，也没有嬗变信息！");
                return;
            }

            // 显示涉及物品的id（备注行）
            if (items.Any())
            {
                items = items.Distinct().ToList();
                List<string> newLines = Utils.WrapItemResult(items);
                newLines[0] = $"备注: {newLines[0]}";
                lines = lines.Concat(newLines).ToList();
            }

            // 追加嬗变信息
            if (shimmerLines.Any())
                lines = lines.Concat(shimmerLines).ToList();

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber)) return;
            PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
            {
                HeaderFormat = (shimmerLines.Any()
                    ? $"{HightLightItemName(id)} 的合成·嬗变信息"
                    : $"{HightLightItemName(id)} 的合成信息") + "({0}/{1}):",
                FooterFormat = $"输入{HL(ft)}查看更多".SFormat(Commands.Specifier),
                MaxLinesPerPage = Utils.MaxLinesPerPage,
            });
        }

        /// <summary>
        /// 显示合成配方
        /// </summary>
        static void ShowOneRequire(Recipe recipe, ref List<string> lines, ref List<int> items)
        {
            List<string> li = new();
            foreach (Item item in recipe.requiredItem.Where((Item r) => r.stack > 0))
            {
                li.Add(ShowItemLite(item.type, item.stack));
                items.Add(item.type);
            }

            List<string> li2 = new();

            // 处理 requiredTile - 兼容不同 Terraria 版本
            // 1.4.5+ 为单个 int（图格id）；1.4.4 及更早为 int[] 数组
            var requiredTileProp = recipe.GetType().GetField("requiredTile");
            if (requiredTileProp != null)
            {
                if (requiredTileProp.FieldType.IsArray)
                {
                    // 数组形式
                    var requiredTileArray = (int[])requiredTileProp.GetValue(recipe);
                    foreach (int id in requiredTileArray)
                    {
                        if (id > 0)
                            ShowStation(id, ref li2, ref items);
                    }
                }
                else if (requiredTileProp.FieldType == typeof(int))
                {
                    // 单个图格形式
                    int id = (int)requiredTileProp.GetValue(recipe);
                    if (id > 0)
                        ShowStation(id, ref li2, ref items);
                }
            }

            if (recipe.needHoney) li2.Add("[i:1128]蜂蜜"); // 1128
            if (recipe.needWater) li2.Add("[i:206]水"); // 206
            if (recipe.needLava) li2.Add("[i:207]岩浆"); // 207
            if (recipe.needSnowBiome) li2.Add("[i:1596]雪原"); // 1596
            if (recipe.needGraveyardBiome) li2.Add("[i:4358]灵雾"); // 4358
            if (recipe.needTorchGodsFavor) li2.Add("[i:5043]火把神祝福"); // 5043
            if (recipe.needMechdusa) li2.Add("[i:5334]机械美杜莎");  // 5334
            // if (recipe.needEverythingSeed) li2.Add($"{Utils.Highlight("getfixedboi")}世界");

            string head = string.Format("[i/s{1}:{0}]", recipe.createItem.type, recipe.createItem.stack);
            string s = li2.Any() ? $" @ {string.Join(",", li2)}" : "";

            lines.Add($"{string.Join("", li)}{s} -> {head}");
        }


        /// <summary>
        /// 显示合成条件中的制作站（图格）
        /// </summary>
        /// <param name="tileId">图格id</param>
        static void ShowStation(int tileId, ref List<string> li2, ref List<int> items)
        {
            // 图格 -> 图例名称（带越界保护）
            string tileName = "";
            int legend = tileId >= 0 && tileId < MapHelper.tileLookup.Length ? MapHelper.tileLookup[tileId] : -1;
            if (legend >= 0 && legend < Lang._mapLegendCache.Length)
                tileName = Lang._mapLegendCache[legend].Value;
            if (string.IsNullOrEmpty(tileName))
                tileName = $"图格{tileId}";

            // 匹配物品图标（可能匹配到2个，例如：砧，匹配铁砧和铅砧）
            var map = Mapping.GetCraftingStations();
            if (map.ContainsValue(tileName))
            {
                foreach (var obj in map)
                {
                    if (obj.Value == tileName)
                    {
                        items.Add(obj.Key);
                        li2.Add(ShowItemLite(obj.Key));
                    }
                }
            }
            else
            {
                li2.Add(tileName);
            }
        }

        /// <summary>
        /// 显示合成
        /// </summary>
        static void ShowCreate(int id, ref List<string> lines, ref List<int> items)
        {
            var founds = from r in Main.recipe
                         where r.requiredItem.Select((Item i) => i.type).Contains(id)
                         select r;
            foreach (var r in founds)
            {
                items.Add(r.createItem.type);
                ShowOneRequire(r, ref lines, ref items);
            }
        }

        /// <summary>
        /// 显示物品
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        static string ShowItemLite(int id, int stack = 1)
        {
            return $"[i/s{stack}:{id}]";
        }

        /// <summary>
        /// 高亮显示带物品图标的物品名称
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        static string HightLightItemName(int id)
        {
            var s = $"{Lang.GetItemNameValue(id)}({id})";
            return $"[i:{id}]{Utils.Highlight(s)}";
        }

    }

}