using Search.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TShockAPI;

namespace Search
{
    /// <summary>
    /// 工具类
    /// </summary>
    public class Utils
    {

        #region 查找物品名
        /// <summary>
        /// 通过id或物品名称获取物品id
        /// </summary>
        /// <param name="text"></param>
        public static List<int> GetItemIDByIdOrName(string text, bool fuzzy = true)
        {
            if (string.IsNullOrEmpty(text))
                return new List<int>();

            if (int.TryParse(text, out int result))
            {
                if (!IsItemID(result))
                    return new List<int>();
                else
                    return new List<int> { result };
            }

            result = GetItemIDFromTag(text);
            if (result != 0)
                return new List<int> { result };
            else
                return GetItemIDByName(text, fuzzy);
        }

        /// <summary>
        /// 物品id是否有效
        /// </summary>
        public static bool IsItemID(int id)
        {
            id = ItemID.FromNetId((short)id);
            return id > 0 && id < ItemID.Count;
        }

        /// <summary>
        /// 物品id是否有效
        /// </summary>
        public static bool IsItemID(string value)
        {
            if (int.TryParse(value, out int id))
                return IsItemID(id);
            else
                return false;
        }

        /// <summary>
        /// 获取tag里面的物品id
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static int GetItemIDFromTag(string tag)
        {
            Match match = new Regex("\\[i(tem)?(?:\\/s(?<Stack>\\d{1,3}))?(?:\\/p(?<Prefix>\\d{1,3}))?:(?<NetID>-?\\d{1,4})\\]").Match(tag);
            if (!match.Success)
                return 0;
            return int.Parse(match.Groups["NetID"].Value);
        }

        /// <summary>
        /// 通过名字匹配物品id
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fuzzy">是否开启模糊匹配</param>
        /// <returns></returns>
        public static List<int> GetItemIDByName(string name, bool fuzzy = true)
        {
            string text = name.ToLowerInvariant();
            string text2;
            List<int> li = new();

            // wiki物品名称
            if (fuzzy)
            {
                var dict = DBHelper.Wiki.All();
                foreach (var obj in dict)
                {
                    text2 = obj.Value.ToLowerInvariant();
                    if (text2 == text || text2.StartsWith(text) || text2.EndsWith(text))
                    {
                        li.Add(obj.Key);
                    }
                }
            }
            else
            {
                int id = DBHelper.Wiki.GetID(name);
                if (id != 0)
                {
                    return new List<int> { id };
                }
            }


            bool flag = Language.ActiveCulture != GameCulture.FromCultureName(GameCulture.CultureName.Chinese);
            for (int i = 1; i < ItemID.Count; i++)
            {
                // 当前服务器语言
                text2 = Lang.GetItemNameValue(i).ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(text2))
                {
                    if (fuzzy)
                    {
                        // 首尾模糊匹配
                        if (text2 == text || text2.StartsWith(text) || text2.EndsWith(text))
                        {
                            li.Add(i);
                        }
                    }
                    else
                    {
                        // 精确匹配
                        if (text2 == text)
                        {
                            return new List<int> { i };
                        }
                    }
                }

                //if (fuzzy && DBHelper.TableWiki.ContainID(i))
                //{
                //    text2 = DBHelper.TableWiki.GetName(i);
                //    if (text2 == text || text2.StartsWith(text) || text2.EndsWith(text))
                //    {
                //        li.Add(i);
                //    }
                //}

                // 中文物品名称
                if (!flag) continue;
                text2 = GetCNItemNameById(i).ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(text2))
                {
                    if (fuzzy)
                    {
                        if (text2 == text || text2.StartsWith(text) || text2.EndsWith(text))
                        {
                            li.Add(i);
                        }
                    }
                    else
                    {
                        if (text2 == text)
                        {
                            return new List<int> { i };
                        }
                    }
                }
            }
            return li;
        }

        /// <summary>
        /// 获取中文物品名
        /// </summary>
        public static string GetCNItemNameById(int id)
        {
            return ChineseLanguage.GetItemNameById(id);
        }
        #endregion


        #region 显示物品查询结果
        /// <summary>
        /// 换行显示结果（一行5个）
        /// </summary>
        public static List<string> WrapItemResult(List<int> ids)
        {
            var li = ids.Select(id => ShowItemByID(id)).ToList();
            var lines = WarpLines(li, 5);
            return lines;
        }

        /// <summary>
        /// 换行显示结果（一行5个）
        /// </summary>
        public static List<string> WarpItemResult(Dictionary<int, string> dict)
        {
            var li = dict.Select(obj => ShowItem(obj.Key, obj.Value)).ToList();
            var lines = WarpLines(li, 5);
            return lines;
        }

        /// <summary>
        /// 将字符串换行
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="column">列数，1行显示多个</param>
        /// <returns></returns>
        public static List<string> WarpLines(List<string> lines, int column = 5)
        {
            List<string> li1 = new();
            List<string> li2 = new();
            foreach (var line in lines)
            {
                if (li2.Count % column == 0)
                {
                    if (li2.Count > 0)
                    {
                        li1.Add(string.Join(", ", li2));
                        li2.Clear();
                    }
                }
                li2.Add(line);
            }
            if (li2.Any())
            {
                li1.Add(string.Join(", ", li2));
            }
            return li1;
        }


        /// <summary>
        /// 显示物品
        /// </summary>
        public static string ShowItem(int id, string name) { return $"[i:{id}]{name}({id})"; }

        /// <summary>
        /// 显示物品
        /// </summary>
        public static string ShowItemByID(int id) { return $"[i:{id}]{Lang.GetItemNameValue(id)}({id})"; }

        ///// <summary>
        ///// 显示物品
        ///// </summary>
        //public static string ShowItemByID(int id, int stack) { return $"[i/s{stack}:{id}]{Lang.GetItemNameValue(id)}({id})"; }
        #endregion

        /// <summary>
        /// 获得用户输入的指令
        /// </summary>
        /// <param name="args"></param>
        /// <param name="num">扩展到第几个参数，例如：/search item指令，要完整显示则是num=1</param>
        /// <returns></returns>
        public static string GetInputRawCMD(CommandArgs args, int num = 0)
        {
            // 记录用户输入的指令
            var CommandSpecifier = args.Silent ? TShock.Config.Settings.CommandSilentSpecifier : TShock.Config.Settings.CommandSpecifier;
            var pArr = args.Message.Split(" ");
            if (num == 0)
                return $"{CommandSpecifier}{pArr[0]}";
            else
                return $"{CommandSpecifier}{pArr[0]} {string.Join(" ", args.Parameters.Take(num))}";
        }

        /// <summary>
        /// 开服时间
        /// </summary>
        static readonly DateTime UtcNow = DateTime.UtcNow;

        /// <summary>
        /// 获取当前时间的 unix时间戳(毫秒)
        /// </summary>
        public static double GetUnixTimestamp { get { return (int)DateTime.UtcNow.Subtract(UtcNow).TotalMilliseconds; } }

        #region 通用
        /// <summary>
        /// 高亮显示文本
        /// </summary>
        public static string Highlight(object msg) { return $"[c/96FF0A:{msg}]"; }

        /// <summary>
        /// 打勾显示
        /// </summary>
        public static string CFlag(bool foo, string fstr) { return foo ? $"[c/96FF96:✔]{fstr}" : $"-{fstr}"; }

        /// <summary>
        /// 打勾显示
        /// </summary>
        public static string CFlag(string fstr, bool foo) { return foo ? $"{fstr}✓" : $"{fstr}-"; }

        /// <summary>
        /// 输出日志
        /// </summary>
        public static void Log(string msg) { TShock.Log.ConsoleInfo($"[查一查]{msg}"); }

        /// <summary>
        /// 输出日志
        /// </summary>
        public static void Log(object obj) { TShock.Log.ConsoleInfo($"[查一查]{obj}"); }
        #endregion
    }

}