using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConcurrencyTestLib.Helpers;
using System.Web.Script.Serialization;
using Serve.Shared.JsonSerializer;
using ConcurrencyTestLib.Utils;
using System.Configuration;
using System.Diagnostics;
using ConcurrencyTestLib.Interfaces;

namespace ConcurrencyTestLib.Test
{
    public class TestCaseProcessor
    {
        private static object lockObj = new object();
        public TestCase LoadTestCase(string testCaseName)
        {
            TestCase test_case = ConcurrencyTestLib.Helpers.Utils.GetTestFromContent<TestCase>(String.Format(@"{0}\{1}.{2}", @"Tests\testcases\",testCaseName,"tcase"));

            return test_case;
        }

        public static bool ProcessTestCase(TestCase tc, string jsonreq, ITestCallback runner, int parallelIndex)
        {
            ServeWebResponse response = null;

            string url = "";

            switch (tc.API)
            {
                case API.ACCOUNT:
                    url = ConfigurationManager.AppSettings["AccountUrl"] + "/AccountService/";
                    break;
                case API.RTQ:
                    url = ConfigurationManager.AppSettings["RTQUrl"] + "/RTQReportService/";
                    break;
                case API.WALLET:
                default:
                    url = ConfigurationManager.AppSettings["WalletUrl"] + "/WalletService/";
                    break;
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string guid = Guid.NewGuid().ToString();

            try
            {

                ServeHttpClient client = new ServeHttpClient(null, Boolean.Parse(ConfigurationManager.AppSettings["UseCert"]));


                runner.MessageCallback("----------------------------------------------------------");
                runner.MessageCallback(String.Format("Thread {0} Started for Case - \r\n"
                                            + "Timestamp: {5} \r\n"
                                            + "GUID: {4} \r\n"
                                            + "API: {1} \r\n"
                                            + "TradeCode: {2} \r\n"
                                            + "Body: {3} \r\n",
                    parallelIndex, tc.API, tc.TradeCode, JsonHelper.Format(jsonreq), guid, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff")));

                response = client.HttpPOST(String.Format("{0}{1}", url, tc.TradeCode), jsonreq);

                runner.MessageCallback("----------------------------------------------------------");

            }
            catch (Exception ex)
            {
                lock (lockObj)
                {
                    runner.MessageCallback("----------------------------------------------------------");
                    runner.MessageCallback(String.Format("Exception Thrown for thread {0}, GUID: {1}, Exception info: {2}, Duration: {3} ms \r\n", parallelIndex, guid, ex.Message, sw.ElapsedMilliseconds));
                    runner.MessageCallback("----------------------------------------------------------");
                }
            }
            finally
            {
                lock (lockObj)
                {
                    long duration = sw.ElapsedMilliseconds;
                    if (!Helpers.Utils.CalldurationList.ContainsKey(tc.TradeCode))
                    {
                        Helpers.Utils.CalldurationList[tc.TradeCode] = new List<long>();
                    }
                    Helpers.Utils.CalldurationList[tc.TradeCode].Add(duration);
                    runner.MessageCallback("----------------------------------------------------------");
                    runner.MessageCallback(String.Format("Thread {0} Ended for Case - \r\n"
                                           + "Timestamp: {5} \r\n"
                                           + "Duration: {6} ms \r\n"
                                           + "GUID: {4} \r\n"
                                           + "API: {1} \r\n"
                                           + "TradeCode: {2} \r\n"
                                           + "Response: {3} \r\n",
                                           parallelIndex, tc.API, tc.TradeCode, 
                                           JsonHelper.Format(response == null ? String.Empty :response.Data), 
                                           guid, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff"),duration));
                    runner.MessageCallback("----------------------------------------------------------");
                }
            }
            sw.Stop();
            return true;
        }

    }
}
