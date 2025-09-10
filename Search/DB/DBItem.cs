using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Terraria;
using TShockAPI;

namespace Search.DB
{
    /// <summary>
    /// 物品表
    /// </summary>
    public class DBItem
    {
        private IDbConnection database;

        /// <summary>
        /// 物品表
        /// </summary>
        /// <param name="db"></param>
        /// <param name="exist">db文件是否存在，如果不存在则生成已知的关键词到初始数据库中</param>
        public DBItem(IDbConnection db, bool exist)
        {
            database = db;

            // Create table using raw SQL since SqlTableCreator is no longer available
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS Item (
                    Name TEXT PRIMARY KEY UNIQUE,
                    ID TEXT
                );";
            
            try
            {
                database.Open();
                using var cmd = database.CreateCommand();
                cmd.CommandText = createTableSql;
                cmd.ExecuteNonQuery();
                database.Close();
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Failed to create Item table: {ex}");
                if (database.State == System.Data.ConnectionState.Open)
                    database.Close();
            }

            if (!exist)
                Initialize();
        }

        /// <summary>
        /// 生成已知的关键词
        /// </summary>
        void Initialize()
        {
            List<string> li = new() {
                "桶", "3031,4820,5302,5364,205,206,207,1128,3032,4872,5303,5304,4827,4824,4825,4826",
                "磁铁", "2219,5010",
                "泰拉靴", "5000",
                "翱翔", "4989",   // 飞升之证-4989
                "召唤物", "560,43,70,1331,1133,5120,4988,556,544,557,5334,1293,2673,3601,267,4271,361,1315,2767,602,1844,1958",   // boss / 事件 召唤物
                "增强", "29,1291,109,3335,4382,5336,5326,5043,5337,5338,5339,5342,5341,5340,5343,5289",
                "任务鱼", string.Join(",", Main.anglerQuestItemNetIDs),
                "模型", "498,1989,3977,2699,3270,3202",
                "银行", "87,3213,5098,346,3813,4076,4131,5325",
                "团队", "1969,1982,3621,3622,3633,3634,3635,3636,3637,3638,3639,3640,3641,3642",
                "鞋", "128,54,1579,3200,4055,405,898,950,1862,5000,863,907,908,3017,3990,3993,4822,4874",
            };

            // 批量插入到数据库
            database.Open();
            var cmd = database.CreateCommand();
            var transaction = database.BeginTransaction();
            cmd.Transaction = transaction;
            try
            {
                for (var i = 0; i < li.Count; i++)
                {
                    cmd.CommandText = string.Format("INSERT INTO Item (Name, ID) VALUES (\"{0}\", \"{1}\");", li[i], li[i + 1]);
                    cmd.ExecuteNonQuery();
                    i++;
                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TShock.Log.Error(ex.ToString());
            }
            database.Close();
        }

        /// <summary>
        /// 获得关键词结果
        /// </summary>
        /// <param name="keywords">关键词</param>
        /// <returns>由数字和英文逗号组成的字符串，为空表示未找到</returns>
        public string GetValue(string keywords)
        {
            try
            {
                database.Open();
                using var cmd = database.CreateCommand();
                cmd.CommandText = "SELECT * FROM Item WHERE Name = @name;";
                var param = cmd.CreateParameter();
                param.ParameterName = "@name";
                param.Value = keywords;
                cmd.Parameters.Add(param);
                
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string s = reader["ID"].ToString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        database.Close();
                        return s;
                    }
                }
                database.Close();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                if (database.State == System.Data.ConnectionState.Open)
                    database.Close();
            }
            return "";
        }

        /// <summary>
        /// 获得物品关键词列表
        /// </summary>
        public List<string> Keys()
        {
            List<string> li = new();
            try
            {
                database.Open();
                using var cmd = database.CreateCommand();
                cmd.CommandText = "SELECT * FROM Item;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    li.Add(reader["Name"].ToString());
                }
                database.Close();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                if (database.State == System.Data.ConnectionState.Open)
                    database.Close();
            }
            return li;
        }

        /// <summary>
        /// 关键词是否存在
        /// </summary>
        public bool Contain(string keywords)
        {
            try
            {
                database.Open();
                using var cmd = database.CreateCommand();
                cmd.CommandText = "SELECT * FROM Item WHERE Name = @name;";
                var param = cmd.CreateParameter();
                param.ParameterName = "@name";
                param.Value = keywords;
                cmd.Parameters.Add(param);
                
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    if (!string.IsNullOrEmpty(reader["Name"].ToString()))
                    {
                        database.Close();
                        return true;
                    }
                }
                database.Close();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                if (database.State == System.Data.ConnectionState.Open)
                    database.Close();
            }
            return false;
        }

        /// <summary>
        /// 添加/更新 关键词
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="ids"></param>
        public void Add(string keywords, string ids)
        {
            if (Contain(keywords))
            {
                // 追加更新
                string s = GetValue(keywords);
                if (!string.IsNullOrEmpty(s))
                {
                    var arr1 = s.Split(",");
                    var arr2 = ids.Split(",");
                    var arr3 = arr1.Concat(arr2).Distinct();
                    ids = string.Join(",", arr3);
                }
                Update(keywords, ids);
            }
            else
            {
                // 插入数据
                Insert(keywords, ids);
            }
        }


        /// <summary>
        /// 更新
        /// </summary>
        void Update(string keywords, string ids)
        {
            try
            {
                database.Open();
                using var cmd = database.CreateCommand();
                cmd.CommandText = "UPDATE Item SET ID = @id WHERE Name = @name;";
                
                var nameParam = cmd.CreateParameter();
                nameParam.ParameterName = "@name";
                nameParam.Value = keywords;
                cmd.Parameters.Add(nameParam);
                
                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = ids;
                cmd.Parameters.Add(idParam);
                
                cmd.ExecuteNonQuery();
                database.Close();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                if (database.State == System.Data.ConnectionState.Open)
                    database.Close();
            }
        }

        /// <summary>
        /// 插入
        /// </summary>
        void Insert(string keywords, string ids)
        {
            try
            {
                database.Open();
                using var cmd = database.CreateCommand();
                cmd.CommandText = "INSERT INTO Item (Name, ID) VALUES (@name, @id);";
                
                var nameParam = cmd.CreateParameter();
                nameParam.ParameterName = "@name";
                nameParam.Value = keywords;
                cmd.Parameters.Add(nameParam);
                
                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = ids;
                cmd.Parameters.Add(idParam);
                
                cmd.ExecuteNonQuery();
                database.Close();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                if (database.State == System.Data.ConnectionState.Open)
                    database.Close();
            }
        }

        /// <summary>
        /// 删除关键词数据
        /// </summary>
        public void Delete(string keywords)
        {
            try
            {
                database.Open();
                using var cmd = database.CreateCommand();
                cmd.CommandText = "DELETE FROM Item WHERE Name = @name;";
                
                var param = cmd.CreateParameter();
                param.ParameterName = "@name";
                param.Value = keywords;
                cmd.Parameters.Add(param);
                
                cmd.ExecuteNonQuery();
                database.Close();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                if (database.State == System.Data.ConnectionState.Open)
                    database.Close();
            }
        }
    }
}
