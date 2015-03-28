using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConcurrencyTestLib.Test;

namespace ConcurrencyTestLib.Interfaces
{
    public interface ITestCallback
    {
        void MessageCallback(string output);
        void CompleteCallback();
    }
}
