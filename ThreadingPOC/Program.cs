using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ThreadingPOC
{
    class Program
    {
        static void Main(string[] args)  // static void Main(string[] args)   //  static async Task Main()
        {
            // RunTenTaskParallel(); //Run10TaskParallel    Async 


            // ParallelLibraryForEach();   // ParallelLibrary 


            //  CancelToken();  // CancelToken


            // CancelationToken2(0);   // if minNumber=0 that we will have camcelation will work (Unable to compute mean: A task was canceled.) if any other > 0 the compute will happen 
            // CancelationToken2(2);  // in this case calculation will finish and no cancel ===>  Calculating overall mean... The mean is


            //  CountDownConcurrentQueueAsync().Wait();   ////  CountdownEvent 


            //  ThreadPoolQueue();   //  Thread pool thread

            //  ThreadPoolFebanci();      ////  Thread pool thread  QueueUserWorkItem ( WaitCallback, Object)

            // DataFlowReadWrite();   /// we should use here static async Task Main()  


            // PassingDataBetweenThreads();
        }



        #region Parallel Canel Token 
        static void CancelToken()
        {
            int[] nums = Enumerable.Range(0, 10000000).ToArray();
            CancellationTokenSource cts = new CancellationTokenSource();

            // Use ParallelOptions instance to store the CancellationToken
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            po.MaxDegreeOfParallelism = System.Environment.ProcessorCount;
            Console.WriteLine("Press any key to start. Press 'c' to cancel.");
            Console.ReadKey();

            // Run a task so that we can cancel from another thread.
            Task.Factory.StartNew(() =>
            {
                if (Console.ReadKey().KeyChar == 'c')
                    cts.Cancel();
                Console.WriteLine("press any key to exit");
            });

            try
            {
                Parallel.ForEach(nums, po, (num) =>
                {
                    double d = Math.Sqrt(num);
                    Console.WriteLine("{0} on {1}", d, Thread.CurrentThread.ManagedThreadId);
                    po.CancellationToken.ThrowIfCancellationRequested();
                });
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                cts.Dispose();
            }

            Console.ReadKey();
        }

        static void CancelationToken2(int minNumber)
        {
            // Define the cancellation token.
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            Random rnd = new Random();
            Object lockObj = new Object();

            List<Task<int[]>> tasks = new List<Task<int[]>>();
            TaskFactory factory = new TaskFactory(token);
            for (int taskCtr = 0; taskCtr <= 10; taskCtr++)
            {
                int iteration = taskCtr + 1;
                tasks.Add(factory.StartNew(() =>
                {
                    int value;
                    int[] values = new int[10];
                    for (int ctr = 1; ctr <= 10; ctr++)
                    {
                        lock (lockObj)
                        {
                            value = rnd.Next(minNumber, 101);
                        }
                        if (value == 0)
                        {
                            source.Cancel();
                            Console.WriteLine("Cancelling at task {0}", iteration);
                            break;
                        }
                        values[ctr - 1] = value;
                    }
                    return values;
                }, token));
            }
            try
            {
                Task<double> fTask = factory.ContinueWhenAll(tasks.ToArray(),
                                                             (results) =>
                                                             {
                                                                 Console.WriteLine("Calculating overall mean...");
                                                                 long sum = 0;
                                                                 int n = 0;
                                                                 foreach (var t in results)
                                                                 {
                                                                     foreach (var r in t.Result)
                                                                     {
                                                                         sum += r;
                                                                         n++;
                                                                     }
                                                                 }
                                                                 return sum / (double)n;
                                                             }, token);
                Console.WriteLine("The mean is {0}.", fTask.Result);
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                        Console.WriteLine("Unable to compute mean: {0}",
                                          ((TaskCanceledException)e).Message);
                    else
                        Console.WriteLine("Exception: " + e.GetType().Name);
                }
            }
            finally
            {
                source.Dispose();
            }
        }
        #endregion

        #region ParallelLibrary 
        static void ParallelLibraryForEach()
        {
            // 2 million
            var limit = 2_000_000;
            var numbers = Enumerable.Range(0, limit).ToList();

            var watch = Stopwatch.StartNew();
            var primeNumbersFromForeach = GetPrimeList(numbers);
            watch.Stop();

            var watchForParallel = Stopwatch.StartNew();
            var primeNumbersFromParallelForeach = GetPrimeListWithParallel(numbers);
            watchForParallel.Stop();

            Console.WriteLine($"Classical foreach loop | Total prime numbers : {primeNumbersFromForeach.Count} | Time Taken : {watch.ElapsedMilliseconds} ms.");
            Console.WriteLine($"Parallel.ForEach loop  | Total prime numbers : {primeNumbersFromParallelForeach.Count} | Time Taken : {watchForParallel.ElapsedMilliseconds} ms.");

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }


        /// <summary>
        /// GetPrimeList returns Prime numbers by using sequential ForEach
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        private static IList<int> GetPrimeList(IList<int> numbers) => numbers.Where(IsPrime).ToList();

        /// <summary>
        /// GetPrimeListWithParallel returns Prime numbers by using Parallel.ForEach
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        private static IList<int> GetPrimeListWithParallel(IList<int> numbers)
        {
            var primeNumbers = new ConcurrentBag<int>();

            Parallel.ForEach(numbers, number =>
            {
                if (IsPrime(number))
                {
                    primeNumbers.Add(number);
                }
            });

            return primeNumbers.ToList();
        }

        /// <summary>
        /// IsPrime returns true if number is Prime, else false.(https://en.wikipedia.org/wiki/Prime_number)
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static bool IsPrime(int number)
        {
            if (number < 2)
            {
                return false;
            }

            for (var divisor = 2; divisor <= Math.Sqrt(number); divisor++)
            {
                if (number % divisor == 0)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Run10TaskParallel 
        /// <summary>
        /// /Run10TaskParallel 
        /// </summary>
        public static void RunTenTaskParallel()
        {
            GoAsync();
            Console.ReadLine();
        }

        public static async void GoAsync()
        {
            Console.WriteLine("Starting");
            List<Task<int>> allTask = new List<Task<int>>();
            Random rnd = new Random();
            for (int i = 0; i < 10; i++)
            {
                var myTask = Sleep(i, rnd.Next(3000, 8000));
                allTask.Add(myTask);
            }

            int[] result = await Task.WhenAll(allTask);
            Console.WriteLine("All myTask are done.");
        }

        private async static Task<int> Sleep(int processId, int ms)
        {
            Console.WriteLine("Sleeping for {0} at {1}", ms, processId);
            await Task.Delay(ms);
            Console.WriteLine("Sleeping for {0} finished at {1}", ms, processId);
            return ms;
        }
        #endregion

        #region count Down ConcurrentQueue

        static async Task CountDownConcurrentQueueAsync()
        {
            // Initialize a queue and a CountdownEvent
            ConcurrentQueue<int> queue = new ConcurrentQueue<int>(Enumerable.Range(0, 10000));
            CountdownEvent cde = new CountdownEvent(10000); // initial count = 10000

            // This is the logic for all queue consumers
            Action consumer = () =>
            {
                int local;
                // decrement CDE count once for each element consumed from queue
                while (queue.TryDequeue(out local)) cde.Signal();
            };

            // Now empty the queue with a couple of asynchronous tasks
            Task t1 = Task.Factory.StartNew(consumer);
            Task t2 = Task.Factory.StartNew(consumer);

            // And wait for queue to empty by waiting on cde
            cde.Wait(); // will return when cde count reaches 0

            Console.WriteLine("Done emptying queue.  InitialCount={0}, CurrentCount={1}, IsSet={2}",
                cde.InitialCount, cde.CurrentCount, cde.IsSet);

            // Proper form is to wait for the tasks to complete, even if you that their work
            // is done already.
            await Task.WhenAll(t1, t2);

            // Resetting will cause the CountdownEvent to un-set, and resets InitialCount/CurrentCount
            // to the specified value
            cde.Reset(10);

            // AddCount will affect the CurrentCount, but not the InitialCount
            cde.AddCount(2);

            Console.WriteLine("After Reset(10), AddCount(2): InitialCount={0}, CurrentCount={1}, IsSet={2}",
                cde.InitialCount, cde.CurrentCount, cde.IsSet);

            // Now try waiting with cancellation
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel(); // cancels the CancellationTokenSource
            try
            {
                cde.Wait(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("cde.Wait(preCanceledToken) threw OCE, as expected");
            }
            finally
            {
                cts.Dispose();
            }
            // It's good to release a CountdownEvent when you're done with it.
            cde.Dispose();
        }

        #endregion

        #region ThreadPool 

        static void ThreadPoolQueue()
        {
            // Queue the task.
            ThreadPool.QueueUserWorkItem(ThreadProc);
            Console.WriteLine("Main thread does some work, then sleeps.");
            Thread.Sleep(1000);

            Console.WriteLine("Main thread exits.");
        }

        // This thread procedure performs the task.
        static void ThreadProc(Object stateInfo)
        {
            // No state object was passed to QueueUserWorkItem, so stateInfo is null.
            Console.WriteLine("Hello from the thread pool.");
        }
        #endregion

        #region Thread Pool  QueueUserWorkItem(WaitCallback, Object)

        static void ThreadPoolFebanci()
        {
            const int FibonacciCalculations = 5;

            var doneEvents = new ManualResetEvent[FibonacciCalculations];
            var fibArray = new Fibonacci[FibonacciCalculations];
            var rand = new Random();

            Console.WriteLine($"Launching {FibonacciCalculations} tasks...");
            for (int i = 0; i < FibonacciCalculations; i++)
            {
                doneEvents[i] = new ManualResetEvent(false);
                var f = new Fibonacci(rand.Next(20, 40), doneEvents[i]);
                fibArray[i] = f;
                ThreadPool.QueueUserWorkItem(f.ThreadPoolCallback, i);
            }

            WaitHandle.WaitAll(doneEvents);
            Console.WriteLine("All calculations are complete.");

            for (int i = 0; i < FibonacciCalculations; i++)
            {
                Fibonacci f = fibArray[i];
                Console.WriteLine($"Fibonacci({f.N}) = {f.FibOfN}");
            }
        }

        #endregion


        #region DataFlow Block 
        static async void DataFlowReadWrite()
        {
            var bufferBlock = new BufferBlock<int>();

            // Post several messages to the block.
            for (int i = 0; i < 3; i++)
            {
                bufferBlock.Post(i);
            }

            // Receive the messages back from the block.
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine(bufferBlock.Receive());
            }

            // Post more messages to the block.
            for (int i = 0; i < 3; i++)
            {
                bufferBlock.Post(i);
            }

            // Receive the messages back from the block.
            while (bufferBlock.TryReceive(out int value))
            {
                Console.WriteLine(value);
            }

            // Write to and read from the message block concurrently.
            var post01 = Task.Run(() =>
            {
                bufferBlock.Post(0);
                bufferBlock.Post(1);
            });
            var receive = Task.Run(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    Console.WriteLine(bufferBlock.Receive());
                }
            });
            var post2 = Task.Run(() =>
            {
                bufferBlock.Post(2);
            });

            await Task.WhenAll(post01, receive, post2);

            // Demonstrate asynchronous dataflow operations.
            await AsyncSendReceive(bufferBlock);
        }

        // Demonstrates asynchronous dataflow operations.
        static async Task AsyncSendReceive(BufferBlock<int> bufferBlock)
        {
            // Post more messages to the block asynchronously.
            for (int i = 0; i < 3; i++)
            {
                await bufferBlock.SendAsync(i);
            }

            // Asynchronously receive the messages back from the block.
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine(await bufferBlock.ReceiveAsync());
            }
        }
        #endregion


        #region Passing Data 

        static void PassingDataBetweenThreads()
        {
            // Create a thread with a ParemeterizedThreadStart  
            Print p = new Print();
            Thread workerThread = new Thread(p.PrintJob);
            // Start thread with a parameter  
            workerThread.Start("Some data in PrintJob ");

            // Pass a class object to a worker thread  
            Person mohammed = new Person("Mohamed Hass", 40, "Male");
            Thread workerThread2 = new Thread(p.PrintPerson);
            workerThread2.Start(mohammed);

            // Main thread  
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"Main thread: {i}");
                Thread.Sleep(200);
            }

            Console.ReadKey();
        }
        #endregion
    }
}
