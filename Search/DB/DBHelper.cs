using Microsoft.Data.Sqlite;
using System.Data;
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
            IDbConnection db = new SqliteConnection(string.Format("Data Source={0}", sql));

            Item = new DBItem(db, exist);
            Wiki = new DBWiki(db, exist);
        }
    }
}
