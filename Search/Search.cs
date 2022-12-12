using Search.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace Search
{
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {

        public override string Description => "";
        public override string Name => "查一查";
        public override string Author => "hufang360";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;


        /// <summary>
        /// 权限
        /// </summary>
        static readonly string permissionSearch = "hf.search";
        static readonly string permissionSearchAdmin = "hf.search.admin";
        static readonly string permissionReicpe = "hf.recipe";

        /// <summary>
        /// 路径
        /// </summary>
        static readonly string WorkDir = Path.Combine(TShock.SavePath, "Search");
        static readonly string DBFile = Path.Combine(WorkDir, "Search.sqlite");
        static readonly string ConfigFile = Path.Combine(WorkDir, "config.json");
        static readonly string WikiFile = Path.Combine(WorkDir, "zh-Hans-wiki.json");


        public Plugin(Main game) : base(game)
        {
        }

        /// <summary>
        /// 初始时
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }

        /// <summary>
        /// 销毁时
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        void OnInitialize(EventArgs args)
        {
            // 创建工作目录
            if (!Directory.Exists(WorkDir))
                Directory.CreateDirectory(WorkDir);

            // 读取配置文件
            var _config = Config.Load(ConfigFile);

            // 移除冲突的其它指令别名
            string[] cmds1 = new string[] { "search" }.Concat(_config.aliasSearch).ToArray();
            string[] cmds2 = new string[] { "recipe" }.Concat(_config.aliasRecipe).ToArray();
            string[] cmds3 = cmds1.Concat(cmds2).ToArray();
            foreach (Command c in Commands.ChatCommands)
            {
                foreach (var cmd in cmds3)
                {
                    if (c.Names.Contains(cmd))
                        c.Names.Remove(cmd);
                }
            }

            // 注册指令
            Commands.ChatCommands.Add(new Command(permissionSearch, SearchCommand, cmds1) { HelpText = "查物品" });
            Commands.ChatCommands.Add(new Command(permissionReicpe, Recipes.Manage, cmds2) { HelpText = "查合成" });


            // 初始化数据库
            DBHelper.Connect(DBFile);
        }

        /// <summary>
        /// 处理指令
        /// </summary>
        void SearchCommand(CommandArgs args)
        {
            // 记录用户输入的指令
            string rawCMD = Utils.GetInputRawCMD(args);
            var HL = Utils.Highlight;
            #region 帮助
            void Help()
            {
                List<string> lines = new()
                {
                    "/search <关键词>, 查找物品",
                    "/search <id+>, 查找指定id起始的物品，例如 100+ 表示查看101~120的物品",
                    "/search list, 列出配置的关键词",
                };

                // 服主专用指令
                if (args.Player.HasPermission(permissionSearchAdmin))
                {
                    lines.Add("/search add <关键词> <id>, 添加关键词");
                    lines.Add("/search del <关键词>, 删除关键词");
                }

                if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber)) return;
                string ft = rawCMD + " help {{0}}";
                PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
                {
                    HeaderFormat = $"{HL("/search")}指令用法" + "({0}/{1}):",
                    FooterFormat = $"输入{HL(ft)}查看更多".SFormat(Commands.Specifier)
                });
            }
            #endregion

            if (args.Parameters.Count == 0)
            {
                Help();
                return;
            }

            // 初始化中文语言
            ChineseLanguage.Initialize();


            // 指令判断
            switch (args.Parameters[0].ToLowerInvariant())
            {
                // 帮助
                case "help": case "帮助": Help(); return;

                // 添加 / 删除 / 列出 关键词
                case "add": AddCommand(args); return;
                case "del": DelCommand(args); return;
                case "list": ListCommand(args); return;

                // 导入wiki物品名
                case "import": ImportCommand(args); return;
            }

            // 查物品
            rawCMD = $"{rawCMD} {args.Parameters[0]}";
            SearchItem(args, rawCMD);
        }


        /// <summary>
        /// 查物品
        /// </summary>
        static void SearchItem(CommandArgs args, string rawCMD)
        {
            int id;

            // 某个id后20个物品
            if (args.Parameters[0].EndsWith("+"))
            {
                var s = args.Parameters[0].Trim('+');
                // id为0是，显示从1开始的物品
                if (s == "0" || Utils.IsItemID(s))
                {
                    int.TryParse(s, out id);
                    ShowRange(args, rawCMD, id);
                }
                else
                {
                    args.Player.SendInfoMessage($"最后一个物品id是{ItemID.Count - 1}");
                }
                return;
            }

            // 物品
            Dictionary<int, string> dict = new();
            string itemNameOrId = args.Parameters[0].ToLowerInvariant();

            // db映射
            List<int> ids;
            string dbResult = DBHelper.Item.GetValue(itemNameOrId);
            foreach (var i in dbResult.Split(",").Where(i => !string.IsNullOrEmpty(i)))
            {
                ids = Utils.GetItemIDByIdOrName(i);
                if (ids.Any())
                {
                    id = ids.First();
                    if (!dict.ContainsKey(id))
                        dict.Add(id, Lang.GetItemName(id).Value);
                }
            }

            // 查找物品名
            var start = Utils.GetUnixTimestamp;
            ids = Utils.GetItemIDByIdOrName(itemNameOrId);
            var end = Utils.GetUnixTimestamp;
            Utils.Log($"查询耗时：{end - start}毫秒");

            if (ids.Any())
            {
                foreach (var i in ids)
                {
                    if (!dict.ContainsKey(i))
                        dict.Add(i, Lang.GetItemName(i).Value);
                }
            }


            if (dict.Count == 0)
            {
                // 未查到
                args.Player.SendErrorMessage("电波未能到达 o(ﾟДﾟ)っ！");
            }
            else if (dict.Count == 1)
            {
                // 单个结果
                args.Player.SendInfoMessage(Utils.ShowItem(dict.First().Key, dict.First().Value));
            }
            else
            {
                // 多个结果
                List<string> lines = Utils.WarpItemResult(dict);
                string ft = $"{rawCMD} " + "{{0}}";
                if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber)) return;
                PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
                {
                    HeaderFormat = $"“{Utils.Highlight(itemNameOrId)}”的查询结果" + "({0}/{1}):",
                    FooterFormat = $"输入{Utils.Highlight(ft)}查看更多".SFormat(Commands.Specifier)
                });
            }
        }

        /// <summary>
        /// 某个id后的20个物品
        /// </summary>
        /// <param name="args"></param>
        /// <param name="rawCMD"></param>
        /// <param name="startID"></param>
        static void ShowRange(CommandArgs args, string rawCMD, int startID)
        {
            // 最后一个物品的id
            if (startID == ItemID.Count - 1)
                startID--;

            Dictionary<int, string> dict = new();
            var HL = Utils.Highlight;
            var end = Math.Min(startID + 21, ItemID.Count);
            for (int i = startID + 1; i < end; i++)
            {
                dict.Add(i, Lang.GetItemNameValue(i));
            }

            if (dict.Count == 1)
            {
                // 单个结果
                args.Player.SendInfoMessage(Utils.ShowItem(dict.First().Key, dict.First().Value));
            }
            else
            {
                // 多个结果
                List<string> lines = Utils.WarpItemResult(dict);
                string s = $"{startID + 1}~{end - 1}";
                string ft = $"{rawCMD} " + "{{0}}";
                if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber)) return;
                PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
                {
                    HeaderFormat = $"id为“{HL(s)}”的物品" + "({0}/{1}):",
                    FooterFormat = $"输入{Utils.Highlight(ft)}查看更多".SFormat(Commands.Specifier)
                });
            }
        }

        /// <summary>
        /// 列表
        /// </summary>
        static void ListCommand(CommandArgs args)
        {
            string rawCMD = Utils.GetInputRawCMD(args, 1);
            //args.Parameters.RemoveAt(0);
            var HL = Utils.Highlight;
            var lines = Utils.WarpLines(DBHelper.Item.Keys(), 10);

            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber)) return;
            string ft = rawCMD + " {{0}}";
            PaginationTools.SendPage(args.Player, pageNumber, lines, new PaginationTools.Settings
            {
                HeaderFormat = $"自定义的关键词" + "({0}/{1}):",
                FooterFormat = $"输入{HL(ft)}查看更多".SFormat(Commands.Specifier)
            });

        }

        /// <summary>
        /// 添加关键词
        /// </summary>
        static void AddCommand(CommandArgs args)
        {
            string rawCMD = Utils.GetInputRawCMD(args, 1);
            args.Parameters.RemoveAt(0);
            var HL = Utils.Highlight;
            if (!args.Player.HasPermission(permissionSearchAdmin))
            {
                args.Player.SendErrorMessage($"你无权执行{HL(rawCMD)}指令！");
                return;
            }

            // 无参数
            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage($"请输入参数，例如：{HL("/search add 桶 3031 4820")}");
                return;
            }

            // 参数不足
            if (args.Parameters.Count < 2)
            {
                args.Player.SendInfoMessage($"参数不足，例如：{HL("/search add 桶 3031")}");
                return;
            }

            List<string> valids = new();
            List<string> invalids = new();

            for (int i = 1; i < args.Parameters.Count; i++)
            {
                var s = args.Parameters[i];
                //foreach (var s in args.Parameters[1].ToLowerInvariant().Split(','))
                //{
                if (Utils.IsItemID(s))
                {
                    valids.Add(s);
                }
                else
                {
                    var arr2 = s.Split("-");
                    if (arr2.Length > 1)
                    {
                        if (Utils.IsItemID(arr2[1]))
                        {
                            valids.Add(arr2[1]);
                        }
                        else
                        {
                            invalids.Add(s);
                        }
                    }
                    else
                    {
                        invalids.Add(s);
                    }
                }

            }

            if (!valids.Any())
            {
                args.Player.SendErrorMessage("查询结果填写错误，未添加任何关键词！");
                return;
            }

            string keywords = args.Parameters[0].ToLowerInvariant();
            string ids = string.Join(",", valids);
            DBHelper.Item.Add(keywords, ids);
            if (invalids.Any())
                args.Player.SendInfoMessage($"未添加 {HL(string.Join(",", invalids))}, 因为它们是无效的！");
            else
                args.Player.SendSuccessMessage("关键词添加成功！");
        }

        /// <summary>
        /// 移除关键词
        /// </summary>
        static void DelCommand(CommandArgs args)
        {
            string rawCMD = Utils.GetInputRawCMD(args, 1);
            args.Parameters.RemoveAt(0);
            var HL = Utils.Highlight;
            if (!args.Player.HasPermission(permissionSearchAdmin))
            {
                args.Player.SendErrorMessage($"你无权执行{HL(rawCMD)}指令！");
                return;
            }

            // 无参数
            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("请输入要删除的关键词");
                return;
            }

            var keywords = args.Parameters[0].ToLowerInvariant();
            if (DBHelper.Item.Contain(keywords))
            {
                DBHelper.Item.Delete(keywords);
                args.Player.SendSuccessMessage($"已移除关键词{HL(keywords)}！");
            }
            else
            {
                args.Player.SendInfoMessage("数据库里没有这个关键词，无需移除");
            }
        }

        /// <summary>
        /// 导入映射数据
        /// </summary>
        static void ImportCommand(CommandArgs args)
        {
            string rawCMD = Utils.GetInputRawCMD(args, 1);
            args.Parameters.RemoveAt(0);
            var HL = Utils.Highlight;
            if (!args.Player.HasPermission(permissionSearchAdmin))
            {
                args.Player.SendErrorMessage($"你无权执行{HL(rawCMD)}指令！");
                return;
            }

            var fileName = new FileInfo(WikiFile).Name;
            if (!File.Exists(WikiFile))
            {
                args.Player.SendErrorMessage($"请将wiki语言包{HL(fileName)}，放置于{HL(WorkDir)}目录下，然后再次导入。");
                return;
            }

            var _config = WikiJson.Load(WikiFile);
            var count = 0;

            Mapping.InitItemFields();
            var map = Mapping.ItemFields;
            foreach (var obj in _config.ItemName)
            {
                if (map.ContainsKey(obj.Key))
                {
                    var id = map[obj.Key];
                    DBHelper.Wiki.Add(id, obj.Value);
                    count++;
                }
            }
            if (count == 0)
                args.Player.SendInfoMessage("未导入任何数据，json文件可能不正确！");
            else
                args.Player.SendSuccessMessage($"成功导入{count}条数据！(*^▽^*)");
        }

    }
}
