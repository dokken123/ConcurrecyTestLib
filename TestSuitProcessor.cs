using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ConcurrencyTestLib.Helpers;
using ConcurrencyTestLib.Interfaces;
using ConcurrencyTestLib.Parallel;
using ConcurrencyTestLib.Test;
using Serve.Shared.JsonSerializer;

namespace ConcurrencyTestLib
{
    public class TestSuitProcessor
    {

        public void RunTest(string testSuitName, ITestCallback runner)
        {
            TestSuit ts = ConcurrencyTestLib.Helpers.Utils.GetTestFromContent<TestSuit>(String.Format(@"{0}\{1}.{2}", @"Tests\", testSuitName, "tsuit"));
            TestCaseProcessor tcProc = new TestCaseProcessor();
            foreach (string tcName in ts.TestCases)
            {
                TestCase tc = tcProc.LoadTestCase(tcName);
                ParallelActivator.StartParallelJob(tc, runner);
            }

            runner.MessageCallback("Call Duration Stats:");
            foreach (var item in Helpers.Utils.CalldurationList)
            {
                double max = item.Value.Max();
                double min = item.Value.Min();
                double avg = item.Value.Average();
                List<long> prob = new List<long>();
                item.Value.Sort();
                for (int m = item.Value.Count / 4; m < Math.Ceiling((double)(item.Value.Count) * 3 / 4); m++)
                {
                    prob.Add(item.Value[m]);
                }
                runner.MessageCallback(String.Format("Trade Code: {0}, MAX:{1}ms, MIN:{2}ms, AVG:{3}ms, BETA:{4}ms",
                    item.Key,
                    max,
                    min,
                    avg,
                    (max + min + 4 * prob.Average()) / 6));
            }
            runner.CompleteCallback();
        }
    }
}
