using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConcurrencyTestLib.Helpers
{
    public class OutputHelper
    {

        public static readonly OutputHelper Instance = new OutputHelper();

        private static readonly string logFileName = String.Empty;
        private static readonly object lockObj = new object();

        private OutputHelper()
        {

        }

        static OutputHelper()
        {
            logFileName = String.Format(@"{1}out\{0}.txt", DateTime.Now.ToString("yyyyMMddHHmmssffff"),AppDomain.CurrentDomain.BaseDirectory);
            if(!File.Exists(logFileName))
            {
                FileInfo fi = new FileInfo(logFileName);
                FileStream outFileSm = fi.Open(FileMode.Create, FileAccess.Write, FileShare.Write);
                outFileSm.Seek(0, SeekOrigin.End);
                outFileSm.Flush();
                outFileSm.Close();
            }
        }

        public void Log(string logInfo)
        {
            lock (lockObj)
            {
                FileInfo fi = new FileInfo(logFileName);
                FileStream outFileSm = fi.Open(FileMode.Append, FileAccess.Write, FileShare.Write);
                StreamWriter sw = new StreamWriter(outFileSm, Encoding.UTF8);
                outFileSm.Seek(0, SeekOrigin.End);
                sw.WriteLine(logInfo);
                sw.Flush();
                outFileSm.Flush();
                sw.Close();
                outFileSm.Close();
            }
        }
    }
}
