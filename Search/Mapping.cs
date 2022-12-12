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
                {36, Language.GetTextValue("ItemName.WorkBench")},  // 工作台
                {32, Language.GetTextValue("MapObject.Table")}, // 桌子
                {34, Language.GetTextValue("MapObject.Chair")}, // 椅子
                {2827, Language.GetTextValue("MapObject.Sink")}, // 水槽
                {354, Language.GetTextValue("ItemName.Bookcase")}, // 书架
                {33, Language.GetTextValue("ItemName.Furnace")},    // 熔炉
                {221, Language.GetTextValue("ItemName.Hellforge")}, // 地狱熔炉
                {35, Language.GetTextValue("MapObject.Anvil")},     // 铁砧
                {716, Language.GetTextValue("MapObject.Anvil")},     // 铅砧
                // 放置的瓶子，没有对应的物品
                {3000, Language.GetTextValue("ItemName.AlchemyTable")}, // 炼药桌
                {363, Language.GetTextValue("ItemName.Sawmill")}, // 锯木机
                {332, Language.GetTextValue("ItemName.Loom")}, // 织布机
                {345, Language.GetTextValue("ItemName.CookingPot")}, // 烹饪锅
                {1791, Language.GetTextValue("ItemName.Cauldron")}, // 大锅
                {398, Language.GetTextValue("ItemName.TinkerersWorkshop")}, // 工匠作坊
                {1430, Language.GetTextValue("ItemName.ImbuingStation")}, // 灌注站
                {1120, Language.GetTextValue("ItemName.DyeVat")}, // 染缸
                {2172, Language.GetTextValue("ItemName.HeavyWorkBench")}, // 重型工作台
                // 恶魔祭坛和猩红祭坛，没有对应的物品
                {525, Language.GetTextValue("ItemName.MythrilAnvil")}, // 秘银砧
                {1220, Language.GetTextValue("ItemName.OrichalcumAnvil")}, // 山铜砧
                {524, Language.GetTextValue("ItemName.AdamantiteForge")}, // 精金熔炉
                {1221, Language.GetTextValue("ItemName.TitaniumForge")}, // 钛金熔炉
                {487, Language.GetTextValue("ItemName.CrystalBall")}, // 水晶球
                {1551, Language.GetTextValue("ItemName.Autohammer")}, // 自动锤炼机
                {3549, Language.GetTextValue("ItemName.LunarCraftingStation")}, // 远古操纵机
                {352, Language.GetTextValue("ItemName.Keg")}, // 酒桶
                {5008, Language.GetTextValue("ItemName.TeaKettle")}, // 茶壶
                {995, Language.GetTextValue("ItemName.BlendOMatic")}, // 搅拌机
                {996, Language.GetTextValue("ItemName.MeatGrinder")}, // 绞肉机
                {2192, Language.GetTextValue("ItemName.BoneWelder")}, // 骨头焊机
                {2194, Language.GetTextValue("ItemName.GlassKiln")}, // 玻璃窑
                {2204, Language.GetTextValue("ItemName.HoneyDispenser")}, // 蜂蜜分配机
                {2198, Language.GetTextValue("ItemName.IceMachine")}, // 冰雪机
                {2196, Language.GetTextValue("ItemName.LivingLoom")}, // 生命木织机
                {2197, Language.GetTextValue("ItemName.SkyMill")}, // 天磨
                {998, Language.GetTextValue("ItemName.Solidifier")}, // 固化机
                {4142, Language.GetTextValue("ItemName.LesionStation")}, // 衰变室
                {2193, Language.GetTextValue("ItemName.FleshCloningVaat")}, // 血肉克隆台
                {2203, Language.GetTextValue("ItemName.SteampunkBoiler")}, // 蒸汽朋克锅炉
                {2195, Language.GetTextValue("ItemName.LihzahrdFurnace")}, // 丛林蜥蜴熔炉
                {966, Language.GetTextValue("ItemName.Campfire")}, // 篝火
                {997, Language.GetTextValue("ItemName.Extractinator")}, // 提炼机
                {5296, Language.GetTextValue("ItemName.ChlorophyteExtractinator")}, // 叶绿素提炼机
            };
            return _craft;
        }



        /// <summary>
        /// 初始化地图集显示名称（可以获得部分图格的名称）
        /// </summary>
        public static void BuildMapAtlas()
        {
            if (MapHelper.tileLookup == null)
            {
                bool status = Main.dedServ;
                Main.dedServ = false;
                MapHelper.Initialize();
                Main.dedServ = status;

                // dedServ为假时，不执行 Main 会执行 MapHelper.Initialize();
                // 执行 MapHelper.Initialize(); 时会执行 Lang.BuildMapAtlas();
                // 但是执行 Lang.BuildMapAtlas(); 遇到dedServ为真时，会不执行
            }
        }

    }

}