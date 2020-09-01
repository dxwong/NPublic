using System;

namespace NPublic.Logger
{

    /// <summary>
    /// 日志类型
    /// </summary>
    public enum LogLevel
    {
        Skip = 0,
        Auto = 1,
        Debug = 2,
        Info = 3,
        Error = 4,
        Warning = 5,
        Fatal = 6
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
        public string SkipRegex { get; set; }
        public string WarningRegex { get; set; }
        public string FatalRegex { get; set; }
        public string ConnectionString { get; set; }
    }
}
