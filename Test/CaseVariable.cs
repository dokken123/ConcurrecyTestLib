using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConcurrencyTestLib.Test
{
    public class CaseVariable
    {
        public int TransAmount { get; set; }

        public string OIDBillNo { get; set; }

        public string DateAcct { get; set; }

        public string DateTrans { get; set; }

        public string AcctNo { get; set; }

        public string UserNo { get; set; }

        public long FreezeBillNo { get; set; }

        public int[] SplittedAmount { get; set; }

        public int ParallelIndex { get; set; }

        public int TransIndex { get; set; }
    }
}
