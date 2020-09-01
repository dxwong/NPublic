using NPublic.Logger;
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Transactions;

namespace UPublic.Dapper
{
    /// <summary>
    /// 原生数据库操作
    /// </summary>
    public static class DatabaseManager
    {
        /// <summary>
        /// 创建Database对象,可自动识别mysql；默认为MSSQL
        /// </summary>
        /// <param name="ConnectionString"></param>
        /// <param name="WriteLog"></param>
        /// <returns></returns>
        public static Database CreateDatabase(string ConnectionString)
        {
            if (ConnectionString.ToUpper().Contains("HOST"))
            {
                return CreateDatabase(ConnectionString, DBType.MySql);
            }
            
            return CreateDatabase(ConnectionString, DBType.MSSQL);
        }

        /// <summary>
        /// 创建Database对象
        /// </summary>
        public static Database CreateDatabase(string strconn, DBType DBType)
        {
            //strconn = UBase.DES_Decrypt(strconn);//数据库连接字符串，DES解密
            #region Sqllite
            if (DBType == DBType.SqlLite)
            {
                strconn = string.Format("Data Source={0}", strconn);
                System.Data.SQLite.SQLiteDataAdapter mysqlda = new System.Data.SQLite.SQLiteDataAdapter();
                mysqlda.SelectCommand = new System.Data.SQLite.SQLiteCommand();
                mysqlda.SelectCommand.Connection = new SQLiteConnection(strconn);
                return new Database(mysqlda, DBType.SqlLite);
            }
            #endregion

            #region MYSQL
            if (DBType == DBType.MySql)
            {            
                //Host=127.0.0.1;UserName=root;Password=123;Database=huizhan;Port=4002;CharSet=utf8;Allow Zero Datetime=true;
                MySql.Data.MySqlClient.MySqlDataAdapter mysqlda = new MySql.Data.MySqlClient.MySqlDataAdapter();
                mysqlda.SelectCommand = new MySql.Data.MySqlClient.MySqlCommand();
                mysqlda.SelectCommand.Connection = new MySql.Data.MySqlClient.MySqlConnection(strconn);
                return new Database(mysqlda, DBType.MySql);
            }
            #endregion

            #region MSSQL
            if (DBType == DBType.MSSQL)
            {
                SqlDataAdapter sqlda = new SqlDataAdapter();
                sqlda.SelectCommand = new SqlCommand();
                sqlda.SelectCommand.Connection = new SqlConnection(strconn);
                return new Database(sqlda, DBType.MSSQL);
            }
            #endregion

            #region Oracle
            if (DBType == DBType.Oracle)
            {
                MySql.Data.MySqlClient.MySqlDataAdapter mysqlda = new MySql.Data.MySqlClient.MySqlDataAdapter();
                mysqlda.SelectCommand = new MySql.Data.MySqlClient.MySqlCommand();
                mysqlda.SelectCommand.Connection = new MySql.Data.MySqlClient.MySqlConnection(strconn);
                return new Database(mysqlda, DBType.MySql);
            }
            #endregion
            return null;
        }
    }
    public enum DBType
    {
        MSSQL,
        MySql,
        SqlLite,
        Oracle
    }

    /// <summary>
    /// 执行主要操作的类
    /// </summary>
    public class Database
    {
        private DbDataAdapter mDataAdapter;
        private DbCommand mCommand;
        public DBType thisDBType;
        private readonly NLog log;

        #region Database
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="DDA">获得一个实例化了的DbDataAdapter的派生类</param>
        public Database(DbDataAdapter DDA, DBType DBType)
        {
            mDataAdapter = DDA;
            mCommand = DDA.SelectCommand;
            thisDBType = DBType;
            log = NLog.Init();
        }
        #endregion

        /// <summary>
        /// 数据库连接状态
        /// </summary>
        /// <returns></returns>
        public ConnectionState State()
        {
            try
            {
                mCommand.Connection.Open();
                return mDataAdapter.SelectCommand.Connection.State;
            }
            catch
            {

            }
            finally
            {
                mCommand.Connection.Close();
            }
            return ConnectionState.Closed;
        }


        public DbCommand GetSqlStringCommand(string commandText)
        {
            if (IsProcedure(commandText)) { mCommand.CommandType = CommandType.StoredProcedure; } else { mCommand.CommandType = CommandType.Text; }

            mCommand.CommandText = commandText;
            return mCommand;
        }


        #region ExecuteNonQuery
        /// <summary>
        /// 执行insert命令返回新增的第一行第一列的值,即自增ID,其他返回影响的记录数
        /// </summary>
        /// <param name="SQLText">SQL语句</param>
        /// <returns>新增的第一行第一列的值</returns>
        public int ExecuteNonQuery(string SQLText)
        {
            try
            {
                if (IsProcedure(SQLText)) { mCommand.CommandType = CommandType.StoredProcedure; } else { mCommand.CommandType = CommandType.Text; }
                mCommand.CommandTimeout = 5;
                mCommand.Connection.Open();
                if (SQLText.ToLower().IndexOf("update") > -1 && thisDBType == DBType.MySql) { SQLText = SQLText.Replace("\\", "\\\\"); }
                if (SQLText.ToLower().IndexOf("insert") > -1)
                {
                    if (thisDBType == DBType.MSSQL) { SQLText += " SELECT @@IDENTITY"; }
                    if (thisDBType == DBType.MySql) { SQLText = SQLText.Replace("\\", "\\\\"); SQLText += " ;SELECT LAST_INSERT_ID()"; }
                    mCommand.CommandText = SQLText;
                    object o = mCommand.ExecuteScalar();//第一行第一列的值如果是非int,ExecuteScalar会返回null
                    if (o == null) { return 0; }
                    return Convert.ToInt32(o);
                }
                else
                {
                    mCommand.CommandText = SQLText;
                    return mCommand.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                log.Write(e.ToString(), "ExecuteNonQuery", "", LogLevel.Error);
                return -1;
            }
            finally
            {
                mCommand.Connection.Close();
                ClearParameters();
            }
        }
        public int ExecuteNonQuery(DbCommand commend)
        {
            try
            {
                commend.Connection.Open();
                return commend.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Write(e.ToString(), "ExecuteNonQuery", "", LogLevel.Error);
                return -1;
            }
            finally
            {
                mCommand.Connection.Close();
                ClearParameters();
            }
        }
        #endregion

        #region ExecuteNonQuery out
        /// <summary>
        /// 执行insert命令返回新增的第一行第一列的ID值,其他返回影响的记录数
        /// </summary>
        /// <param name="SQLText"></param>
        /// <param name="errTip"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string SQLText, out string errTip)
        {
            try
            {
                errTip = null;
                if (IsProcedure(SQLText)) { mCommand.CommandType = CommandType.StoredProcedure; } else { mCommand.CommandType = CommandType.Text; }
                mCommand.CommandTimeout = 5;
                mCommand.Connection.Open();
                if (SQLText.ToLower().IndexOf("insert") > -1)
                {
                    if (thisDBType == DBType.MSSQL) { SQLText += " ;SELECT @@IDENTITY"; }
                    if (thisDBType == DBType.MySql) { SQLText += " ;SELECT LAST_INSERT_ID()"; }
                    mCommand.CommandText = SQLText;
                    object o = mCommand.ExecuteScalar();//第一行第一列的值如果是非int,ExecuteScalar会返回null
                    if (o == null) { return 0; }
                    return Convert.ToInt32(o);
                }
                else
                {
                    mCommand.CommandText = SQLText;
                    return mCommand.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                errTip = e.Message;
                log.Write(e.ToString(), "ExecuteNonQuery", "", LogLevel.Error);
                return -1;
            }
            finally
            {
                mCommand.Connection.Close();
                ClearParameters();
            }
        }
        #endregion

        #region ExecuteNonQueryAccess
        /// <summary>
        /// 执行Access数据库
        /// </summary>
        /// <param name="SQLText"></param>
        /// <returns></returns>
        public int ExecuteNonQueryAccess(string SQLText)
        {
            try
            {
                if (IsProcedure(SQLText)) { mCommand.CommandType = CommandType.StoredProcedure; } else { mCommand.CommandType = CommandType.Text; }
                mCommand.CommandText = SQLText;
                mCommand.CommandTimeout = 5;
                mCommand.Connection.Open();
                return mCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                log.Write(e.ToString(), "ExecuteNonQuery", "", LogLevel.Error);
                return -1;
            }
            finally
            {
                mCommand.Connection.Close();
                ClearParameters();
            }
        }

        #region ExecuteNonQueryAccess out

        /// <summary>
        /// 执行Access数据库
        /// </summary>
        /// <param name="SQLText"></param>
        /// <returns></returns>
        public int ExecuteNonQueryAccess(string SQLText, out string errTip)
        {
            try
            {
                errTip = null;
                if (IsProcedure(SQLText)) { mCommand.CommandType = CommandType.StoredProcedure; } else { mCommand.CommandType = CommandType.Text; }
                mCommand.CommandText = SQLText;
                mCommand.CommandTimeout = 5;
                mCommand.Connection.Open();
                return mCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                errTip = e.Message;
                log.Write(e.ToString(), "ExecuteNonQuery", "", LogLevel.Error);
                return -1;
            }
            finally
            {
                mCommand.Connection.Close();
                ClearParameters();
            }
        }
        #endregion
        #endregion

        #region IsProcedure
        /// <summary>
        /// 判断一个stirng是否为储存过程
        /// </summary>
        /// <param name="SQLText">目标string</param>
        /// <returns>返回是否为储存过程的调用</returns>
        private bool IsProcedure(string SQLText)
        {
            if (SQLText.Contains(" "))
            {
                string[] tmp;
                tmp = SQLText.Split(' ');
                if (tmp[0].ToUpper() == "EXECUTE" || tmp[0].ToUpper() == "EXEC")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        #endregion



        #region 事务执行一组SQL语句,多个Sql用英文逗号隔开
        /// <summary>
        /// 事务执行一组SQL语句,多个Sql用英文逗号隔开
        /// </summary>
        /// <param name="SqlStrings">SQL语句组</param>
        /// <returns>bool</returns>
        public bool ExecuteNonQuery(string[] SqlStrings)
        {
            bool success = true;
            using (TransactionScope scope = new TransactionScope())
            {
                foreach (String sql in SqlStrings)
                {
                    int n = ExecuteNonQuery(sql);
                    if (n == -1) { success = false; break; }
                }
                if (success) { scope.Complete(); }
            }
            return success;
        }

        /// <summary>
        /// 事务执行一组SQL语句,多个Sql用英文逗号隔开
        /// </summary>
        /// <param name="SqlStrings"></param>
        /// <param name="errTip"></param>
        /// <returns></returns>
        public bool ExecuteNonQuery(string[] SqlStrings, out string errTip)
        {
            errTip = null;
            bool success = true;
            using (TransactionScope scope = new TransactionScope())
            {
                foreach (String sql in SqlStrings)
                {
                    int n = ExecuteNonQuery(sql, out errTip);
                    if (n == -1) { success = false; break; }
                }
                if (success) { scope.Complete(); }
            }
            return success;

            //errTip = null;
            //bool success = true;
            //mCommand.Connection.Open();
            //DbTransaction trans = mCommand.Connection.BeginTransaction();
            //mCommand.Transaction = trans;
            //mCommand.CommandTimeout = 5;
            //try
            //{
            //    foreach (String str in SqlStrings)
            //    {
            //        mCommand.CommandText = str;
            //       int n= mCommand.ExecuteNonQuery();
            //    }
            //    trans.Commit();
            //}
            //catch(ExecutionEngineException e)
            //{
            //    errTip = e.Message;
            //    success = false;
            //    trans.Rollback();
            //}
            //finally
            //{
            //    mCommand.Connection.Close();
            //    ClearParameters();
            //}
            //return success;
        }
        #endregion

        #region 使用哈希表插入一条记录。

        /// <summary>
        /// 使用哈希表插入一条记录
        /// </summary>
        /// <param name="TableName">表名</param>
        /// <param name="Cols">哈西表，键值为字段名，值为字段值</param>
        /// <returns>影响记录</returns>
        public int ExecuteNonQuery(String TableName, Hashtable Cols)
        {
            int Count = 0;

            if (Cols.Count <= 0)
            {
                return Count;
            }

            String Fields = " (";
            String Values = " Values(";
            foreach (DictionaryEntry item in Cols)
            {
                if (Count != 0)
                {
                    Fields += ",";
                    Values += ",";
                }
                Fields += item.Key.ToString();
                if (item.Value.GetType().ToString() == "System.String" || item.Value.GetType().ToString() == "System.DateTime")
                {
                    Values += "'" + item.Value.ToString().Replace("'", "''") + "'";
                }
                else
                {
                    Values += item.Value.ToString().Replace("'", "''");
                }
                Count++;
            }
            Fields += ")";
            Values += ")";

            String SqlString = "Insert into " + TableName + "  " + Fields + Values;
            try
            {
                return ExecuteNonQuery(SqlString);
            }
            catch (Exception e)
            {
                log.Write(e.ToString(), "ExecuteNonQueryHashtable", "", LogLevel.Error);
                return -1;
            }
        }

        /// <summary>
        /// 使用哈希表插入一条记录
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="Cols"></param>
        /// <param name="errTip"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(String TableName, Hashtable Cols, out string errTip)
        {
            errTip = null;
            int Count = 0;

            if (Cols.Count <= 0)
            {
                return Count;
            }

            String Fields = " (";
            String Values = " Values(";
            foreach (DictionaryEntry item in Cols)
            {
                if (Count != 0)
                {
                    Fields += ",";
                    Values += ",";
                }
                Fields += item.Key.ToString();
                if (item.Value.GetType().ToString() == "System.String" || item.Value.GetType().ToString() == "System.DateTime")
                {
                    Values += "'" + item.Value.ToString().Replace("'", "''") + "'";
                }
                else
                {
                    Values += item.Value.ToString().Replace("'", "''");
                }
                Count++;
            }
            Fields += ")";
            Values += ")";

            String SqlString = "Insert into " + TableName + "  " + Fields + Values;
            try
            {
                return ExecuteNonQuery(SqlString, out errTip);
            }
            catch (Exception e)
            {
                errTip = e.Message;
                log.Write(e.ToString(), "ExecuteNonQueryHashtable", "", LogLevel.Error);
                return -1;
            }
        }
        #endregion

        #region 哈希表插入记录，返回执行的SQL语句
        /// <summary>
        /// 哈希表插入记录，返回执行的SQL语句
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="Cols"></param>
        /// <returns></returns>
        public string ExecuteNonQuerySQL(String TableName, Hashtable Cols)
        {
            int Count = 0;

            if (Cols.Count <= 0)
            {
                return Count.ToString();
            }

            String Fields = " (";
            String Values = " Values(";
            foreach (DictionaryEntry item in Cols)
            {
                if (Count != 0)
                {
                    Fields += ",";
                    Values += ",";
                }
                Fields += item.Key.ToString();
                if (item.Value.GetType().ToString() == "System.String" || item.Value.GetType().ToString() == "System.DateTime")
                {
                    Values += "'" + item.Value.ToString().Replace("'", "''") + "'";
                }
                else
                {
                    Values += item.Value.ToString().Replace("'", "''");
                }
                Count++;
            }
            Fields += ")";
            Values += ")";

            String SqlString = "Insert into " + TableName + "  " + Fields + Values;//with(Tablock)

            return SqlString;
        }


        #endregion

        #region 使用哈希表更新一个数据表
        /// <summary>
        /// 使用哈希表更新一个数据表
        /// </summary>
        /// <param name="TableName">表名</param>
        /// <param name="Cols">哈西表，键值为字段名，值为字段值</param>
        /// <param name="Where">Where子句</param>
        /// <returns>影响记录</returns>
        public int ExecuteNonQuery(String TableName, Hashtable Cols, String Where)
        {
            int Count = 0;
            if (Cols.Count <= 0)
            {
                return Count;
            }
            String Fields = " ";
            foreach (DictionaryEntry item in Cols)
            {
                if (Count != 0)
                {
                    Fields += ",";
                }
                Fields += item.Key.ToString();
                Fields += "=";
                Fields += "'" + item.Value.ToString().Replace("'", "''") + "'";
                Count++;
            }
            Fields += " ";

            String SqlString = "Update " + TableName + " Set " + Fields + Where;

            return ExecuteNonQuery(SqlString);
        }

        /// <summary>
        /// 使用哈希表更新一个数据表
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="Cols"></param>
        /// <param name="Where"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(String TableName, Hashtable Cols, String Where, out string errTip)
        {
            errTip = null;
            int Count = 0;
            if (Cols.Count <= 0)
            {
                return Count;
            }
            String Fields = " ";
            foreach (DictionaryEntry item in Cols)
            {
                if (Count != 0)
                {
                    Fields += ",";
                }
                Fields += item.Key.ToString();
                Fields += "=";
                Fields += "'" + item.Value.ToString().Replace("'", "''") + "'";
                Count++;
            }
            Fields += " ";

            String SqlString = "Update " + TableName + " Set " + Fields + Where;

            return ExecuteNonQuery(SqlString, out errTip);
        }
        #endregion


        #region ExecuteReader
        /// <summary>
        /// 执行数据库命令返回DataReader
        /// </summary>
        /// <param name="SQLText">SQL命令</param>
        /// <returns>返回DataReader</returns>
        //public DbDataReader ExecuteReader(string SQLText)
        //{
        //    try
        //    {
        //        if (IsProcedure(SQLText)) { mCommand.CommandType = CommandType.StoredProcedure; } else { mCommand.CommandType = CommandType.Text; }
        //        mCommand.CommandText = SQLText;
        //        mCommand.CommandTimeout = 5;
        //        mCommand.Connection.Open();
        //        return mCommand.ExecuteReader(CommandBehavior.CloseConnection);
        //    }
        //    catch { return null; }
        //} 
        #endregion

        #region IsHaveRow
        public bool IsHaveRow(string sql)
        {
            try
            {
                if (thisDBType == DBType.MySql) { sql = sql.Replace("\\", "\\\\"); }
                DataSet ds = ExecuteDataSet(sql);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }
        #endregion

        #region ExecuteScalar
        /// <summary>
        /// 执行统计的方法
        /// </summary>
        /// <param name="SQLText">SQL命令</param>
        /// <returns>返回object对象代表统计数据或者结果的第一列第一行</returns>
        public object ExecuteScalar(string SQLText)
        {
            if (IsProcedure(SQLText)) { mCommand.CommandType = CommandType.StoredProcedure; } else { mCommand.CommandType = CommandType.Text; }
            mCommand.CommandText = SQLText;
            mCommand.CommandTimeout = 5;
            try
            {
                mCommand.Connection.Open();
                return mCommand.ExecuteScalar();
            }
            catch (Exception e)
            {
                log.Write(e.ToString(), "ExecuteScalar", "", LogLevel.Error);
                return "-1";
            }
            finally
            {
                mCommand.Connection.Close();
                ClearParameters();
            }
        }
        #endregion

        #region ExecuteDataSet
        /// <summary>
        /// 执行查询返回填充的DataSet对象
        /// </summary>
        /// <param name="SQLText">SQl命令</param>
        /// <param name="VisualTableName">虚拟表名</param>
        /// <param name="StartIndex">制定返回多少行以后的数据</param>
        /// <param name="Count">制定总共返回多少行</param>
        /// <returns>返回按要求填充了的DataSet</returns>
        public DataSet ExecuteDataSet(string SQLText, string VisualTableName, int StartIndex, int Count)
        {
            try
            {
                DataSet ds = new DataSet();
                if (IsProcedure(SQLText)) { mCommand.CommandType = CommandType.StoredProcedure; } else { mCommand.CommandType = CommandType.Text; }
                mCommand.CommandText = SQLText;
                mCommand.CommandTimeout = 5;

                //mCommand.Connection.Open();
                mDataAdapter.Fill(ds, StartIndex, Count, VisualTableName);
                //mCommand.Connection.Close();
                //object state = mDataAdapter.InsertCommand.Connection.State;

                return ds;
            }
            catch (Exception e)
            {
                log.Write(e.ToString(), "ExecuteDataSet", "", LogLevel.Error);
                return null;
            }
            finally
            {
                //mCommand.Connection.Close();
                ClearParameters();
            }

            //if (mCommand.Connection.ConnectionString.ToLower().IndexOf("server=") > -1)
            //{
            //    Connection = new SqlConnection(mCommand.Connection.ConnectionString);

            //    DataSet ds = new DataSet();
            //    try
            //    {
            //        Connection.Open();
            //        SqlDataAdapter command = new SqlDataAdapter(SQLText, Connection);
            //        command.Fill(ds, "ds");
            //    }
            //    catch 
            //    {
            //        return null;
            //    }
            //    return ds;
            //}
            //else
            //{
            //    DataSet ds = new DataSet();
            //    System.Data.OleDb.OleDbDataAdapter Adapter = new System.Data.OleDb.OleDbDataAdapter(SQLText, mCommand.Connection.ConnectionString);
            //    Adapter.Fill(ds, "w");
            //    return ds;
            //}

        }
        #endregion

        #region ExecuteDataView
        /// <summary>
        /// 返回DataView对象
        /// </summary>
        /// <param name="SQLText"></param>
        /// <returns>返回DataView对象</returns>
        public DataView ExecuteDataView(string SQLText)
        {
            try
            {
                DataSet ds = ExecuteDataSet(SQLText, "Table1", 0, 0);
                if (ds != null)
                {
                    return ds.Tables[0].DefaultView;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                log.Write(e.ToString(), "ExecuteDataView", "", LogLevel.Error);
                return null;
            }
            finally
            {
                ClearParameters();
            }
        }
        #endregion

        #region ExecuteDataRow
        /// <summary>
        /// 返回DataRow对象 为空则返回Null 多个字段时使用
        /// </summary>
        /// <param name="SQLText"></param>
        /// <returns></returns>
        public DataRow ExecuteDataRow(string SQLText)
        {
            try
            {
                DataSet ds = ExecuteDataSet(SQLText, "Table1", 0, 0);
                if (ds != null && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                {
                    return ExecuteDataSet(SQLText, "Table1", 0, 0).Tables[0].Rows[0];
                }
                else
                {
                    return null;
                }
            }
            catch { return null; }
        }
        #endregion



        #region base
        //下面为重载调用,不包含实际代码
        public DataSet ExecuteDataSet(string SQLText, int StartIndex, int Count) { return ExecuteDataSet(SQLText, "Table1", StartIndex, Count); }
        public DataSet ExecuteDataSet(string SQLText, string VisualTableName) { return ExecuteDataSet(SQLText, VisualTableName, 0, 0); }
        public DataSet ExecuteDataSet(string SQLText) { return ExecuteDataSet(SQLText, "Table1", 0, 0); }

        /// <summary>
        /// 添加一个参数
        /// </summary>
        /// <param name="ParameterName">参数的名称</param>
        /// <param name="Value">参数的值</param>
        /// <param name="Type">参数值的类型</param>
        /// <param name="Size">参数值的大小</param>
        /// <param name="Direction">参数的返回类型</param>
        /// <returns>返回添加后的参数对象DbParameter</returns>
        public DbParameter AddParameter(string ParameterName, object Value, DbType Type, int Size, ParameterDirection Direction)
        {
            DbParameter dbp = mCommand.CreateParameter();
            dbp.ParameterName = ParameterName;
            dbp.Value = Value;
            dbp.DbType = Type;
            if (Size != 0) { dbp.Size = Size; }
            dbp.Direction = Direction;
            mCommand.Parameters.Add(dbp);
            return dbp;
        }

        public void AddInParameter(DbCommand command,
                                  string name,
                                  DbType dbType)
        {
            AddParameter(command, name, dbType, ParameterDirection.Input, String.Empty, DataRowVersion.Default, null);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The commmand to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>                
        /// <param name="value"><para>The value of the parameter.</para></param>      
        public void AddInParameter(DbCommand command,
                                   string name,
                                   DbType dbType,
                                   object value)
        {
            AddParameter(command, name, dbType, ParameterDirection.Input, String.Empty, DataRowVersion.Default, value);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>                
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the value.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        public void AddInParameter(DbCommand command,
                                   string name,
                                   DbType dbType,
                                   string sourceColumn,
                                   DataRowVersion sourceVersion)
        {
            AddParameter(command, name, dbType, 0, ParameterDirection.Input, true, 0, 0, sourceColumn, sourceVersion, null);
        }

        /// <summary>
        /// Adds a new Out <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the out parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>        
        /// <param name="size"><para>The maximum size of the data within the column.</para></param>        
        public void AddOutParameter(DbCommand command,
                                    string name,
                                    DbType dbType,
                                    int size)
        {
            AddParameter(command, name, dbType, size, ParameterDirection.Output, true, 0, 0, String.Empty, DataRowVersion.Default, DBNull.Value);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>
        /// <param name="size"><para>The maximum size of the data within the column.</para></param>
        /// <param name="direction"><para>One of the <see cref="ParameterDirection"/> values.</para></param>
        /// <param name="nullable"><para>Avalue indicating whether the parameter accepts <see langword="null"/> (<b>Nothing</b> in Visual Basic) values.</para></param>
        /// <param name="precision"><para>The maximum number of digits used to represent the <paramref name="value"/>.</para></param>
        /// <param name="scale"><para>The number of decimal places to which <paramref name="value"/> is resolved.</para></param>
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the <paramref name="value"/>.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>       
        public virtual void AddParameter(DbCommand command,
                                         string name,
                                         DbType dbType,
                                         int size,
                                         ParameterDirection direction,
                                         bool nullable,
                                         byte precision,
                                         byte scale,
                                         string sourceColumn,
                                         DataRowVersion sourceVersion,
                                         object value)
        {
            if (command == null) throw new ArgumentNullException("command");

            DbParameter parameter = CreateParameter(name, dbType, size, direction, nullable, precision, scale, sourceColumn, sourceVersion, value);
            command.Parameters.Add(parameter);
        }
        protected DbParameter CreateParameter(string name)
        {
            DbParameter param = mCommand.CreateParameter();
            param.ParameterName = name;

            return param;
        }
        protected DbParameter CreateParameter(string name,
                                               DbType dbType,
                                               int size,
                                               ParameterDirection direction,
                                               bool nullable,
                                               byte precision,
                                               byte scale,
                                               string sourceColumn,
                                               DataRowVersion sourceVersion,
                                               object value)
        {
            DbParameter param = CreateParameter(name);
            ConfigureParameter(param, name, dbType, size, direction, nullable, precision, scale, sourceColumn, sourceVersion, value);
            return param;
        }
        protected virtual void ConfigureParameter(DbParameter param,
                                                   string name,
                                                   DbType dbType,
                                                   int size,
                                                   ParameterDirection direction,
                                                   bool nullable,
                                                   byte precision,
                                                   byte scale,
                                                   string sourceColumn,
                                                   DataRowVersion sourceVersion,
                                                   object value)
        {
            param.DbType = dbType;
            param.Size = size;
            param.Value = value ?? DBNull.Value;
            param.Direction = direction;
            param.IsNullable = nullable;
            param.SourceColumn = sourceColumn;
            param.SourceVersion = sourceVersion;
        }
        /// <summary>
        /// <para>Adds a new instance of a <see cref="DbParameter"/> object to the command.</para>
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>        
        /// <param name="direction"><para>One of the <see cref="ParameterDirection"/> values.</para></param>                
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the <paramref name="value"/>.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>    
        public void AddParameter(DbCommand command,
                                 string name,
                                 DbType dbType,
                                 ParameterDirection direction,
                                 string sourceColumn,
                                 DataRowVersion sourceVersion,
                                 object value)
        {
            AddParameter(command, name, dbType, 0, direction, false, 0, 0, sourceColumn, sourceVersion, value);
        }

        #region 重载调用,不包含实际代码

        public DbParameter AddParameter(string ParameterName, object Value, DbType Type, int Size) { return AddParameter(ParameterName, Value, Type, Size, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, object Value, ParameterDirection Direction) { return AddParameter(ParameterName, Value, DbType.Object, 0, Direction); }
        public DbParameter AddParameter(string ParameterName, object Value) { return AddParameter(ParameterName, Value, DbType.Object, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, string Value) { return AddParameter(ParameterName, Value, DbType.String, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, Int32 Value) { return AddParameter(ParameterName, Value, DbType.Int32, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, Int16 Value) { return AddParameter(ParameterName, Value, DbType.Int16, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, Boolean Value) { return AddParameter(ParameterName, Value, DbType.Boolean, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, UInt32 Value) { return AddParameter(ParameterName, Value, DbType.UInt32, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, UInt16 Value) { return AddParameter(ParameterName, Value, DbType.UInt16, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, Byte Value) { return AddParameter(ParameterName, Value, DbType.Byte, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, Decimal Value) { return AddParameter(ParameterName, Value, DbType.Decimal, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, Double Value) { return AddParameter(ParameterName, Value, DbType.Double, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, DateTime Value) { return AddParameter(ParameterName, Value, DbType.DateTime, 0, ParameterDirection.Input); }
        public DbParameter AddParameter(string ParameterName, Single Value) { return AddParameter(ParameterName, Value, DbType.Single, 0, ParameterDirection.Input); }

        public DbParameter AddOutParameter(string ParameterName, object Value, DbType Type, int Size) { return AddParameter(ParameterName, Value, Type, Size, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, object Value) { return AddParameter(ParameterName, Value, DbType.Object, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, string Value) { return AddParameter(ParameterName, Value, DbType.String, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, Int32 Value) { return AddParameter(ParameterName, Value, DbType.Int32, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, Int16 Value) { return AddParameter(ParameterName, Value, DbType.Int16, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, Boolean Value) { return AddParameter(ParameterName, Value, DbType.Boolean, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, UInt32 Value) { return AddParameter(ParameterName, Value, DbType.UInt32, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, UInt16 Value) { return AddParameter(ParameterName, Value, DbType.UInt16, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, Byte Value) { return AddParameter(ParameterName, Value, DbType.Byte, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, Decimal Value) { return AddParameter(ParameterName, Value, DbType.Decimal, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, Double Value) { return AddParameter(ParameterName, Value, DbType.Double, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, DateTime Value) { return AddParameter(ParameterName, Value, DbType.DateTime, 0, ParameterDirection.Output); }
        public DbParameter AddOutParameter(string ParameterName, Single Value) { return AddParameter(ParameterName, Value, DbType.Single, 0, ParameterDirection.Output); }

        #endregion
        /// <summary>
        /// 清除DbParameterCollection中所有DbParameter的引用
        /// </summary>
        public void ClearParameters()
        {
            mCommand.Parameters.Clear();
        }

        public DbParameterCollection Parameters
        {
            get { return mCommand.Parameters; }
        }

        public DbCommand Command
        {
            get { return mCommand; }
        }

        public DbDataAdapter DataAdapter
        {
            get { return mDataAdapter; }
        }
        #endregion

    }
}
