using System;

namespace NPublic.Logger
{

    /// <summary>
    /// 日志类型
    /// </summary>
    public enum LogLevel
    {
        Auto = 0,
        Debug = 1,
        Info = 2,
        Error = 3,
        Warning = 4,
        Fatal = 5
    }

    /// <summary>
    /// 日志内容
    /// </summary>
    public class LogMessage
    {
        public string Title { get; set; }
        public string TitleDetail { get; set; }
        public LogLevel Level { get; set; }
        public string Exception { get; set; }
        public DateTime Addtime { get; set; }
        public string IP { get; set; }
    }

    public class LogConfig
    {
        public string Type { get; set; }
        public DetailConfig DetailConfig { get; set; }
    }

    public class DetailConfig
    {
        public bool Enable { get; set; }
        public string TxtFormat { get; set; }
        public string SavePath { get; set; }
        public string WarningRegex { get; set; }
        public string FatalRegex { get; set; }
        public string ConnectionString { get; set; }
    }
}
