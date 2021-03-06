﻿using NPublic.Dapper;
using System;
using System.Data;
using static NPublic.Dapper.DapperManager;

namespace NPublic.DapperCoreDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            string ConnectionStr = "server=(local);UID=sa;PWD=sa;database=test";
            string mysqlhost = "Host = 127.0.0.1; UserName = root; Password = @; Database = KLine; Port = 3316; CharSet = utf8; Allow Zero Datetime = true;";
           
            NDapper dbss = DapperManager.CreateDatabase(ConnectionStr, DBType.SqlServer);
            ConnectionState state = dbss.State();
            var list = dbss.Query<KLine>("select id,symbol from pp2009_min10");

            NDapper dbss1 = DapperManager.CreateDatabase(ConnectionStr, DBType.SqlServer);
            var list1 = dbss1.Query<KLine>("select id,symbol from pp2009_min11");

            NDapper dbss2 = DapperManager.CreateDatabase(ConnectionStr, DBType.SqlServer);
            var list2 = dbss2.Query<KLine>("select id,symbol from pp2009_min12");


            NDapper dbSqlLite = DapperManager.CreateDatabase(@"symbo3.db", DBType.SqlLite);
            //ConnectionState DapperState = dbSqlLite.State();
            string createtb = "create table  hsi1903_min1 (id int, symbol varchar(50))";
            int x = dbSqlLite.Execute(createtb);
            int xs = dbSqlLite.Execute("insert into hsi1903_min1(id,symbol)values('1','122')");
            var listSS = dbSqlLite.Query<KLine>("select id,symbol from hsi1903_min1");
            Console.ReadKey();
        }

        public class KLine
        {
            public int ID { get; set; }
            public string symbol { get; set; }
        }
    }
}
