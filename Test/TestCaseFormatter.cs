using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConcurrencyTestLib.Helpers;

namespace ConcurrencyTestLib.Test
{
    public class TestCaseFormatter
    {
        private static readonly Random rdm = new Random();
        private static readonly List<string> usedIds = new List<string>();
        private static readonly List<long> usedFrzIds = new List<long>();

        private static DateTime initialBillNoDT = DateTime.Now;

        public static void ManipulateCaseData(TestCase ts, int parallelIndex)
        {
            for (int x = 0; x < ts.TransCount; x++)
            {
                CaseVariable variable = new CaseVariable();
                variable.ParallelIndex = parallelIndex;
                variable.TransIndex = x;
                int ind = parallelIndex;
                if (parallelIndex > 99)
                {
                    ind = parallelIndex - 99;
                }

                initialBillNoDT = DateTime.Now;
                string noOffset = ind.ToString("D2").Substring(0, 2);

                int myChildIndex = 0;
                if (ts.Parent != null)
                {
                    myChildIndex = ts.Parent.ChildCases.IndexOf(ts);
                }

                variable.UserNo = String.Format("{0}{3}{2}{1}", initialBillNoDT.ToString("yyyyMMddhhmmssffff"), noOffset, rdm.Next(0, 99), x + 1);
                variable.AcctNo = String.Format("{0}{3}{1}{2}", initialBillNoDT.ToString("yyyyMMddhhmmssffff"), noOffset, rdm.Next(0, 99), x + 1);
                
                variable.OIDBillNo = String.Format("{0}{2}{3}{1}", initialBillNoDT.ToString("yyyyMMddHHmmssffff"), noOffset, rdm.Next(0, 99), x + 1);
                while (usedIds.Contains(variable.OIDBillNo))
                {
                    variable.OIDBillNo = String.Format("{0}{2}{3}{1}", initialBillNoDT.ToString("yyyyMMddHHmmssffff"), noOffset, rdm.Next(0, 99), x + 1);
                }
                usedIds.Add(variable.OIDBillNo);
                if (ts.Parent != null)
                {
                    variable.OIDBillNo = String.Format("9{0}{1}{3}{2}", initialBillNoDT.ToString("yyyyMMddhhmmssffff"), noOffset, x + 1, myChildIndex);
                }
                variable.DateAcct = DateTime.Now.ToString("yyyyMMdd");
                variable.DateTrans = DateTime.Now.ToString("yyyyMMddHHmmss");

                variable.FreezeBillNo = long.Parse(String.Format("{0}{1}", DateTime.Now.ToString("yyyyMMddHHmmss"), rdm.Next(0, 99)));
                while (usedFrzIds.Contains(variable.FreezeBillNo))
                {
                    variable.FreezeBillNo = long.Parse(String.Format("{0}{1}", DateTime.Now.ToString("yyyyMMddHHmmss"), rdm.Next(0, 99)));
                }
                usedFrzIds.Add(variable.FreezeBillNo);

                variable.TransAmount = rdm.Next(1, 999999);

                if (ts.SplitAmount && ts.ChildCases != null && ts.ChildCases.Count > 0)
                {
                    variable.SplittedAmount = new int[ts.ChildCases.Count];
                    int leftAmount = variable.TransAmount;

                    for (int i = 0; i < ts.ChildCases.Count; i++)
                    {
                        if (i == ts.ChildCases.Count - 1)
                        {
                            variable.SplittedAmount[i] = leftAmount;
                        }
                        else
                        {
                            variable.SplittedAmount[i] = rdm.Next(1, leftAmount);
                            leftAmount = leftAmount - variable.SplittedAmount[i];
                        }

                    }
                }
                ts.Variables.Add(variable);
            }
        }

        public static string FormatSingleCase(TestCase ts,int parallelIndex,int transIndex, int parentParalIndex = 0)
        {
            CaseVariable variable = ts.Variables.Find((v) => { return v.TransIndex == transIndex && v.ParallelIndex == parallelIndex; });
            string tp_path = ts.JSONTemplatePath;
            if (String.IsNullOrEmpty(tp_path))
            {
                tp_path = String.Format("{0}-{1}", ts.API, ts.TradeCode);
            }
            string json = FileReader.LoadFileContent(tp_path, "tmpl", @"Tests\templates\");

            json = Regex.Replace(json, "[\t\r\n]", "");
            json = json.Replace("{{bill_no}}", variable.OIDBillNo);
            json = json.Replace("{{date_acct}}", variable.DateAcct);
            json = json.Replace("{{date_trans}}", variable.DateTrans);
            json = json.Replace("{{user_no}}", variable.UserNo);
            json = json.Replace("{{acct_no}}", variable.AcctNo);
            json = json.Replace("{{frz_bill_no}}", variable.FreezeBillNo.ToString());
            json = json.Replace("{{amt}}", variable.TransAmount.ToString());

            if (ts.Parent != null)
            {
                string totalAmt = ts.Parent.Variables.FindAll((v) => { return v.ParallelIndex == parentParalIndex; })
                                    .Sum((v) => { return v.TransAmount; }).ToString();
                json = json.Replace("{{ord_amt_sum}}", totalAmt);
                Regex re = new Regex(@"\{\{([^\}\|]*)\|?([^\}]*)\}\}", RegexOptions.None);
                MatchCollection mc = re.Matches(json);
                foreach (Match m in mc)
                {
                    string parentVarname = m.Groups[1].Value.Trim();
                    int parentTranIndex = 0;
                    if (!String.IsNullOrEmpty(m.Groups[2].Value))
                    {
                        parentTranIndex = int.Parse(m.Groups[2].Value.Trim());
                        parentTranIndex -= 1;
                    }
                    CaseVariable parentVar = ts.Parent.Variables.Find((v) => { return v.ParallelIndex == parentParalIndex; });
                    string parentVarval = "";
                    switch (parentVarname)
                    {
                        case "ord_bill_no":
                            parentVarval = parentVar.OIDBillNo;
                            break;
                        case "ord_date_acct":
                            parentVarval = parentVar.DateAcct;
                            break;
                        case "ord_date_trans":
                            parentVarval = parentVar.DateTrans;
                            break;
                        case "ord_acct_no":
                            parentVarval = parentVar.AcctNo;
                            break;
                        case "ord_frz_bill_no":
                            parentVarval = parentVar.FreezeBillNo.ToString();
                            break;
                        case "ord_amt":
                            int parentAmount = parentVar.TransAmount;
                            int myIndex = ts.Parent.ChildCases.IndexOf(ts);
                            if (ts.Parent.SplitAmount)
                            {
                                parentAmount = parentVar.SplittedAmount[myIndex];
                            }
                            parentVarval = parentAmount.ToString();
                            break;
                    }
                    json = json.Replace(m.Value, parentVarval);
                }
            }
            return json;
        }
    }
}
