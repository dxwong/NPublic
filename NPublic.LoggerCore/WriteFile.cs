using System;
using System.IO;
using System.Text;

namespace NPublic.Logger
{
    public class WriteFile : ILogger
    {
        static object _WLock = new object();
        public void LogWrite(LogMessage msg)
        {
            lock (_WLock)
            {
                string Dir = Path.IsPathRooted(NLog.config["File"].SavePath) ? NLog.config["File"].SavePath : string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, Path.Combine(NLog.config["File"].SavePath, msg.Level.ToString()));
                string filename = string.Format("{0}\\{1}.log", Dir, msg.Addtime.ToString("yyyyMMdd"));
                string logString = string.Format(NLog.config["File"].TxtFormat, msg.Addtime.ToString(), msg.Title, msg.TitleDetail, msg.Exception, msg.IP);
               
                FileWrite(Dir, filename, logString);
            }
        }

        #region 写文件
        static object _fLock = new object();
        /// <summary>
        /// 写文件
        /// </summary>
        /// <param name="err"></param>
        /// <param name="file"></param>
        static void FileWrite(string Dir, string filename, string logstr)
        {
            lock (_fLock)
            {
                if (!Directory.Exists(Dir)) { Directory.CreateDirectory(Dir); }
                if (!System.IO.File.Exists(filename))
                {
                    System.IO.FileStream f = System.IO.File.Create(filename);
                    f.Close();
                }
                StreamWriter sw = new StreamWriter(filename, true, Encoding.GetEncoding("UTF-8"));
                sw.Write(logstr);
                sw.Flush();
                sw.Close();
            }
        }
        #endregion
    }
}
