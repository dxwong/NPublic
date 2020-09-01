using System;
using System.Text.RegularExpressions;

namespace NPublic.Logger
{
    public partial class NLog
    {
        /// <summary>
        /// 日志写入MQ队列
        /// </summary>
        /// <param name="level">等级</param>
        /// <param name="exception">异常</param>
        /// <param name="title">日志标题</param>
        /// <param name="titleDetail">详细提示，如SQL来源</param>
        public void Write(string exception, string title="", string titleDetail = "", LogLevel level = LogLevel.Auto)
        { 
            if (level == LogLevel.Skip)
            {
                return;//允许客户端判断日志是否写入
            }

            if (level == LogLevel.Auto && CheckFatal(exception))
            {
                level = LogLevel.Fatal;
            }
            else if (level == LogLevel.Auto && CheckWarning(exception))
            {
                level = LogLevel.Warning;
            }
            else if (level == LogLevel.Auto && CheckSkip(exception))
            {
                return;//Skip如果
            }

            _que.Enqueue(new LogMessage
            {
                Title = title,
                TitleDetail = titleDetail,
                Level = level,
                Exception = exception,
                Addtime = DateTime.Now,
                IP = ""
            });

            if (_que.Count > 3000)//队列最多保存的记录数
            {
                _que.TryDequeue(out LogMessage _);
            }
        }

        /// <summary>
        /// CheckSkip
        /// </summary>
        /// <param name="level"></param>
        /// <param name="ex"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        static bool CheckSkip(string exception)
        {
            string pattern = NLog.config["File"].SkipRegex;
            return pattern == "" ? false : Regex.IsMatch(exception, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// CheckWarning
        /// </summary>
        /// <param name="level"></param>
        /// <param name="ex"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        static bool CheckWarning(string exception)
        {
            string pattern = NLog.config["File"].WarningRegex;
            return pattern == "" ? false : Regex.IsMatch(exception, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// CheckFatal
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        static bool CheckFatal(string exception)
        {
            string pattern = NLog.config["File"].FatalRegex;
            return pattern == "" ? false : Regex.IsMatch(exception, pattern, RegexOptions.IgnoreCase);
        }
    }
}
