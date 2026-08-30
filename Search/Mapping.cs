using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;

namespace Search
{
    /// <summary>
    /// 数据映射
    /// </summary>
    public class Mapping
    {
        /// <summary>
        /// 物品内部名称(名称,id)
        /// 默认无任何内容，需调用 InitItemFields 方法获取
        /// </summary>
        public static Dictionary<string, short> ItemFields = new();

        /// <summary>
        /// 物品内部名称(名称,id)
        /// </summary>
        public static void InitItemFields()
        {
            if (!ItemFields.Any())
            {
                var fields = typeof(ItemID).GetFields(BindingFlags.Static | BindingFlags.Public).Where(obj => obj.FieldType == typeof(short)).ToArray();
                foreach (var field in fields)
                {
                    if (!ItemFields.ContainsKey(field.Name) && field.GetValue(field) != null)
                    {
                        ItemFields.Add(field.Name, (short)field.GetValue(field));
                    }
                }
            }
        }



        /// <summary>
        /// 制作站物品映射
        /// </summary>
        static Dictionary<int, string> _craft;

        /// <summary>
        /// 制作站物品映射
        /// </summary>
        public static Dictionary<int, string> GetCraftingStations()
        {
            _craft ??= new Dictionary<int, string> {
                {36, LangText("ItemName.WorkBench")},  // 工作台
                {32, LangText("MapObject.Table")}, // 桌子
                {34, LangText("MapObject.Chair")}, // 椅子
                {2827, LangText("MapObject.Sink")}, // 水槽
                {354, LangText("ItemName.Bookcase")}, // 书架
                {33, LangText("ItemName.Furnace")},    // 熔炉
                {221, LangText("ItemName.Hellforge")}, // 地狱熔炉
                {35, LangText("MapObject.Anvil")},     // 铁砧
                {716, LangText("MapObject.Anvil")},     // 铅砧
                // 放置的瓶子，没有对应的物品
                {3000, LangText("ItemName.AlchemyTable")}, // 炼药桌
                {363, LangText("ItemName.Sawmill")}, // 锯木机
                {332, LangText("ItemName.Loom")}, // 织布机
                {345, LangText("ItemName.CookingPot")}, // 烹饪锅
                {1791, LangText("ItemName.Cauldron")}, // 大锅
                {398, LangText("ItemName.TinkerersWorkshop")}, // 工匠作坊
                {1430, LangText("ItemName.ImbuingStation")}, // 灌注站
                {1120, LangText("ItemName.DyeVat")}, // 染缸
                {2172, LangText("ItemName.HeavyWorkBench")}, // 重型工作台
                // 恶魔祭坛和猩红祭坛，没有对应的物品
                {525, LangText("ItemName.MythrilAnvil")}, // 秘银砧
                {1220, LangText("ItemName.OrichalcumAnvil")}, // 山铜砧
                {524, LangText("ItemName.AdamantiteForge")}, // 精金熔炉
                {1221, LangText("ItemName.TitaniumForge")}, // 钛金熔炉
                {487, LangText("ItemName.CrystalBall")}, // 水晶球
                {1551, LangText("ItemName.Autohammer")}, // 自动锤炼机
                {3549, LangText("ItemName.LunarCraftingStation")}, // 远古操纵机
                {352, LangText("ItemName.Keg")}, // 酒桶
                {5008, LangText("ItemName.TeaKettle")}, // 茶壶
                {995, LangText("ItemName.BlendOMatic")}, // 搅拌机
                {996, LangText("ItemName.MeatGrinder")}, // 绞肉机
                {2192, LangText("ItemName.BoneWelder")}, // 骨头焊机
                {2194, LangText("ItemName.GlassKiln")}, // 玻璃窑
                {2204, LangText("ItemName.HoneyDispenser")}, // 蜂蜜分配机
                {2198, LangText("ItemName.IceMachine")}, // 冰雪机
                {2196, LangText("ItemName.LivingLoom")}, // 生命木织机
                {2197, LangText("ItemName.SkyMill")}, // 天磨
                {998, LangText("ItemName.Solidifier")}, // 固化机
                {4142, LangText("ItemName.LesionStation")}, // 衰变室
                {2193, LangText("ItemName.FleshCloningVaat")}, // 血肉克隆台
                {2203, LangText("ItemName.SteampunkBoiler")}, // 蒸汽朋克锅炉
                {2195, LangText("ItemName.LihzahrdFurnace")}, // 丛林蜥蜴熔炉
                {966, LangText("ItemName.Campfire")}, // 篝火
                {997, LangText("ItemName.Extractinator")}, // 提炼机
                {5296, LangText("ItemName.ChlorophyteExtractinator")}, // 叶绿素提炼机
            };
            return _craft;
        }



        /// <summary>
        /// 初始化地图集显示名称（可以获得部分图格的名称）
        /// 加锁保证只执行一次，避免并发重复初始化（Main.dedServ 翻转是全局副作用）
        /// </summary>
        static readonly object _mapLock = new();

        static bool _mapBuilt = false;

        public static void BuildMapAtlas()
        {
            if (_mapBuilt) return;
            lock (_mapLock)
            {
                if (_mapBuilt) return;

                if (MapHelper.tileLookup == null)
                {
                    bool status = Main.dedServ;
                    Main.dedServ = false;
                    try
                    {
                        MapHelper.Initialize();
                        // dedServ为假时，不执行 Main 会执行 MapHelper.Initialize();
                        // 执行 MapHelper.Initialize(); 时会执行 Lang.BuildMapAtlas();
                        // 但是执行 Lang.BuildMapAtlas(); 遇到dedServ为真时，会不执行
                    }
                    finally
                    {
                        Main.dedServ = status;
                    }
                }

                _mapBuilt = true;
            }
        }

        /// <summary>
        /// 安全获取本地化文本，键不存在或异常时返回空串
        /// </summary>
        static string LangText(string key)
        {
            try
            {
                var value = Language.GetTextValue(key);
                return string.IsNullOrEmpty(value) ? "" : value;
            }
            catch
            {
                return "";
            }
        }

    }

}