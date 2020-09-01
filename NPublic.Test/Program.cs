using NPublic.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NPublic.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            NLog log = NLog.Init();
            log.Receive += (msg) => { Console.WriteLine("Receive:" + msg.Title + " Level:" + msg.Level); };

            //Task.Run(() => { test(); });
            //Task.Run(() => { test2(); });
            for (int i = 0; i < 10000; i++)
            {
                log.Write("严重错误", "title" + i, "ssfs", LogLevel.Auto);
                log.Write("警告", "title" + i, "ssfs", LogLevel.Auto);
                log.Write("警警告123", "title" + i, "ssfs", LogLevel.Skip);
                Thread.Sleep(1000);
            }

            //NLog.Init().Writer(NPublic.NLogger.LogLevel.Error, null, "ExecuteSql7", "");
            //NLog.Init().Writer(NPublic.NLogger.LogLevel.Error, null, "ExecuteSql8", "");

            Console.ReadKey();
        }
    }
}
