using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TShockAPI;

namespace Search
{
    /// <summary>
    /// 查嬗变
    /// </summary>
    public class Shimmer
    {
        /// <summary>
        /// 嬗变查询指令
        /// </summary>
        public static void Manage(CommandArgs args)
        {
            var HL = Utils.Highlight;

            // /shi 或 /shi help|h 显示帮助
            if (args.Parameters.Count == 0 || args.Parameters[0].ToLowerInvariant() is "help" or "h" or "帮助")
            {
                Help(args);
                return;
            }

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

            // 显示嬗变信息
            int id = ids[0];
            lines = BuildLines(itemNameOrId, id);

            // 显示结果
            if (!lines.Any())
            {
                args.Player.SendInfoMessage($"{Utils.ShowItemByText(itemNameOrId, id)}，没有嬗变信息！");
                return;
            }

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber)) return;
            PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
            {
                HeaderFormat = $"{HighlightedItem(id)} 的嬗变信息" + "({0}/{1}):",
                FooterFormat = $"输入{HL(ft)}查看更多".SFormat(Commands.Specifier),
                MaxLinesPerPage = Utils.MaxLinesPerPage,
            });
        }

        /// <summary>
        /// 生成物品的嬗变信息行（/r 合成查询合并显示与 /shi 独立查询共用）
        /// </summary>
        /// <param name="itemNameOrId">用户输入（用于展示）</param>
        /// <param name="id">解析出的物品id</param>
        /// <returns>嬗变/分解/嬗变而来 信息行</returns>
        public static List<string> BuildLines(string itemNameOrId, int id)
        {
            List<string> lines = new();
            int equiv = Equivalent(id, forDecraft: false);
            int equivDecraft = Equivalent(id, forDecraft: true);

            // 硬币特殊处理（微光中转化为财运，不产出物品）
            if (IsCommonCoin(id))
            {
                lines.Add($"嬗变: {Utils.ShowItemByID(id)} 是硬币，扔进微光不会转化物品，而是增加财运(coin luck)");
            }
            else
            {
                // 嬗变成什么
                int toItem = ShimmerTransforms.GetTransformToItem(equiv);
                if (toItem > 0 && Utils.IsItemID(toItem))
                {
                    string note = ShimmerTransforms.IsItemTransformLocked(equiv) ? Utils.Highlight("（击败月总后解锁）") : "";
                    lines.Add($"嬗变: {Utils.ShowItemByID(toItem)}{note}");
                }

                // 分解还原（还原成合成材料）
                int decraftIdx = ShimmerTransforms.GetDecraftingRecipeIndex(equivDecraft);
                if (decraftIdx >= 0 && decraftIdx < Main.recipe.Length)
                {
                    string mats = string.Join("", Main.recipe[decraftIdx].requiredItem
                        .Where(r => r.stack > 0)
                        .Select(r => ShowIcon(r.type, r.stack)));
                    bool locked = ShimmerTransforms.RecipeSets.PostSkeletron != null && ShimmerTransforms.IsRecipeIndexDecraftLocked(decraftIdx);
                    lines.Add($"分解: {mats}{(locked ? Utils.Highlight("（击败骷髅王/石巨人后解锁）") : "")}");
                }
            }

            // 谁能嬗变成它（反向）
            List<int> from = new();
            if (ItemID.Sets.ShimmerTransformToItem != null)
            {
                for (int i = 1; i < ItemID.Sets.ShimmerTransformToItem.Length; i++)
                {
                    if (ItemID.Sets.ShimmerTransformToItem[i] == id)
                        from.Add(i);
                }
            }
            if (from.Any())
            {
                List<string> newLines = Utils.WrapItemResult(from);
                newLines[0] = $"嬗变而来: {newLines[0]}";
                lines = lines.Concat(newLines).ToList();
            }
            return lines;
        }

        /// <summary>
        /// 帮助
        /// </summary>
        static void Help(CommandArgs args)
        {
            var HL = Utils.Highlight;
            List<string> lines = new()
            {
                "/shi <物品名/id>, 查看物品的嬗变信息",
                "嬗变: 物品扔进微光后变成什么",
                "分解: 物品扔进微光后还原成哪些材料",
                "嬗变而来: 哪些物品扔进微光会变成它",
                "/shi help, 显示本帮助",
            };
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber)) return;
            string rawCMD = Utils.GetInputRawCMD(args);
            string ft = rawCMD + " help {{0}}";
            PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
            {
                HeaderFormat = $"{HL("/shi")}指令用法" + "({0}/{1}):",
                FooterFormat = $"输入{HL(ft)}查看更多".SFormat(Commands.Specifier),
                MaxLinesPerPage = Utils.MaxLinesPerPage,
            });
        }

        /// <summary>
        /// 物品的微光等价类型（例如水桶算水、蜂蜜瓶算蜂蜜）
        /// </summary>
        static int Equivalent(int type, bool forDecraft)
        {
            var arr = forDecraft ? ItemID.Sets.ShimmerCountsAsItemForDecraft : ItemID.Sets.ShimmerCountsAsItem;
            if (arr != null && type >= 0 && type < arr.Length)
            {
                int mapped = arr[type];
                if (mapped != -1)
                    return mapped;
            }
            return type;
        }

        /// <summary>
        /// 是否硬币
        /// </summary>
        static bool IsCommonCoin(int type)
        {
            var arr = ItemID.Sets.CommonCoin;
            return arr != null && type >= 0 && type < arr.Length && arr[type];
        }

        /// <summary>
        /// 显示物品图标
        /// </summary>
        static string ShowIcon(int id, int stack = 1) { return $"[i/s{stack}:{id}]"; }

        /// <summary>
        /// 高亮显示带物品图标的物品名称
        /// </summary>
        static string HighlightedItem(int id)
        {
            var s = $"{Lang.GetItemNameValue(id)}({id})";
            return $"[i:{id}]{Utils.Highlight(s)}";
        }

    }
}