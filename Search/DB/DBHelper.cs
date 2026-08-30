using Microsoft.Data.Sqlite;
using System.IO;

namespace Search.DB
{
    /// <summary>
    /// 数据库辅助对象
    /// </summary>
    public static class DBHelper
    {
        /// <summary>
        /// Item表
        /// </summary>
        public static DBItem Item;

        /// <summary>
        /// Wiki表
        /// </summary>
        public static DBWiki Wiki;

        public static void Connect(string sql)
        {
            bool exist = File.Exists(sql);
            var connStr = $"Data Source={sql}";

            Item = new DBItem(connStr, exist);
            Wiki = new DBWiki(connStr, exist);
        }
    }
}
