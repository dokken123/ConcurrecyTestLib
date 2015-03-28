using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConcurrencyTestLib.Utils;
using ConcurrencyTestLib.Helpers;

namespace ConcurrencyTestLib.Test
{
    public class TestCase
    {

        private TestCase _parent = null;

        private List<TestCase> _children = null;

        private int _transCount = 1;

        public int Degree { get; set; }

        public int Runs { get; set; }

        public API API { get; set; }

        public API_TRADECODES TradeCode { get; set; }

        public List<CaseVariable> Variables { get; set; }

        public string JSONTemplatePath { get; set; }

        public bool SplitAmount { get; set; }

        public int TransCount
        {
            get
            {
                if (this._transCount <= 0)
                {
                    this._transCount = 1;
                }
                return _transCount;
            }

            set
            {
                this._transCount = value;
                if (value <= 0)
                {
                    this._transCount = 1;
                }
            }
        }

        public TestCase Parent
        {
            get
            {
                return this._parent;
            }
        }

        public List<TestCase> ChildCases
        {
            get { return _children; }
            set
            {
                _children = value;
                if (_children != null && _children.Count > 0)
                {
                    foreach (TestCase ts in _children)
                    {
                        ts._parent = this;
                    }
                }
            }
        }

        public void Manipulate()
        {
            this.Variables = new List<CaseVariable>();
            for (int i = 0; i < this.Runs; i++)
            {
                TestCaseFormatter.ManipulateCaseData(this, i);
            }
        }
    }
}
