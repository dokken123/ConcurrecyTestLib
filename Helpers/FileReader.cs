using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConcurrencyTestLib.Helpers
{
    public class FileReader
    {
        private static readonly Random rnd = new Random(DateTime.Now.Second);

        public static string LoadRandomAcctDate()
        {
            string acctdate = "";

            using (FileStream fs = File.OpenRead(String.Format(@"{0}templates\acct_balance.csv",AppDomain.CurrentDomain.BaseDirectory)))
            {
                StreamReader sr = new StreamReader(fs, Encoding.UTF8);
                string content = sr.ReadToEnd();
                fs.Flush();
                sr.Close();
                fs.Close();
                content = content.Replace("\r","");
                string[] list = content.Split('\n');
                acctdate = list[rnd.Next(0,list.Length - 1)];
            }

            return acctdate;
        }

        public static string LoadFileContent(string fileName,string folder)
        {
            return LoadFileContent(String.Format(@"{1}\{0}", fileName,folder));
        }

        public static string LoadFileContent(string fileName,string postfix, string folder)
        {
            return LoadFileContent(String.Format("{0}.{1}",fileName,postfix),folder);
        }

        public static string LoadFileContent(string path)
        {
            string content = "";
            try
            {
                using (FileStream fs = File.OpenRead(String.Format(@"{0}\{1}", AppDomain.CurrentDomain.BaseDirectory, path)))
                {
                    StreamReader sr = new StreamReader(fs, Encoding.UTF8);
                    content = sr.ReadToEnd();
                    fs.Flush();
                    sr.Close();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return content;
        }
    }
}
