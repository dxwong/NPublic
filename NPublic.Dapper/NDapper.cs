using Dapper;
using NPublic.Logger;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

/// <summary>
/// dxwang
/// 4638912@qq.com
/// </summary>
namespace NPublic.Dapper
{
    public partial class DapperManager
    {
        /// <summary>
        /// 执行主要操作的类,重写Dapper
        /// </summary>
        public class NDapper
        {
            public IDbTransaction DbTransaction { get; set; }
            private readonly IDbConnection conn;
            private readonly NLog log;

            /// <summary>
            /// 构造函数
            /// </summary>
            public NDapper(IDbConnection conn)
            {
                this.conn = conn;
                log = NLog.Init();
            }

            #region 判断数据库连接状态
            /// <summary>
            /// 判断数据库连接状态
            /// </summary>
            /// <returns></returns>
            public ConnectionState State()
            {
                try
                {
                    conn.Open();
                    return conn.State;
                }
                catch (Exception ex)
                {
                    log.Write(ex.ToString(), "ConnectionState", "", LogLevel.Error);
                }
                finally
                {
                    conn.Close();
                }
                return conn.State;
            }
            #endregion


            #region 实例方法

            #region 查询

            /// <summary>
            /// 查询
            /// </summary>
            /// <typeparam name="T">返回类型</typeparam>
            /// <param name="sql">sql语句</param>
            /// <param name="dbConnKey">数据库连接</param>
            /// <param name="param">sql查询参数</param>
            /// <param name="commandTimeout">超时时间</param>
            /// <param name="commandType">命令类型</param>
            /// <returns></returns>
            public T QueryFirst<T>(string sql, object param = null, bool buffered = true, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
            {
                if (DbTransaction == null)
                {
                    try
                    {
                        return conn.QueryFirstOrDefault<T>(sql, param, null, commandTimeout, commandType);
                    }
                    catch (Exception ex)
                    {
                        log.Write(ex.ToString(), "QueryFirst", "", LogLevel.Error);
                        return default(T);
                    }
                }
                else
                {
                    return DbTransaction.Connection.QueryFirstOrDefault<T>(sql, param, DbTransaction, commandTimeout, commandType);
                }

            }

            /// <summary>
            /// 查询(异步版本)
            /// </summary>
            /// <typeparam name="T">返回类型</typeparam>
            /// <param name="sql">sql语句</param>
            /// <param name="dbConnKey">数据库连接</param>
            /// <param name="param">sql查询参数</param>
            /// <param name="commandTimeout">超时时间</param>
            /// <param name="commandType">命令类型</param>
            /// <returns></returns>
            public async Task<T> QueryFirstAsync<T>(string sql, object param = null, bool buffered = true, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
            {
                if (DbTransaction == null)
                {
                    try
                    {
                        return await conn.QueryFirstOrDefaultAsync<T>(sql, param, null, commandTimeout, commandType);
                    }
                    catch (Exception ex)
                    {
                        log.Write(ex.ToString(), "QueryFirstAsync", "", LogLevel.Error);
                        return default(T);
                    }
                }
                else
                {
                    return await DbTransaction.Connection.QueryFirstOrDefaultAsync<T>(sql, param, DbTransaction, commandTimeout, commandType);
                }

            }


            /// <summary>
            /// 查询
            /// </summary>
            /// <typeparam name="T">返回类型</typeparam>
            /// <param name="sql">sql语句</param>
            /// <param name="dbConnKey">数据库连接</param>
            /// <param name="param">sql查询参数</param>
            /// <param name="buffered">是否缓冲</param>
            /// <param name="commandTimeout">超时时间</param>
            /// <param name="commandType">命令类型</param>
            /// <returns></returns>
            public IEnumerable<T> Query<T>(string sql, object param = null, bool buffered = true, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
            {
                if (DbTransaction == null)
                {
                    try
                    {
                        return conn.Query<T>(sql, param, null, buffered, commandTimeout, commandType);
                    }
                    catch (Exception ex)
                    {
                        log.Write(ex.ToString(), "Query", "", LogLevel.Error);
                        return null;
                    }
                }
                else
                {
                    return DbTransaction.Connection.Query<T>(sql, param, DbTransaction, buffered, commandTimeout, commandType);
                }

            }


            /// <summary>
            /// 查询(异步版本)
            /// </summary>
            /// <typeparam name="T">返回类型</typeparam>
            /// <param name="sql">sql语句</param>
            /// <param name="dbConnKey">数据库连接</param>
            /// <param name="param">sql查询参数</param>
            /// <param name="buffered">是否缓冲</param>
            /// <param name="commandTimeout">超时时间</param>
            /// <param name="commandType">命令类型</param>
            /// <returns></returns>
            public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, bool buffered = true, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
            {
                if (DbTransaction == null)
                {
                    try
                    {
                        return await conn.QueryAsync<T>(sql, param, null, commandTimeout, commandType);
                    }
                    catch (Exception ex)
                    {
                        log.Write(ex.ToString(), "QueryAsync", "", LogLevel.Error);
                        return null;
                    }
                }
                else
                {
                    return await DbTransaction.Connection.QueryAsync<T>(sql, param, DbTransaction, commandTimeout, commandType);
                }

            }



            /// <summary>
            /// 查询返回 IDataReader
            /// </summary>
            /// <param name="sql">sql语句</param>
            /// <param name="dbConnKey">数据库连接</param>
            /// <param name="param">sql查询参数</param>
            /// <param name="commandTimeout">超时时间</param>
            /// <param name="commandType">命令类型</param>
            /// <returns></returns>
            public IDataReader ExecuteReader(string sql, object param = null, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
            {
                if (DbTransaction == null)
                {
                    try
                    {
                        return conn.ExecuteReader(sql, param, null, commandTimeout, commandType);
                    }
                    catch (Exception ex)
                    {
                        log.Write(ex.ToString(), "ExecuteReader", "", LogLevel.Error);
                        return null;
                    }
                }
                else
                {
                    return DbTransaction.Connection.ExecuteReader(sql, param, DbTransaction, commandTimeout, commandType);
                }
            }

            /// <summary>
            /// 查询单个返回值
            /// </summary>
            /// <typeparam name="T">返回类型</typeparam>
            /// <param name="sql">sql语句</param>
            /// <param name="dbConnKey">数据库连接</param>
            /// <param name="param">sql查询参数</param>
            /// <param name="commandTimeout">超时时间</param>
            /// <param name="commandType">命令类型</param>
            /// <returns></returns>
            public T ExecuteScalar<T>(string sql, object param = null, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
            {
                if (DbTransaction == null)
                {
                    try
                    {
                        return conn.ExecuteScalar<T>(sql, param, null, commandTimeout, commandType);
                    }
                    catch (Exception ex)
                    {
                        log.Write(ex.ToString(), "ExecuteScalar", "", LogLevel.Error);
                        return default(T);
                    }
                }
                else
                {
                    return DbTransaction.Connection.ExecuteScalar<T>(sql, param, DbTransaction, commandTimeout, commandType);
                }

            }
            #endregion

            /// <summary>
            /// 执行增删改sql
            /// </summary>
            /// <param name="sql">sql</param>
            /// <param name="dbkey">数据库连接</param>
            /// <param name="param">sql查询参数</param>
            /// <param name="commandTimeout">超时时间</param>
            /// <param name="commandType">命令类型</param>
            /// <returns></returns>
            public int Execute(string sql, object param = null, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
            {
                if (DbTransaction == null)
                {
                    try
                    {
                        return conn.Execute(sql, param, null, commandTimeout, commandType);
                    }
                    catch (Exception ex)
                    {
                        log.Write(ex.ToString(), "ExecuteSql", "", LogLevel.Error);
                        return -1;
                    }
                }
                else
                {
                    return DbTransaction.Connection.Execute(sql, param, DbTransaction);
                }
            }

            /// <summary>
            /// 执行增删改sql(异步版本)
            /// </summary>
            /// <param name="sql">sql</param>
            /// <param name="dbkey">数据库连接</param>
            /// <param name="param">sql查询参数</param>
            /// <param name="commandTimeout">超时时间</param>
            /// <param name="commandType">命令类型</param>
            /// <returns></returns>
            public async Task<int> ExecuteSqlAsync(string sql, object param = null, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
            {
                if (DbTransaction == null)
                {
                    try
                    {
                        return await conn.ExecuteAsync(sql, param, null, commandTimeout, commandType);
                    }
                    catch (Exception ex)
                    {
                        log.Write(ex.ToString(), "ExecuteSqlAsync", "", LogLevel.Error);
                        return -1;
                    }
                }
                else
                {
                    await DbTransaction.Connection.ExecuteAsync(sql, param, DbTransaction);
                    return 0;
                }
            }
            #endregion

            #region 事务提交

            /// <summary>
            /// 事务开始
            /// </summary>
            /// <returns></returns>
            public NDapper BeginTransaction()
            {
                conn.BeginTransaction();//需要手动开启事务控制
                return this; ;
            }

            /// <summary>
            /// 提交当前操作的结果
            /// </summary>
            public int Commit()
            {
                try
                {
                    if (DbTransaction != null)
                    {
                        DbTransaction.Commit();
                        this.Close();
                    }
                    return 1;
                }
                catch (Exception ex)
                {
                    log.Write(ex.ToString(), "Commit", "", LogLevel.Error);
                    return -1;
                }
                finally
                {
                    if (DbTransaction == null)
                    {
                        this.Close();
                    }
                }
            }

            /// <summary>
            /// 把当前操作回滚成未提交状态
            /// </summary>
            public void Rollback()
            {
                this.DbTransaction.Rollback();
                this.DbTransaction.Dispose();
                this.Close();
            }

            /// <summary>
            /// 关闭连接 内存回收
            /// </summary>
            public void Close()
            {
                IDbConnection dbConnection = DbTransaction.Connection;
                if (dbConnection != null && dbConnection.State != ConnectionState.Closed)
                {
                    dbConnection.Close();
                }
            }

            #endregion
        }
    }
}
