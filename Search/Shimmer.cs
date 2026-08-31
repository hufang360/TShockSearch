using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace Search
{
    /// <summary>
    /// 嬗变信息生成（供 /r 合成查询合并展示）
    /// </summary>
    public static class Shimmer
    {
        /// <summary>
        /// 微光（嬗变“制作站”）显示：物品图标+名称
        /// </summary>
        static readonly string ShimmerStation = "[i:5362]微光";

        /// <summary>
        /// 生成物品的嬗变信息行
        /// </summary>
        /// <param name="id">物品id</param>
        /// <returns>嬗变/分解/嬗变而来 信息行，以及涉及物品的id列表</returns>
        public static (List<string> Lines, List<int> Items) BuildLines(int id)
        {
            List<string> lines = new();
            List<int> shimmerItems = new();
            int equiv = Equivalent(id, forDecraft: false);
            int equivDecraft = Equivalent(id, forDecraft: true);

            // 硬币特殊处理（微光中转化为财运，不产出物品）
            if (IsCommonCoin(id))
            {
                lines.Add($"[i:{id}] @ {ShimmerStation} -> 财运(coin luck)");
            }
            else
            {
                // 嬗变优先（与原版一致：有嬗变就不会触发分解）
                int toItem = ShimmerTransforms.GetTransformToItem(equiv);
                if (toItem > 0 && Utils.IsItemID(toItem))
                {
                    string note = ShimmerTransforms.IsItemTransformLocked(equiv) ? Utils.Highlight("（击败月总后解锁）") : "";
                    lines.Add($"[i:{id}] @ {ShimmerStation} -> [i:{toItem}]{note}");
                    shimmerItems.Add(toItem);
                }
                else
                {
                    // 分解还原（还原成合成材料）
                    int decraftIdx = ShimmerTransforms.GetDecraftingRecipeIndex(equivDecraft);
                    if (decraftIdx >= 0 && decraftIdx < Main.recipe.Length)
                    {
                        string mats = string.Join("", Main.recipe[decraftIdx].requiredItem
                            .Where(r => r.stack > 0)
                            .Select(r => ShowIcon(r.type, r.stack)));
                        bool locked = ShimmerTransforms.RecipeSets.PostSkeletron != null && ShimmerTransforms.IsRecipeIndexDecraftLocked(decraftIdx);
                        lines.Add($"[i:{id}] @ {ShimmerStation} -> {mats}{(locked ? Utils.Highlight("（击败骷髅王/石巨人后解锁）") : "")}");
                        shimmerItems.AddRange(Main.recipe[decraftIdx].requiredItem
                            .Where(r => r.stack > 0)
                            .Select(r => r.type));
                    }
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
                // 与嬗变显示一致：来源物品 @ 微光 -> 本物品
                lines.AddRange(from.Select(x => $"[i:{x}] @ {ShimmerStation} -> [i:{id}]"));
                shimmerItems.AddRange(from);
            }
            return (lines, shimmerItems);
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
    }
}