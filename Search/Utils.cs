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

        /// <summary>
        /// 分页每页显示行数（配置文件 maxLinesPerPage 赋值）
        /// </summary>
        public static int MaxLinesPerPage = 8;

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
        /// 物品标签正则：匹配 [i:17]、[i/p77:17]、[i/s2:17]、[i/p77,s2:17]、[i/s2/p77:17]、[i:17 2] 等常见格式
        /// </summary>
        static readonly Regex ItemTagRegex = new(@"\[i(tem)?(?:(?<Options>\/[^:\]]+))?:(?<NetID>-?\d{1,4})(?: (?<Stack>\d{1,4}))?\]", RegexOptions.Compiled);

        /// <summary>
        /// 获取tag里面的物品id
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static int GetItemIDFromTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return 0;
            Match match = ItemTagRegex.Match(tag);
            if (!match.Success)
                return 0;
            return int.Parse(match.Groups["NetID"].Value);
        }

        /// <summary>
        /// 提取消息中的物品标签，替换为纯文本物品id
        /// 颜色标签[c/颜色:内容]与物品标签[i/xxx]嵌套时，只有颜色会生效，物品标签会显示为原文，
        /// 此方法将物品标签转换为普通文本，保证颜色高亮时能正常显示，例如：[i/p77:17] -> 17
        /// 提示语用于让玩家重输指令查看更多，数量(stack)不属于查询参数，因此只保留id不显示数量
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string ReplaceItemTags(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;
            return ItemTagRegex.Replace(message, m => m.Groups["NetID"].Value);
        }

        /// <summary>
        /// 名称索引缓存：小写名称 -> id列表（首次查询懒构建，wiki导入后调用 MarkNameIndexDirty 重建）
        /// </summary>
        static Dictionary<string, List<int>> _nameIndex;

        static bool _nameIndexDirty = true;

        static readonly object _nameIndexLock = new();

        /// <summary>
        /// 标记名称索引失效（wiki数据变更后调用，下次查询自动重建）
        /// </summary>
        public static void MarkNameIndexDirty()
        {
            _nameIndexDirty = true;
        }

        /// <summary>
        /// 构建名称索引（懒加载：首次查询时执行一次）
        /// </summary>
        static void BuildNameIndex()
        {
            lock (_nameIndexLock)
            {
                if (!_nameIndexDirty && _nameIndex != null)
                    return;

                // 初始化中文名缓存
                ChineseLanguage.Initialize();

                var index = new Dictionary<string, List<int>>();
                void Add(string name, int id)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return;
                    string key = name.ToLowerInvariant();
                    if (!index.TryGetValue(key, out var li))
                        index[key] = li = new List<int>();
                    if (!li.Contains(id))
                        li.Add(id);
                }

                // wiki 修正名
                foreach (var obj in DBHelper.Wiki.All())
                    Add(obj.Value, obj.Key);

                // 游戏内物品名（当前语言） + 中文名
                for (int i = 1; i < ItemID.Count; i++)
                {
                    Add(Lang.GetItemNameValue(i), i);
                    Add(ChineseLanguage.GetItemNameById(i), i);
                }

                _nameIndex = index;
                _nameIndexDirty = false;
            }
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
            BuildNameIndex();

            // 精确匹配
            if (_nameIndex.TryGetValue(text, out var hits))
                return new List<int>(hits);

            if (!fuzzy)
                return new List<int>();

            // 模糊匹配（首/尾）
            List<int> li = new();
            foreach (var kv in _nameIndex)
            {
                if (kv.Key.StartsWith(text) || kv.Key.EndsWith(text))
                    li.AddRange(kv.Value);
            }
            return li.Distinct().ToList();
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

        /// <summary>
        /// 数量提取正则（匹配 /s47、,s47、/x47）
        /// </summary>
        static readonly Regex ItemCountRegex = new(@"[\/,][sx](\d{1,4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 显示物品（图标+名称+id），用于替换提示信息中的纯数字高亮，不套颜色代码
        /// 若输入是带数量的物品标签，图标会带上数量，例如：[i/s47:6173] -> [i/s47:6173]名称(6173)
        /// </summary>
        /// <param name="text">用户输入</param>
        /// <param name="id">解析出的物品id</param>
        /// <returns></returns>
        public static string ShowItemByText(string text, int id)
        {
            int stack = 1;
            Match m = ItemTagRegex.Match(text ?? "");
            if (m.Success)
            {
                // 空格数量格式：[i:17 2]
                if (m.Groups["Stack"].Success && int.TryParse(m.Groups["Stack"].Value, out int s1) && s1 > 0)
                    stack = s1;
                else
                {
                    // 选项段数量格式：[i/p77,s47:6173] / [i/s47:6173]
                    var sm = ItemCountRegex.Match(m.Groups["Options"].Value);
                    if (sm.Success && int.TryParse(sm.Groups[1].Value, out int s2) && s2 > 0)
                        stack = s2;
                }
            }
            string icon = stack > 1 ? $"[i/s{stack}:{id}]" : $"[i:{id}]";
            return $"{icon}{Lang.GetItemNameValue(id)}({id})";
        }
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
        public static string Highlight(object msg) { return $"[c/96FF0A:{ReplaceItemTags(msg?.ToString())}]"; }

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