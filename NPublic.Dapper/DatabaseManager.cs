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
    /// ԭ�����ݿ����
    /// </summary>
    public static class DatabaseManager
    {
        /// <summary>
        /// ����Database����,���Զ�ʶ��mysql��Ĭ��ΪMSSQL
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
        /// ����Database����
        /// </summary>
        public static Database CreateDatabase(string strconn, DBType DBType)
        {
            //strconn = UBase.DES_Decrypt(strconn);//���ݿ������ַ�����DES����
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
    /// ִ����Ҫ��������
    /// </summary>
    public class Database
    {
        private DbDataAdapter mDataAdapter;
        private DbCommand mCommand;
        public DBType thisDBType;
        private readonly NLog log;

        #region Database
        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="DDA">���һ��ʵ�����˵�DbDataAdapter��������</param>
        public Database(DbDataAdapter DDA, DBType DBType)
        {
            mDataAdapter = DDA;
            mCommand = DDA.SelectCommand;
            thisDBType = DBType;
            log = NLog.Init();
        }
        #endregion

        /// <summary>
        /// ���ݿ�����״̬
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
        /// ִ��insert����������ĵ�һ�е�һ�е�ֵ,������ID,��������Ӱ��ļ�¼��
        /// </summary>
        /// <param name="SQLText">SQL���</param>
        /// <returns>�����ĵ�һ�е�һ�е�ֵ</returns>
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
                    object o = mCommand.ExecuteScalar();//��һ�е�һ�е�ֵ����Ƿ�int,ExecuteScalar�᷵��null
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
        /// ִ��insert����������ĵ�һ�е�һ�е�IDֵ,��������Ӱ��ļ�¼��
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
                    object o = mCommand.ExecuteScalar();//��һ�е�һ�е�ֵ����Ƿ�int,ExecuteScalar�᷵��null
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
        /// ִ��Access���ݿ�
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
        /// ִ��Access���ݿ�
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
        /// �ж�һ��stirng�Ƿ�Ϊ�������
        /// </summary>
        /// <param name="SQLText">Ŀ��string</param>
        /// <returns>�����Ƿ�Ϊ������̵ĵ���</returns>
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



        #region ����ִ��һ��SQL���,���Sql��Ӣ�Ķ��Ÿ���
        /// <summary>
        /// ����ִ��һ��SQL���,���Sql��Ӣ�Ķ��Ÿ���
        /// </summary>
        /// <param name="SqlStrings">SQL�����</param>
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
        /// ����ִ��һ��SQL���,���Sql��Ӣ�Ķ��Ÿ���
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

        #region ʹ�ù�ϣ�����һ����¼��

        /// <summary>
        /// ʹ�ù�ϣ�����һ����¼
        /// </summary>
        /// <param name="TableName">����</param>
        /// <param name="Cols">��������ֵΪ�ֶ�����ֵΪ�ֶ�ֵ</param>
        /// <returns>Ӱ���¼</returns>
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
        /// ʹ�ù�ϣ�����һ����¼
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

        #region ��ϣ������¼������ִ�е�SQL���
        /// <summary>
        /// ��ϣ������¼������ִ�е�SQL���
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

        #region ʹ�ù�ϣ�����һ�����ݱ�
        /// <summary>
        /// ʹ�ù�ϣ�����һ�����ݱ�
        /// </summary>
        /// <param name="TableName">����</param>
        /// <param name="Cols">��������ֵΪ�ֶ�����ֵΪ�ֶ�ֵ</param>
        /// <param name="Where">Where�Ӿ�</param>
        /// <returns>Ӱ���¼</returns>
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
        /// ʹ�ù�ϣ�����һ�����ݱ�
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
        /// ִ�����ݿ������DataReader
        /// </summary>
        /// <param name="SQLText">SQL����</param>
        /// <returns>����DataReader</returns>
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
        /// ִ��ͳ�Ƶķ���
        /// </summary>
        /// <param name="SQLText">SQL����</param>
        /// <returns>����object�������ͳ�����ݻ��߽���ĵ�һ�е�һ��</returns>
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
        /// ִ�в�ѯ��������DataSet����
        /// </summary>
        /// <param name="SQLText">SQl����</param>
        /// <param name="VisualTableName">�������</param>
        /// <param name="StartIndex">�ƶ����ض������Ժ������</param>
        /// <param name="Count">�ƶ��ܹ����ض�����</param>
        /// <returns>���ذ�Ҫ������˵�DataSet</returns>
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
        /// ����DataView����
        /// </summary>
        /// <param name="SQLText"></param>
        /// <returns>����DataView����</returns>
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
        /// ����DataRow���� Ϊ���򷵻�Null ����ֶ�ʱʹ��
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
        //����Ϊ���ص���,������ʵ�ʴ���
        public DataSet ExecuteDataSet(string SQLText, int StartIndex, int Count) { return ExecuteDataSet(SQLText, "Table1", StartIndex, Count); }
        public DataSet ExecuteDataSet(string SQLText, string VisualTableName) { return ExecuteDataSet(SQLText, VisualTableName, 0, 0); }
        public DataSet ExecuteDataSet(string SQLText) { return ExecuteDataSet(SQLText, "Table1", 0, 0); }

        /// <summary>
        /// ���һ������
        /// </summary>
        /// <param name="ParameterName">����������</param>
        /// <param name="Value">������ֵ</param>
        /// <param name="Type">����ֵ������</param>
        /// <param name="Size">����ֵ�Ĵ�С</param>
        /// <param name="Direction">�����ķ�������</param>
        /// <returns>������Ӻ�Ĳ�������DbParameter</returns>
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

        #region ���ص���,������ʵ�ʴ���

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
        /// ���DbParameterCollection������DbParameter������
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
