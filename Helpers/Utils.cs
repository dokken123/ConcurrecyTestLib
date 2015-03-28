using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using ConcurrencyTestLib.Utils;
using Serve.Shared.JsonSerializer;

namespace ConcurrencyTestLib.Helpers
{
    public static class Utils
    {

        public static readonly Dictionary<API_TRADECODES, List<long>> CalldurationList = new Dictionary<API_TRADECODES, List<long>>();

        public static Dictionary<string, string> ConvertArgs(string[] args)
        {
            Dictionary<string, string> opt = new Dictionary<string, string>();

            foreach (string a in args)
            {
                var matches = Regex.Split(a,@"^-([^:]+):(.+)$");
                if (matches.Length == 4)
                {
                    opt.Add(matches[1].ToUpper(), matches[2].ToUpper());
                }
            }

            return opt;
        }

        public static string GetOption(Dictionary<string,string> opts ,string key)
        {
            if (opts.Keys.Contains(key.ToUpper()))
            {
                return opts[key.ToUpper()];
            }
            return null;
        }

        public static T GetTestFromContent<T>(string path)
        {
            string content = FileReader.LoadFileContent(path);
            content = JsonHelper.Format(content);

            JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
            return jsonSerializer.Deserialize<T>(content);
        }
    }
}
