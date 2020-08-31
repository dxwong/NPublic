using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NPublic.Logger
{
    public class WriteMQ : ILogger
    {
        public void LogWrite(LogMessage msg)
        {
           DetailConfig detail= NLog.config["File"];
            //Thread.Sleep(5000);
            //Console.WriteLine("MQ:" + msg.Title);
        }
    }

    //public static void TryAdd(LogMessage msg, string key = "q")
    //{
    //    if (NLog.DicReceiveQue.ContainsKey(key))
    //    {
    //        NLog.DicReceiveQue[key].Enqueue(msg);
    //    }
    //    else
    //    {
    //        NLog.DicReceiveQue.TryAdd(key, new ConcurrentQueue<LogMessage>());
    //        NLog.DicReceiveQue[key].Enqueue(msg);
    //    }
    //}

    //public static bool Get(string key, out LogMessage msg)
    //{
    //    msg = null;
    //    while (NLog.DicReceiveQue.Count > 0 && NLog.DicReceiveQue[key] != null &&
    //        NLog.DicReceiveQue[key].Count > 0 && NLog.DicReceiveQue[key].TryDequeue(out msg))
    //    {
    //    }
    //    return msg == null ? false : true;
    //}
}
