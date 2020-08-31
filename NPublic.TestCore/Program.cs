using NPublic.Logger;
using System;
using System.Threading;

namespace NPublic.TestCore
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
                log.Write("严重错误", "title" + i, "ssfs", LogLevel.Error);
                log.Write("警告", "title" + i, "ssfs", LogLevel.Error);
                Thread.Sleep(1000);
            }

            //NLog.Init().Writer(NPublic.NLogger.LogLevel.Error, null, "ExecuteSql7", "");
            //NLog.Init().Writer(NPublic.NLogger.LogLevel.Error, null, "ExecuteSql8", "");

            Console.ReadKey();
        }
    }
}
