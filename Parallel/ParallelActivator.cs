using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConcurrencyTestLib.Helpers;
using ConcurrencyTestLib.Interfaces;
using ConcurrencyTestLib.Test;
using Serve.Shared.JsonSerializer;


namespace ConcurrencyTestLib.Parallel
{
    public class ParallelActivator
    {
        public static void StartParallelJob(TestCase tc, ITestCallback runner, int parentParalIndex = 0)
        {

        //Object 
            List<Task> parallelTasks = new List<Task>();
            int p = 0;
            tc.Manipulate();
            
            for (int i = 0; i < tc.Runs; i++)
            {
                List<Task> preqTasks = new List<Task>();
                int parallelIndex = i;
                for (int j = 0; j < tc.TransCount; j++)
                {
                    int transIndex = j;

                    string jsonReq = TestCaseFormatter.FormatSingleCase(tc, parallelIndex, transIndex, parentParalIndex);
                    Task mt = Task.Factory.StartNew(
                        () =>
                        {
                            TestCaseProcessor.ProcessTestCase(tc,jsonReq, runner, parallelIndex + 1);
                        }
                    );
                    preqTasks.Add(mt);
                    parallelTasks.Add(mt);
                }
                if (tc.ChildCases != null && tc.ChildCases.Count > 0)
                {
                    Task.WaitAll(preqTasks.ToArray());
                    Task st = Task.Factory.StartNew(() =>
                    {
                        System.Threading.Tasks.Parallel.ForEach(tc.ChildCases, (stc) =>
                        {
                            StartParallelJob(stc, runner, parallelIndex);
                        });
                    });
                    parallelTasks.Add(st);
                }
                p++;

                if (p >= tc.Degree || i >= tc.Runs - 1)
                {
                    Task.WaitAll(parallelTasks.ToArray());
                    p = 0;
                    parallelTasks.Clear();
                }

            }
        }
    }
}
