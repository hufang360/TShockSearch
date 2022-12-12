using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Search
{
    /// <summary>
    /// 配置文件
    /// </summary>
    public class Config
    {
        /// <summary>
        /// 查一查指令别名
        /// </summary>
        public string[] aliasSearch = { "s", "查一查", "查" };

        /// <summary>
        /// 合成表指令别名
        /// </summary>
        public string[] aliasRecipe = { "r", "合成表", "合成", "合", "制作" };


        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static Config Load(string path)
        {
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
            }
            else
            {
                var c = new Config();
                File.WriteAllText(path, JsonConvert.SerializeObject(c, Formatting.Indented));
                return c;
            }
        }
    }



    /// <summary>
    /// wiki语言包文件
    /// </summary>
    public class WikiJson
    {
        /// <summary>
        /// 物品名称
        /// </summary>
        public Dictionary<string, string> ItemName = new();

        /// <summary>
        /// 加载json文件
        /// </summary>
        public static WikiJson Load(string path)
        {
            return JsonConvert.DeserializeObject<WikiJson>(File.ReadAllText(path), new JsonSerializerSettings()
            {
                Error = (sender, error) => error.ErrorContext.Handled = true
            });
        }
    }

}