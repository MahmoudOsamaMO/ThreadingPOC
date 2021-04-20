using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadingPOC
{
    public class Fibonacci
    {
        private ManualResetEvent _doneEvent;

        public Fibonacci(int n, ManualResetEvent doneEvent)
        {
            N = n;
            _doneEvent = doneEvent;
        }

        public int N { get; }

        public int FibOfN { get; private set; }

        public void ThreadPoolCallback(Object threadContext)
        {
            int threadIndex = (int)threadContext;
            Console.WriteLine($"Thread {threadIndex} started...");
            FibOfN = Calculate(N);
            Console.WriteLine($"Thread {threadIndex} result calculated...");
            _doneEvent.Set();
        }

        public int Calculate(int n)
        {
            if (n <= 1)
            {
                return n;
            }
            return Calculate(n - 1) + Calculate(n - 2);
        }
    }
}
