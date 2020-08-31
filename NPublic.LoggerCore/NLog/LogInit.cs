using Newtonsoft.Json;
using NPublic.DI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// dxwang
/// 4638912@qq.com
/// </summary>
namespace NPublic.Logger
{
    /// <summary>
    /// NLog入口
    /// </summary>
    public partial class NLog
    {
        public static Dictionary<string, DetailConfig> config = new Dictionary<string, DetailConfig>();
        public Action<LogMessage> Receive { get; set; }
        static ConcurrentQueue<LogMessage> _que = new ConcurrentQueue<LogMessage>();
        ServiceContainer Service = new ServiceContainer();
        ILogger logFile; ILogger logMQ; ILogger logDB;

        /// <summary>
        /// 构造函数
        /// </summary>
        private NLog(string ConfigFilePath)
        {
            Service.Register<ILogger, WriteFile>();
            Service.Register<ILogger, WriteMQ>();
            Service.Register<ILogger, WriteDB>();

            DeserializeObject(ConfigFilePath);
            Task.Run(() => { LogFileStorage(); });
        }

        /// <summary>
        /// log配置文件反序列化
        /// </summary>
        /// <param name="ConfigFilePath"></param>
        public static void DeserializeObject(string ConfigFilePath)
        {
            string jsonfile = ConfigFilePath == null ? $"{Directory.GetCurrentDirectory()}" + "\\logConfig.json" : ConfigFilePath;
            string strjson = File.ReadAllText(jsonfile);

            List<LogConfig> List = JsonConvert.DeserializeObject<List<LogConfig>>(strjson);
            foreach (LogConfig fig in List)
            {
                NLog.config.Add(fig.Type, fig.DetailConfig);
            }
        }

        /// <summary>
        /// 日志保存服务；依赖注入
        /// 单例模式，此入口仅执行一次
        /// </summary>
        void LogFileStorage()
        {
            if (ServiceContainer.DicToRegister.ContainsKey(typeof(WriteFile)))
            {
                logFile = Service.Resolve<WriteFile>();
            }
            if (ServiceContainer.DicToRegister.ContainsKey(typeof(WriteMQ)))
            {
                logMQ = Service.Resolve<WriteMQ>();
            }
            if (ServiceContainer.DicToRegister.ContainsKey(typeof(WriteDB)))
            {
                logDB = Service.Resolve<WriteDB>();
            }

            while (true)
            {
                while (_que.Count > 0 && _que.TryDequeue(out LogMessage msg))
                {
                    if (Receive != null)  Task.Run(() => { Receive(msg); }); 
                    if (NLog.config["File"].Enable)  Task.Run(() => { logFile.LogWrite(msg); }); 
                    if (NLog.config["MQ"].Enable)  Task.Run(() => { logMQ.LogWrite(msg); }); 
                    if (NLog.config["DB"].Enable) Task.Run(() => { logDB.LogWrite(msg); }); 
                }

                Thread.Sleep(100);
            }
        }

        private static NLog _logger = null;
        private static readonly object _objLock = new object();
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public static NLog Init(string ConfigFilePath=null)
        {
            if (_logger == null)
            {
                lock (_objLock)
                {
                    if (_logger == null)
                    {
                        _logger = new NLog(ConfigFilePath);
                    }
                }
            }
            return _logger;
        }
    }
}
